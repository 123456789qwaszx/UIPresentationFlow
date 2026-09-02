# M2 — Resolver Boundary, Determinism & Priority Policy

## Goal

UI Resolver 계층을 다음 계약으로 정리한다.

> **같은 Spec + 같은 Context → 같은 Resolved Result**

그리고 Resolve 과정이:

- Unity 전역 환경을 직접 읽지 않고
- authored input을 변경하지 않고
- variant priority를 일관된 규칙으로 처리하도록 만든다.

M2가 끝나면 화면비에 따른 실제 Layout asset은 아직 없어도 된다.
하지만 가짜 `DisplayContext`를 넣어 resolver를 완전히 테스트할 수 있어야 한다.

---

# 1. Current State Problems

## Problem 1 — `VariantCondition`의 hidden input

현재:

```text
Matches(UIContext)
  ├─ UIContext
  ├─ Application.platform   ← hidden input
  └─ Screen.width/height    ← hidden input
```

같은 `UIContext`라도 실행 환경에 따라 결과가 달라진다.

목표:

```text
Matches(UIContext, DisplayContext)
```

모든 판정 입력이 signature에 드러난다.

---

## Problem 2 — input mutation

현재 `UIVariantResolver`가 `spec.variants` 배열을 정렬한다.

이 배열은 authored asset data다.

문제:

```text
Resolve
→ spec 내부 order 변경
→ 이후 Editor/Runtime observer가 다른 order를 봄
```

목표:

- 원본 배열 불변
- 별도 index/order enumeration 사용

---

## Problem 3 — priority semantics 불일치

현재 descending order에서:

```text
Prefab
→ 첫 override만 적용 → high priority wins

Theme
→ 모든 match가 overwrite → low priority가 마지막에 이길 수 있음

Layout
→ 모든 match가 overwrite → low priority가 마지막에 이길 수 있음
```

`priority`라는 필드의 의미가 일관되지 않다.

M2에서 반드시 계약을 고친다.

---

## Problem 4 — missing spec error path

`UIResolver`는 catalog miss를 log한 뒤 계속 진행할 수 있다.

이후 null spec이 variant resolver에 들어가면 다른 예외가 발생한다.

실패 원인이 흐려진다.

---

## Problem 5 — duplicate variant ID

Forced screen override는 `variantId`로 rule을 찾는다.

동일 ID가 여러 개면 어떤 rule이 선택되는지 authored data order에 의존한다.

Catalog/Spec validation으로 막는 편이 좋다.

---

# 2. Context API Decision

권장 최종 signature:

```csharp
public ResolvedUIScreen Resolve(
    UIScreenSpec spec,
    in UIContext ui,
    in DisplayContext display)
```

`UIResolver`:

```csharp
public UIResolveResult Resolve(
    ScreenKey screenKey,
    in DisplayContext display,
    UIActionKey? action = null)
```

UIContext는 UIResolver가 현재처럼 보유할 수 있다.

또는:

```csharp
Resolve(screenKey, uiContext, displayContext)
```

까지 모두 명시할 수 있다.

## 권장

M2에서는 최소 변경:

```text
UIResolver owns UIContext
Resolve call receives DisplayContext
```

이유:

- Theme/Locale/Experiment는 session-level context 성격
- Display는 preview/test마다 쉽게 바뀌어야 함
- Router API 변화 최소화

M5에서 live context switch가 필요하면 재평가한다.

---

# 3. PresentationContext를 지금 만들지 않는 이유

다음 wrapper는 가능하다.

```csharp
PresentationContext
{
    UIContext UI;
    DisplayContext Display;
}
```

하지만 지금은 역할이 명확히 둘로 나뉜다.

wrapper를 추가하면 단순 parameter bundle일 가능성이 높다.

M2에서는 만들지 않는다.

다음 조건이 생기면 도입:

- Character Presentation이 추가되고 동일 bundle을 공유
- resolver가 3개 이상의 공통 Context를 받음
- API 호출부의 parameter drift가 실제 문제로 증명됨

즉 Optional M7 전까지는 필요성을 증명하지 않은 abstraction이다.

---

# 4. Variant Priority Policy

## 추천 정책 — Highest Priority Wins Per Field

각 field를 독립적으로 lock한다.

예:

```text
priority 100:
  overrideLayout = WideLayout
  overrideTheme  = null

priority 50:
  overrideLayout = LegacyLayout
  overrideTheme  = DarkTheme
```

결과:

```text
Layout = WideLayout    (p100)
Theme  = DarkTheme     (p50)
```

즉 가장 높은 priority 중 실제로 해당 field를 override한 rule이 승리한다.

Pseudo:

```text
prefabResolved = false
themeResolved  = false
layoutResolved = false

for each matching rule in priority desc:
    if !prefabResolved && overridePrefab:
        prefab = ...
        prefabResolved = true

    if !themeResolved && overrideTheme:
        theme = ...
        themeResolved = true

    if !layoutResolved && overrideLayout:
        layout = ...
        layoutResolved = true
```

Base는 어떤 rule도 해당 field를 override하지 않을 때 사용한다.

이 정책은 현재 Prefab lock 동작을 Theme/Layout에도 일관되게 확장한다.

---

# 5. Stable Ordering

priority가 같은 rule이 여러 개일 때 tie-breaker가 필요하다.

권장:

```text
1. priority descending
2. authored array index ascending
```

즉 같은 priority에서는 asset에 적힌 순서를 유지한다.

원본 배열은 정렬하지 않는다.

구현 후보:

### A. index list 생성

할당 최소화보다 명확성 우선.

### B. LINQ OrderByDescending

코드는 간단하지만 runtime allocation.

현재 UI navigation 빈도라면 큰 문제는 아닐 수 있지만,
포트폴리오 core resolver에서는 LINQ 없이 명시적 copy+sort도 좋다.

예:

```csharp
var ordered = new List<(UIVariantRule rule, int index)>();
```

성능보다 mutation-free correctness가 우선이다.

---

# 6. VariantCondition Redesign

현재 내부 helper:

```text
MatchesPlatform()
DetectCurrentPlatform()
MatchesAspectRatio()
GetCurrentAspectRatio()
```

목표:

```csharp
public bool Matches(in UIContext ui, in DisplayContext display)
```

내용:

```text
Theme
Locale
Experiment
Platform ← display.Platform
Aspect   ← display.AspectRatio / Orientation
```

M3에서 class classifier가 들어오기 전까지 기존 `AspectRule.Range`를 유지할 수 있다.

즉 M2에서는 기능 회귀를 최소화한다.

```text
Portrait
Landscape
Range
Any
```

를 DisplayContext 기반으로 그대로 판정한다.

---

# 7. Platform Condition

기존 `VariantPlatform`과 M1의 `DisplayPlatform`이 중복될 가능성이 있다.

M2에서 하나로 정리한다.

권장:

- `VariantPlatform` 제거/대체
- `VariantCondition.platform` 타입을 `DisplayPlatform`으로 사용
- `Any` 의미가 필요한 경우:
  - `usePlatform == false`가 이미 Any 역할
  - enum의 Any를 없앨 수 있음

Migration 영향:

Serialized ScriptableObject가 이미 존재하면 enum 변경이 asset serialization에 영향을 줄 수 있다.

M0에서 actual authored assets를 확인한 후 선택.

안전한 단계적 방법:

1. M2에서는 기존 `VariantPlatform` 유지
2. provider의 `DisplayPlatform` → VariantPlatform mapping
3. M6에서 enum 통합

하지만 중복 model은 좋지 않다.

**asset migration 비용이 낮으면 M2에서 통합**한다.

---

# 8. Aspect Condition in M2

M3에서 `AspectLayoutClass`를 넣을 예정이므로,
M2에서는 기존 Range를 그대로 살린다.

예:

```text
display.AspectRatio >= aspectMin
&& display.AspectRatio <= aspectMax
```

Portrait/Landscape:

```text
display.Orientation
```

사용.

이 단계에서 Wide/Tablet enum을 성급히 넣지 않는다.

---

# 9. Error Contract

## Catalog miss

권장:

`UIResolver.Resolve`는 불가능한 ScreenKey를 정상 결과처럼 취급하지 않는다.

현재 구조가 exception 기반 factory를 이미 사용한다.

따라서 초기 포트폴리오 runtime에는 다음이 일관적이다.

```text
Unknown ScreenKey
→ KeyNotFoundException / InvalidOperationException
→ message에 ScreenKey 포함
```

또는 `TryResolve`를 추가할 수 있지만,
사용처가 하나뿐이면 API가 늘어난다.

권장 M2:

```text
Resolve = strict
```

Router의 unknown route fallback은 별도 책임으로 유지한다.

---

# 10. Trace Redesign

현재 trace가 두 종류다.

```text
UIResolveTrace
ResolvedUIScreen.DecisionTrace string
```

그리고 `UIResolver`가 DecisionTrace 전체 문자열을 다시 한 줄로 Add한다.

구조가 중복된다.

M2에서는 큰 logging framework를 만들 필요는 없지만,
M5 확장을 위해 shape를 정리한다.

권장 최소안:

```text
UIResolveTrace
  Add(...)
  Dump()

UIVariantResolver는 UIResolveTrace를 인자로 받아 append
```

그러면 별도 StringBuilder DecisionTrace를 결과에 저장할 필요가 줄 수 있다.

하지만 `ResolvedUIScreen.DecisionTrace`가 Editor/debug에서 유용할 수 있다.

대안:

```text
ResolveDiagnostics
```

를 immutable result에 둔다.

M2에서는 다음만 보장하면 된다.

- Display resolution 기록
- aspect ratio 기록
- matched rule/priority 기록
- field winner 기록
- forced override 여부 기록

M5에서 UI를 붙인다.

---

# 11. Implementation Tasks

## M2.1 API signature 변경

`VariantCondition.Matches`

from:

```text
Matches(UIContext)
```

to:

```text
Matches(UIContext, DisplayContext)
```

## M2.2 Direct Unity API 제거

`VariantCondition`에서 제거:

```text
Application.platform
Screen.width
Screen.height
```

M2 완료 후 repository search로 해당 direct read가 provider 외에 남았는지 확인.

예외:
- Editor preview utility
- unrelated gameplay code

현재 프로젝트 UI 범위에서 zero가 목표.

## M2.3 Resolver ordering mutation 제거

원본 `spec.variants` 보존.

테스트에서 reference/order를 비교한다.

## M2.4 Priority field-lock 정책 구현

Prefab / Theme / Layout 모두 동일 policy.

## M2.5 Tie policy 구현

same priority = authored order.

## M2.6 Forced override 검증

Forced override는 명시적 debug override이므로 normal condition을 무시하는 현재 의미를 유지할지 확정.

권장 유지:

```text
ScreenOverride
= 조건을 무시하고 해당 variantId 강제 적용
```

단 duplicate variant ID는 validation error.

## M2.7 Missing screen failure 정리

명확한 오류 발생.

## M2.8 Validation 보강

`UIScreenCatalog.ValidateAll` 또는 별도 validator에:

- empty variantId
- duplicate variantId
- null condition (허용 여부 결정)
- aspectMin > aspectMax
- Range에서 invalid threshold

추가 후보.

Editor validation의 범위를 너무 키우지 않는다.

---

# 12. Tests

## Determinism

같은 spec/context 100회 Resolve:

```text
Prefab same
Theme same
Layout same
AppliedVariantIds same
```

## No mutation

before:

```text
[A(p10), B(p100), C(p50)]
```

Resolve 후 authored array가 같은 순서인지 검사.

## Priority

### Prefab

p100/p50 둘 다 override → p100

### Theme

p100/p50 둘 다 override → p100

### Layout

p100/p50 둘 다 override → p100

### field-independent

p100 layout only
p50 theme only

→ 둘 다 적용

## Tie

p100 A, p100 B가 같은 field override
→ authored first A

## Conditions

- theme match/miss
- locale match/miss
- experiment key/value
- platform
- portrait
- landscape
- aspect range inclusive edges

## Forced override

- variant exists → forced result
- variant not found → 명확한 trace + base? 또는 failure policy 확정
- duplicate id → validation failure

## Catalog miss

expected exception/message.

---

# 13. Manual Verification

기존 Sample Screen에서:

1. M0 baseline UIContext로 Resolve
2. 실제 current DisplayContext 전달
3. 이전과 동일한 Prefab/Theme/Layout 결과인지 확인
4. Trace 확인

M2는 아직 새로운 responsive layout이 없어야 정상이다.

즉 **구조는 바뀌지만 화면은 바뀌지 않는 단계**다.

---

# 14. Files Expected to Change

```text
Assets/scripts/UI/Patcher/VariantCondition.cs
Assets/scripts/UI/Patcher/UIVariantResolver.cs
Assets/scripts/UI/Core/UIResolver.cs
Assets/scripts/UI/Core/ResolvedUIScreen.cs        # trace 설계에 따라
Assets/scripts/UI/Core/SO/UIScreenCatalog.cs      # validation에 따라
```

M1 files 사용:

```text
Assets/scripts/Display/*
```

Tests 추가.

---

# 15. Risks

## Serialized enum migration

Platform enum 통합 시 기존 asset 값 깨짐 여부.

## Priority behavior change

현재 Theme/Layout이 사실상 lower priority last-write일 수 있으므로,
기존 authored asset이 그 우연한 동작에 의존할 수 있다.

M0에서 variant assets 목록을 보고 회귀 확인한다.

## Trace API scope

M5 요구를 미리 다 만들다 과설계할 위험.

M2에서는 console-readable diagnostics까지만.

---

# 16. Completion Checklist

- [ ] VariantCondition hidden environment input 제거
- [ ] UIResolver가 DisplayContext를 받음
- [ ] authored variants mutation 제거
- [ ] priority policy 문서화
- [ ] Prefab priority test
- [ ] Theme priority test
- [ ] Layout priority test
- [ ] tie test
- [ ] Theme/Locale/Experiment 회귀 test
- [ ] Platform test
- [ ] Aspect Range test
- [ ] forced override test
- [ ] duplicate variant validation
- [ ] catalog miss failure 명확화
- [ ] 기존 visual baseline 회귀 없음
- [ ] M3 entry 승인

---

# 17. Completion Record

```text
Status: NOT STARTED

Final Resolve API:

Priority policy:
Tie policy:
Forced override policy:
Missing screen policy:

Mutation test:
Condition tests:
Priority tests:
Regression:

Serialized asset migration:

Known issues:

M3 entry approved: NO
```

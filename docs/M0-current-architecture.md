# M0 — Current Architecture Audit & Baseline

## Goal

`dev` 브랜치의 현재 UI 시스템이 실제로 어떤 순서로 동작하고, 무엇이 살아 있으며, 무엇이 임시/Legacy 상태인지 확정한다.

M0의 목적은 리팩터링이 아니다.

> **변경 전에 현재 시스템의 실제 기준선을 확보한다.**

M1 이후의 모든 변경은 이 기준선과 비교하여 회귀 여부를 판단한다.

---

# 1. Current State

## Repository baseline

- Repository: `123456789qwaszx/UIPresentationFlow`
- Working branch: `dev`
- 조사 시점의 `dev` tree SHA: `3ffd08e13cd6466769dc6e9aa29318816a867c89`
- Unity: `6000.2.7f2`
- UGUI 사용
- URP 2D 사용
- Unity Test Framework 패키지는 존재
- 현재 저장소 트리에서는 별도 Test asmdef / Tests 디렉터리가 확인되지 않음
- Spine Runtime은 없음

현재 `dev`는 조사 시점에 `main`과 같은 tree SHA를 가리키고 있다. 이후 작업은 반드시 `dev`를 기준으로 한다.

---

# 2. 현재 Runtime Flow 분석

현재 소스의 의도상 핵심 호출 흐름은 다음과 같다.

```text
UIRouter.Navigate(action)
        │
        ▼
RouteKeyResolver
        │
        ▼
UIResolver.Resolve(screenKey, action)
        │
        ├─ UIScreenCatalog.TryGetScreenSpec
        │
        ▼
UIVariantResolver.Resolve(spec, UIContext)
        │
        ▼
ResolvedUIScreen
        │
        ├─ Theme.BuildPatches
        └─ Layout.BuildPatches
        │
        ▼
UIResolveResult
        │
        ▼
UIScreenFactory.Create(result)
        │
        ├─ Instantiate(resolved.Prefab)
        ├─ UIScreen.BuildSlotMap
        ├─ UIComposer.Compose
        └─ UIPatchApplier.Apply
        │
        ▼
UIScreen
```

이 순서는 이후 M1~M4에서도 가능한 한 유지한다.

환경 대응을 추가하더라도 다음 책임은 바꾸지 않는다.

```text
Router   = 어떤 화면으로 갈 것인가
Resolver = 어떤 표현 결과를 사용할 것인가
Factory  = 결과를 실제 Unity Object로 만들 것인가
Composer = authored widget 구조를 생성
Patch    = 생성된 결과의 theme/layout을 변형
```

---

# 3. 주요 클래스별 현재 책임

## `UIScreenCatalog`

현재 책임:

- `ScreenKey -> UIScreenSpec`
- `route string -> ScreenKey`
- 런타임 캐시 `_screenMap`, `_routeMap`
- Editor validation 일부

주의점:

- 캐시는 `Init()`을 호출해야 만들어진다.
- `TryGetScreenSpec()`은 `_screenMap == null`이면 단순 `false`.
- 현재 활성 Runtime bootstrap 위치가 명확하지 않으므로 **누가 언제 `catalog.Init()`을 호출하는지 M0에서 반드시 실제 씬 기준으로 확인한다.**

확인 항목:

- Scene component가 직접 호출하는가
- 다른 bootstrap이 있는가
- Editor-only 흐름으로만 사용되고 있는가
- 현재 SampleScene이 실제 runtime navigation까지 가능한가

---

## `UIResolver`

현재 책임:

```text
ScreenKey
→ UIScreenSpec lookup
→ UIVariantResolver
→ Theme/Layout patch list 생성
→ Trace 생성
```

현재 주의점:

1. catalog lookup 실패 시 Error를 기록하지만 흐름을 중단하지 않는다.
2. 이후 `baseSpec == null` 상태로 `UIVariantResolver.Resolve`에 진입할 수 있다.
3. `UIContext`는 생성자에서 고정된다.
4. 아직 `DisplayContext`가 없다.

M2에서 error contract와 context boundary를 확정한다.

---

## `UIVariantResolver`

현재 핵심 동작:

```text
Base Prefab / Theme / Layout
        ↓
Forced Screen Override
        ↓
Normal Variant Rules
        ↓
ResolvedUIScreen
```

현재 중요한 관찰 사항:

### A. 입력 배열 정렬

`spec.variants`를 `Array.Sort`한다.

즉 Resolve가 authored input 배열의 순서를 바꿀 수 있다.

M2에서 제거 대상이다.

### B. 우선순위 정책이 속성마다 다르게 동작할 가능성

현재 높은 priority부터 순회한다.

- Prefab: 첫 매치가 적용되고 lock됨 → 높은 priority 승리
- Theme: 매치마다 덮어씀 → 뒤의 낮은 priority가 최종 승리할 수 있음
- Layout: 매치마다 덮어씀 → 뒤의 낮은 priority가 최종 승리할 수 있음

즉 현재 구현만 보면 `priority`의 의미가 Prefab과 Theme/Layout 사이에서 일관되지 않다.

M2 전에 이것을 단순 버그로 고치지 않는다.
먼저 기대 정책을 문서로 결정한다.

후보 정책:

```text
Policy A — Highest Wins Per Field
각 필드는 가장 높은 priority의 첫 override가 승리.

Policy B — Layered Composition
낮은 priority부터 높은 priority까지 누적 적용.
단, Theme/Layout이 단일 ScriptableObject 교체 구조이므로 실제 composition 의미를 다시 정의해야 함.
```

현재 구조에는 **Policy A가 더 자연스럽다.**

M2에서 최종 확정한다.

### C. VariantCondition이 Unity Runtime 환경을 직접 조회

현재 다음을 직접 사용한다.

```text
Screen.width
Screen.height
Application.platform
```

M1/M2 핵심 제거 대상이다.

---

## `ResolvedUIScreen`

현재 결과가 다음을 보유한다.

```text
ScreenKey
BaseSpec
Prefab
Theme
Layout
AppliedVariantIds
DecisionTrace
```

이 구조는 유지 가치가 높다.

M2 이후 `DisplayContext`를 결과에 보존할지는 신중히 결정한다.

권장:

- 결과 자체가 전체 Context를 소유하지 않는다.
- Trace/diagnostic 정보에는 resolution/aspect class를 기록한다.
- 재현성이 필요하면 별도의 `ResolveDiagnostics` 또는 `ResolveInputSummary`를 둔다.

---

## `UIScreenFactory`

현재 실제 Unity 생성 순서:

```text
Instantiate Prefab
→ Get UIScreen
→ BuildSlotMap
→ Compose
→ Apply Patches
```

이 순서는 Responsive UI에 적합하다.

이유:

- 위젯이 모두 만들어진 후
- `nameTag` 기반으로
- LayoutPatch를 적용할 수 있기 때문

M3에서도 기본 순서를 유지한다.

---

## `UIComposer`

현재 Slot tree를 BFS로 순회하면서 Widget을 생성한다.

중요 특징:

- `SlotSpec.slotName` lookup 사용
- prefab 내부 UISlot을 시작점으로 사용
- 동적으로 생성된 Slot widget 내부 UISlot도 다시 queue에 넣음
- `nameTag -> WidgetHandle` map 생성
- duplicate `nameTag` 경고

Responsive Layout이 안정적으로 작동하려면 `nameTag`가 사실상 layout target key 역할을 한다.

M3에서 다음 규칙을 명확히 한다.

> Responsive patch의 대상이 되는 Widget은 안정적인 `nameTag`를 가져야 한다.

---

## `UIScreen`

현재 두 개의 접근 경로가 있다.

### 정식 경로

```text
nameTag
→ WidgetHandle
→ RectTransform / Component
```

### 우회 경로

```text
GetWidgetDirect<T>(nameTag)
→ GameObject.name DFS
→ Component
```

`GetWidgetDirect`는 코드 주석상 prefab/rig 우회로다.

M0에서는 사용처를 조사한다.

M6 이전까지 바로 제거하지 않는다.

분류:

- 사용처 없음 → M6 Remove 후보
- 특정 prefab에 필요 → Legacy compatibility로 격리
- Responsive target에 필요 → 정식 WidgetHandle 방식으로 승격 가능한지 검토

---

## `UISlotBinder`

현재 특징:

- UISlot marker 우선
- marker가 없으면 name-based fallback
- duplicate slot은 first wins + warning
- required validation은 실제 호출 경로에서 strict:false 성격이 강함

M0에서는 현재 demo에서 누락 slot warning이 있는지 확인한다.

이번 프로젝트의 주목표가 slot validation은 아니므로 대규모 변경은 하지 않는다.

---

## `LayoutPatchSpec / LayoutSpecPatch`

현재 적용 가능한 항목:

```text
Active
AnchorMin / AnchorMax
Pivot
AnchoredPosition
SizeDelta
```

M3에서 Responsive UI의 핵심 재사용 자산이다.

현재 한계:

- `offsetMin/offsetMax` 직접 override 없음
- scale 없음
- sibling/order 없음
- LayoutElement preferred/min size 없음
- ContentSizeFitter/LayoutGroup 파라미터 없음
- prefab direct object는 WidgetHandle이 아니면 patch target이 아님

M3에서는 먼저 현재 기능만으로 대표 UI를 해결한다.
기능 부족이 실제로 증명될 때만 최소 확장한다.

---

# 4. Editor Tool 현재 상태

`UIScreenSpecEditorWindow`가 이미 존재하며 다음 기능을 가진다.

- `UIScreenSpecAsset` selection auto-bind
- Slot/Widget 편집
- ReorderableList 기반 구조
- nested slot path 관리
- widget preset
- slot graph cache
- clipboard/preset 관련 편집 보조

따라서 M5 Device Preview를 만들 때 이 100k+ 규모 EditorWindow 안에 처음부터 모든 기능을 밀어 넣지 않는다.

권장:

```text
UIScreenSpecEditorWindow   = authoring
UIDevicePreviewWindow      = environment simulation / preview
```

필요하면 나중에 서로 링크만 제공한다.

또한 초기 코드에서 layout constant로 보이는 값 중:

```text
CenterMinWidth = 3000f
CenterMaxWidth = 380f
```

처럼 이름과 값이 비정상적으로 보이는 부분이 있다.

M0에서 실제 Editor UI에 영향이 있는지 확인하고 단순 typo인지 기록한다.
이번 milestone의 핵심이 아니라면 즉시 수정하지 않는다.

---

# 5. Legacy / Runtime Bootstrap 상태

현재 확인된:

```text
Assets/scripts/UI/Legacy/UIBootStrap.cs
Assets/scripts/UI/Legacy/UITestDriver.cs
```

는 파일 전체가 주석 처리되어 있다.

`UIRuntimeRouter`는 단순 static holder다.

```text
UIRuntimeRouter.Router
```

따라서 소스만 놓고 보면 현재 Runtime composition root가 명확하지 않다.

이것은 M0의 최우선 확인 사항이다.

M0에서 다음 중 하나를 결론 낸다.

```text
A. 다른 활성 Bootstrap이 존재한다.
B. SampleScene이 과거 직렬화 상태에 의존한다.
C. 현재 runtime demo는 깨져 있고 Editor authoring만 살아 있다.
D. 별도의 수동 wiring 절차가 있다.
```

결론을 내기 전에는 기존 runtime이 정상이라고 가정하지 않는다.

---

# 6. SampleScene / Canvas 상태

현재 `SampleScene`에 `CanvasForTest`가 있고,
CanvasScaler는 조사된 YAML상:

```text
UI Scale Mode = Constant Pixel Size
Scale Factor  = 1
Reference Resolution = 800 x 600
```

상태다.

즉 현재 Scene은 아직 `Scale With Screen Size + 1920x1080` 같은 대표적인 responsive baseline이 아니다.

M3에서 반드시 결정할 질문:

> CanvasScaler가 담당할 연속적 스케일링과 Variant/LayoutPatch가 담당할 구조적 재배치는 어디서 나눌 것인가?

권장 책임:

```text
CanvasScaler
= 동일한 layout의 연속적 크기 스케일링

Variant/LayoutPatch
= 화면비 class가 바뀔 때 구조/위치/크기 정책 변경
```

---

# 7. M0 Decisions

M0에서 최종 확정해야 하는 결정들:

## D0-1 Runtime composition root

누가 아래 객체를 생성/연결하는지 확정:

```text
UIScreenCatalog
UIResolver
UIScreenFactory
UIRouter
WidgetFactory
UIComposer
UIPatchApplier
```

## D0-2 Catalog initialization

`UIScreenCatalog.Init()`의 소유자를 확정.

## D0-3 Resolver failure policy 후보

M2에서 구현하기 전에 다음 중 방향만 정한다.

```text
Strict demo/runtime:
missing ScreenSpec → throw clear exception

Recoverable runtime:
TryResolve → explicit failure result
```

단순 `Debug.LogError 후 null 진행`은 피한다.

## D0-4 Responsive sample screen

M3에서 사용할 대표 UIScreenSpec 하나 선정.

조건:

- 최소 3개 이상의 layout target
- 좌/우/중앙 정렬 차이가 보임
- Wide와 Tablet에서 구조 차이를 설명할 수 있음
- SafeArea 대상 control 포함 가능

## D0-5 Test location

권장:

```text
Assets/Tests/EditMode/
UIPresentationFlow.Tests.asmdef
```

Runtime assembly가 현재 Assembly-CSharp에 있으므로,
M1에서 테스트 asmdef를 만들 때 참조 가능 구조를 실제 Unity에서 검증한다.

장기적으로 Runtime asmdef 분리는 M6 후보이며 M1에서 과도하게 확장하지 않는다.

---

# 8. Implementation Tasks

## M0.1 Branch baseline 기록

- `dev` 존재 확인
- current commit/tree SHA 기록
- `main`에 쓰지 않는 작업 규칙 기록

## M0.2 Runtime call-flow 확인

코드 정적 추적 + Unity 실제 실행 비교.

확인 순서:

```text
scene start
→ catalog init
→ router creation
→ initial navigate
→ resolver
→ factory
→ compose
→ patch
```

## M0.3 Active/Legacy 분류

분류표 작성:

| Component | Keep | Refactor | Remove Candidate | Unknown |
|---|---|---|---|---|

최소 대상:

- UIRuntimeRouter
- UIRouter
- UIResolver
- UIVariantResolver
- UIScreenFactory
- UIComposer
- UIScreen
- UISlotBinder
- WidgetFactory
- WidgetRectApplier
- LayoutPatchSpec
- UIContext
- VariantCondition
- Legacy/*
- Editor/*

## M0.4 SampleScene baseline

기록:

- scene open 성공 여부
- compile error
- Console warning/error
- CanvasScaler 설정
- initial UI 상태
- navigation 가능 여부
- variant 적용 여부
- trace 출력 여부

## M0.5 Editor baseline

확인:

```text
Tools/UI/UIScreen Spec Editor
UIScreenSlotImporterWindow
UIScreenCatalog inspector validation
```

## M0.6 Test baseline

- EditMode test assembly 존재 여부
- 현재 Test Runner에서 테스트 수
- 새 test assembly 추가가 필요한지

---

# 9. Files to Inspect

M0에서 변경 없이 우선 읽어야 하는 파일:

```text
Assets/scripts/UI/Core/UIResolver.cs
Assets/scripts/UI/Core/UIRouter.cs
Assets/scripts/UI/Core/UIScreenFactory.cs
Assets/scripts/UI/Core/UIComposer.cs
Assets/scripts/UI/Core/UIScreen.cs
Assets/scripts/UI/Core/UISlotBinder.cs
Assets/scripts/UI/Core/WidgetFactory.cs
Assets/scripts/UI/Core/SO/UIScreenCatalog.cs

Assets/scripts/UI/Patcher/UIContext.cs
Assets/scripts/UI/Patcher/UIVariantResolver.cs
Assets/scripts/UI/Patcher/UIVariantRule.cs
Assets/scripts/UI/Patcher/VariantCondition.cs
Assets/scripts/UI/Patcher/LayoutPatchSpec.cs
Assets/scripts/UI/Patcher/LayoutSpecPatch.cs
Assets/scripts/UI/Patcher/UIPatchApplier.cs

Assets/scripts/UI/Legacy/*
Assets/scripts/UI/Editor/*
Assets/Scenes/SampleScene.unity
Assets/Prefabs/ScreenTemplate.prefab
```

M0 자체에서는 가능하면 production code를 변경하지 않는다.

필요한 경우 baseline을 복구하기 위한 최소 수정만 별도 기록한다.

---

# 10. Tests

## Static verification

- Runtime composition root 발견 여부
- Catalog Init caller 발견 여부
- `Screen.*` / `Application.platform` 사용처 목록
- `GetWidgetDirect` 사용처
- Legacy type 사용처
- `spec.variants` mutation 위치
- tests/asmdef 존재 여부

## Manual verification

### Gate A — Project

- [ ] Unity 6000.2.7f2에서 열림
- [ ] Compile Error 0

### Gate B — Scene

- [ ] SampleScene 열림
- [ ] Play Mode 진입 가능
- [ ] 현재 UI 표시 여부 기록
- [ ] 현재 navigation 여부 기록
- [ ] Console warning/error 캡처

### Gate C — Editor

- [ ] UIScreen Spec Editor 열림
- [ ] Asset 선택 시 bind
- [ ] Slot 목록 표시
- [ ] Widget 편집 가능

---

# 11. Deliverable

완료 시 이 문서의 `Completion Record`를 실제 결과로 채운다.

추가 산출물 권장:

```text
docs/audit/
├─ current-runtime-flow.txt
├─ current-console-notes.txt
└─ current-device-baseline.md
```

필수는 아니다.
M0 문서 하나에 기록해도 된다.

---

# 12. Completion Checklist

- [ ] `dev` baseline SHA 기록
- [ ] Runtime composition root 확인
- [ ] `UIScreenCatalog.Init()` 소유자 확인
- [ ] 실제 Runtime flow 확인
- [ ] Legacy 사용 여부 확인
- [ ] `GetWidgetDirect` 사용 여부 확인
- [ ] Resolver mutation/priority 문제 기록
- [ ] SampleScene baseline 확인
- [ ] CanvasScaler baseline 확인
- [ ] Editor Window baseline 확인
- [ ] Test assembly baseline 확인
- [ ] M1 변경 대상 파일 확정
- [ ] 모든 수동 검증 결과 기록

---

# 13. Completion Record

```text
Status: NOT STARTED

Baseline commit/tree:
Unity version:

Runtime composition root:
Catalog init owner:

SampleScene:
- Compile:
- Play Mode:
- Navigation:
- Variant:
- Trace:
- Console:

Editor Tool:
- Spec Editor:
- Slot Importer:

Tests:
- Existing test assembly:
- Existing test count:

Legacy:
- Active dependencies:

Known blockers:

Decision summary:

M1 entry approved: NO
```

`M1 entry approved`는 테스트하지 않은 상태에서 YES로 바꾸지 않는다.

# M1 — Display Environment Model

## Goal

Unity의 전역 표시 환경을 직접 읽는 코드를 하나의 경계로 모으고,
Resolver가 테스트 가능한 명시적 입력인 `DisplayContext`를 받을 수 있는 기반을 만든다.

M1의 핵심은 **Responsive UI를 구현하는 것이 아니다.**

> `Screen`과 `Application`을 읽는 책임을 UI 규칙에서 분리한다.

실제 Variant 연동은 M2~M3에서 수행한다.

---

# 1. Current State

현재 `VariantCondition`이 직접 다음을 읽는다.

```text
Screen.width
Screen.height
Application.platform
```

현재 `UIContext`는 다음을 가진다.

```text
ThemeId
LocaleId
Experiments
ScreenOverrides
```

이 두 종류의 정보는 성격이 다르다.

```text
UIContext
= 콘텐츠 / 운영 / 사용자 UI 조건

DisplayContext
= 실제 표시 장치 / viewport 조건
```

M1에서는 두 Context를 합치지 않는다.

---

# 2. Design Target

```text
Unity APIs
  │
  ├─ Screen.width
  ├─ Screen.height
  ├─ Screen.safeArea
  ├─ Screen.orientation
  └─ Application.platform
  │
  ▼
UnityDisplayContextProvider
  │
  ▼
DisplayContext
  │
  ├─────────────┐
  ▼             ▼
Runtime       Tests / Preview
```

M5 Editor Preview도 같은 `DisplayContext`를 직접 만들어 Resolver에 넣는다.

즉 `DisplayContext`는 Runtime 전용 DTO가 아니라 Presentation Layer의 공통 입력 모델이다.

---

# 3. DisplayContext 데이터 설계

## 권장 모델

중복 상태를 최소화한다.

```csharp
public readonly struct DisplayContext
{
    public Vector2Int Resolution { get; }
    public Rect SafeAreaPixels { get; }
    public DisplayPlatform Platform { get; }

    public float AspectRatio { get; }
    public DisplayOrientation Orientation { get; }
    public Rect SafeAreaNormalized { get; }
}
```

핵심 원칙:

> 계산 가능한 값은 가능하면 저장하지 않고 파생한다.

예:

```text
AspectRatio
= Resolution.x / Resolution.y

Orientation
= Resolution.x > Resolution.y
  ? Landscape
  : Resolution.x < Resolution.y
    ? Portrait
    : Square
```

이렇게 하면 다음 불일치가 불가능해진다.

```text
Resolution = 1920×1080
AspectRatio = 1.2   // 잘못된 중복 상태
```

---

# 4. 타입 결정

## `DisplayPlatform`

Unity의 `RuntimePlatform`을 그대로 Context에 노출하지 않는 것을 권장한다.

초기 후보:

```csharp
public enum DisplayPlatform
{
    Unknown,
    Desktop,
    Mobile,
    Console
}
```

이유:

- tests가 Unity platform enum 세부값에 묶이지 않음
- Android/iOS를 같은 Mobile 정책으로 다룰 수 있음
- VariantCondition의 기존 의미와 일치

필요해지기 전에는 WebGL/TV/VR 등을 추가하지 않는다.

---

## `DisplayOrientation`

초기 후보:

```csharp
public enum DisplayOrientation
{
    Unknown,
    Portrait,
    Landscape,
    Square
}
```

Unity `ScreenOrientation`은 AutoRotation 등 runtime 상태까지 포함한다.
Presentation 판정에는 현재 viewport의 기하학적 orientation이 더 명확하다.

M1에서는 **Resolution 기반 orientation**을 권장한다.

---

## Safe Area 표현

원본 정보는 pixel space로 보존한다.

```text
SafeAreaPixels
```

그리고 계산 프로퍼티:

```text
SafeAreaNormalized
```

를 제공한다.

정규화 규칙:

```text
x      / width
y      / height
width  / width
height / height
```

Invalid resolution에서는 divide-by-zero가 없어야 한다.

---

# 5. Validation Policy

`DisplayContext` 생성 시 입력을 어디까지 허용할지 결정한다.

권장:

```text
width <= 0 or height <= 0
→ invalid context를 조용히 만들지 않음
→ ArgumentOutOfRangeException
```

단, Unity Provider는 실제 `Screen` 값이 유효하지 않은 특수 초기화 시점을 고려해야 한다.

선택지:

### A. Context는 항상 valid

Provider가 invalid screen을 만나면 fallback 또는 생성 실패.

장점:
- Resolver가 defensive code를 덜 가짐
- 테스트가 단순

### B. Context.IsValid 허용

장점:
- initialization edge case 표현 가능

현재 프로젝트 규모에서는 **A를 우선 권장**한다.

M1 구현 시 Unity Editor/PlayMode에서 실제 initialization 문제 여부를 보고 확정한다.

---

# 6. Provider Design

## Interface

```csharp
public interface IDisplayContextProvider
{
    DisplayContext Current { get; }
}
```

또는:

```csharp
DisplayContext GetCurrent();
```

권장: `GetCurrent()`.

이유:

- 현재 값을 캡처한다는 의미가 분명함
- future caching처럼 보이지 않음

```csharp
public interface IDisplayContextProvider
{
    DisplayContext GetCurrent();
}
```

---

## Unity implementation

```text
UnityDisplayContextProvider
```

책임:

```text
Screen.width / height 읽기
Screen.safeArea 읽기
Application.platform → DisplayPlatform mapping
DisplayContext 생성
```

이 객체 외에는 M2 완료 시 해당 API를 직접 읽지 않는 것을 목표로 한다.

---

# 7. Context Lifetime

DisplayContext를 singleton state로 만들지 않는다.

권장 흐름:

```text
Navigate
  ↓
Resolve
  ↓
provider.GetCurrent()
  ↓
captured DisplayContext
  ↓
한 번의 Resolve 동안 동일 입력 사용
```

왜냐하면 Resolve 중간에 서로 다른 시점의 Screen 값을 읽으면 재현성이 떨어지기 때문이다.

M2에서 실제 소유자를 확정한다.

후보:

```text
UIResolver가 provider를 보유하고 Resolve 시작 시 capture
```

또는:

```text
상위 composition root가 DisplayContext를 만들어 Resolve에 전달
```

테스트 용이성 기준으로 두 번째가 더 순수하지만,
현재 Router API 변화량을 줄이려면 첫 번째도 가능하다.

M2에서 결정한다.

---

# 8. Aspect Class는 어디에 둘 것인가

M1에서 `AspectRatioClass` enum 파일을 만들 수는 있지만,
분류 임계값은 M3에서 확정한다.

권장 분리:

```text
DisplayContext
= 사실

AspectRatioClassifier
= 정책
```

즉 Context 자체에:

```text
AspectClass = Wide
```

를 저장하지 않는다.

같은 aspect ratio를 다른 프로젝트 정책에서는 다르게 분류할 수 있기 때문이다.

M1에서는 `AspectRatio`까지만 사실로 제공한다.

M3:

```text
AspectRatioClassifier
DisplayLayoutClass
```

를 구현한다.

따라서 초기 PLAN의 `AspectRatioClass.cs`는 M3로 미뤄도 된다.

---

# 9. Folder Layout

권장:

```text
Assets/scripts/Display
├─ DisplayContext.cs
├─ DisplayPlatform.cs
├─ DisplayOrientation.cs
├─ IDisplayContextProvider.cs
└─ UnityDisplayContextProvider.cs
```

아직 `Presentation/` 대규모 이동은 하지 않는다.

이유:

- 기존 UI 코드 이동과 기능 리팩터링을 동시에 하지 않음
- Unity meta churn 최소화
- M6에서 최종 구조 판단

---

# 10. Implementation Tasks

## M1.1 DisplayPlatform 작성

Mapping 표를 문서화한다.

예:

```text
WindowsEditor/WindowsPlayer → Desktop
OSXEditor/OSXPlayer         → Desktop
LinuxEditor/LinuxPlayer     → Desktop
Android                     → Mobile
IPhonePlayer                → Mobile
PS/Xbox/Switch 계열         → Console
그 외                       → Unknown
```

실제 Unity enum 존재 여부는 현재 Unity 6 API 기준 컴파일로 확인한다.

---

## M1.2 DisplayContext 작성

필수:

- immutable
- resolution
- safe area pixel
- platform
- computed aspect
- computed orientation
- normalized safe area

추가하지 않을 것:

- UI theme
- locale
- variant id
- CanvasScaler state
- camera size
- DPI 기반 물리 크기
- device model string

---

## M1.3 UnityDisplayContextProvider 작성

직접 Unity API 접근을 이 파일로 모은다.

`VariantCondition`은 아직 M1에서 즉시 제거하지 않아도 된다.

M1 끝에는 두 경로가 잠시 공존할 수 있다.

```text
old VariantCondition direct read
new DisplayContext provider
```

단 M2에서 old path를 반드시 제거한다.

---

## M1.4 Unit Test Assembly 생성

현재 별도 test assembly가 없는 상태를 기준으로 한다.

후보:

```text
Assets/Tests/EditMode/UIPresentationFlow.Tests.asmdef
Assets/Tests/EditMode/DisplayContextTests.cs
```

주의:

현재 production code가 Assembly-CSharp라면 asmdef test assembly의 참조 제약을 실제 Unity에서 확인해야 한다.

필요하면 M1에서 Runtime asmdef까지 만드는 대신,
Test Framework 구조를 최소 변경으로 먼저 세운다.

asmdef 분리가 예상보다 큰 구조 변경을 요구하면:

```text
BLOCKED / DECISION REQUIRED
```

로 기록하고 M6의 assembly 정리와 분리한다.

---

# 11. Tests

## DisplayContext construction

### 16:9

```text
Resolution = 1920×1080
Aspect ≈ 1.7777778
Orientation = Landscape
```

### 20:9

```text
Resolution = 2400×1080
Aspect ≈ 2.2222222
Orientation = Landscape
```

### 4:3

```text
Resolution = 2048×1536
Aspect ≈ 1.3333333
Orientation = Landscape
```

### Portrait

M3 주요 지원 범위가 아니어도 모델 테스트는 한다.

```text
1080×2400
Orientation = Portrait
```

---

## SafeArea normalization

예:

```text
Resolution = 2400×1080
SafeAreaPixels = (100, 0, 2200, 1080)

Normalized:
x = 100 / 2400
width = 2200 / 2400
```

float tolerance로 검증한다.

---

## Invalid resolution

결정한 policy에 맞춰:

```text
0×1080
1920×0
negative
```

테스트.

---

## Platform mapping

Provider의 private mapping을 테스트하기 어렵다면 mapping만 별도 pure helper로 분리할지 판단한다.

과도한 public API를 만들지 않는다.

---

# 12. Manual Verification

PlayMode에서 temporary debug 출력 또는 debugger로 확인:

```text
Screen.width / height
Screen.safeArea
Application.platform
↓
DisplayContext
```

다음 값 확인:

- GameView 16:9
- GameView wide preset
- current desktop platform

M1에서는 UI가 바뀔 필요가 없다.

---

# 13. Files to Add

예상:

```text
Assets/scripts/Display/DisplayContext.cs
Assets/scripts/Display/DisplayPlatform.cs
Assets/scripts/Display/DisplayOrientation.cs
Assets/scripts/Display/IDisplayContextProvider.cs
Assets/scripts/Display/UnityDisplayContextProvider.cs
```

Tests:

```text
Assets/Tests/EditMode/...
```

---

# 14. Files Not Yet Changed

M1에서 가능하면 다음은 그대로 둔다.

```text
VariantCondition.cs
UIVariantResolver.cs
UIResolver.cs
LayoutPatchSpec.cs
SampleScene.unity
```

이 파일들의 실제 연결 변경은 M2/M3에서 한다.

이렇게 해서 각 milestone의 원인을 분리한다.

---

# 15. Risks

## Risk A — Unity-specific types in DisplayContext

`Vector2Int`, `Rect`를 사용하면 pure .NET library는 아니다.

하지만 이 프로젝트는 Unity Presentation Framework이므로 허용 가능하다.

장점:
- 불필요한 자체 geometry 타입 방지

## Risk B — SafeArea coordinate convention

Unity SafeArea는 pixel rect다.

정규화가 bottom-left 기반인지, UI anchor 적용 시 어떤 변환을 사용하는지는 M4에서 검증한다.

M1에서는 raw fact만 정확히 보존한다.

## Risk C — Test assembly

현재 Assembly-CSharp 구조 때문에 EditMode test asmdef 구성이 예상보다 복잡할 수 있다.

테스트 구조를 위해 Runtime 전체를 성급하게 asmdef로 이동하지 않는다.

---

# 16. Completion Checklist

- [ ] DisplayContext 책임 확정
- [ ] DisplayPlatform 확정
- [ ] DisplayOrientation 확정
- [ ] SafeArea pixel/normalized convention 확정
- [ ] UnityDisplayContextProvider 생성
- [ ] Unity API 직접 접근 위치 목록 갱신
- [ ] 16:9 context test
- [ ] 20:9 context test
- [ ] 4:3 context test
- [ ] Portrait model test
- [ ] SafeArea normalization test
- [ ] invalid resolution policy test
- [ ] Runtime provider 수동 확인
- [ ] 기존 UI 동작 회귀 없음
- [ ] M2 진입 조건 충족

---

# 17. Completion Record

```text
Status: NOT STARTED

Files added:

DisplayContext fields:
Platform mapping:
Invalid resolution policy:
SafeArea convention:

Automatic tests:
- Passed:
- Failed:

Manual:
- GameView:
- Runtime provider:

Existing UI regression:

Known issues:

M2 entry approved: NO
```

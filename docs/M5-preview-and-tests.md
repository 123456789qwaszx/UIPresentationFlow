# M5 — Device Preview, Resolve Trace & Verification Tooling

## Goal

M1~M4에서 만든 구조를 개발자가 Unity Editor 안에서 즉시 관찰하고 재현할 수 있게 한다.

포트폴리오 관점의 핵심:

> “해상도 대응 코드를 작성했다”가 아니라  
> **“어떤 환경이 들어왔고, 어떤 규칙이 선택되었으며, 무엇이 적용됐는지 재현 가능한 도구로 보여준다.”**

---

# 1. Existing Editor Foundation

현재 `UIScreenSpecEditorWindow`가 이미 상당히 큰 authoring tool이다.

현재 확인된 책임:

- selected UIScreenSpecAsset bind
- slot hierarchy editing
- widget editing
- preset
- cached slot graph
- clipboard/editor helpers

따라서 Device Preview를 이 창에 처음부터 합치지 않는다.

권장:

```text
UIScreenSpecEditorWindow
= authored content 편집

UIDevicePreviewWindow
= environment simulation / resolve inspection
```

두 창은 선택 asset 공유 또는 button link만 한다.

---

# 2. Preview Scope

M5 Preview는 “실제 Android/iOS 기기 에뮬레이터”가 아니다.

목표:

```text
DisplayContext preset 생성
→ Resolver 실행
→ 선택된 UIScreen 생성/갱신
→ SafeArea visualization
→ Resolve Trace 표시
```

즉 Presentation Resolver test harness다.

---

# 3. Device Presets

최소:

```text
Desktop 16:9       1920×1080
Desktop 16:9 Hi    2560×1440
Mobile 19.5:9      2340×1080
Mobile 20:9        2400×1080
Tablet 4:3         2048×1536
```

SafeArea preset은 분리한다.

```text
None
Top
Sides
Bottom
Combined
```

즉 Device와 Cutout preset을 독립 선택 가능.

이유:

- 조합 테스트 쉬움
- 특정 vendor database 필요 없음
- policy 자체를 보여줌

---

# 4. Preview Data Model

권장 Editor-only model:

```text
DevicePreviewPreset
- Name
- Resolution
- DisplayPlatform
- optional default SafeArea preset
```

이것을 runtime DisplayContext에 합치지 않는다.

Preset은 authoring convenience다.

ScriptableObject로 만들지 plain editor data로 시작할 수 있다.

공유/확장 필요가 생기면 asset화.

---

# 5. Preview Modes

## Mode A — Resolver Only

Unity GameObject를 만들지 않고:

```text
UIScreenSpec
UIContext
DisplayContext
→ Resolve
→ 결과/Trace만 표시
```

가장 빠르고 테스트 친화적.

## Mode B — Visual Instantiate

Preview root 또는 demo scene에 실제 UIScreenFactory로 생성.

권장 둘 다 제공하되 순차 구현:

1. Resolver-only 먼저
2. Visual preview 후

---

# 6. Visual Preview 구현 선택

가능한 방식:

## A. Dedicated Preview Scene

`AdaptiveUIDemo.unity`의 Canvas를 사용.

장점:
- 실제 GameView와 동일
- 구현 단순

## B. EditorWindow 내부 PreviewRenderUtility

UGUI Canvas preview가 복잡해질 수 있음.

### 권장

처음에는 A.

Device Preview Window가:

- DisplayContext override를 설정
- demo scene resolver를 refresh
- 필요한 GameView size 안내/연동

정도로 시작.

GameView size 자동 제어는 Unity internal API에 의존할 수 있으므로 필수가 아니다.

핵심은 **Resolver input preview**다.

---

# 7. Context Override Architecture

Runtime global static에 preview context를 박지 않는다.

후보:

```text
IDisplayContextProvider
├─ UnityDisplayContextProvider
└─ FixedDisplayContextProvider
```

`FixedDisplayContextProvider`는 tests/editor에서 지정 Context 반환.

M5에서 매우 유용하다.

```text
FixedDisplayContextProvider(display)
```

같은 production-safe 작은 implementation이면 Editor assembly가 runtime 내부를 hack하지 않아도 된다.

---

# 8. Resolve Trace Schema

현재 trace는 자유 문자열 중심이다.

M5에서는 최소한 아래 항목이 확실히 보여야 한다.

```text
Input
- ScreenKey
- Action
- Theme
- Locale
- Resolution
- AspectRatio
- Orientation
- DisplayPlatform
- SafeArea

Classification
- LayoutClass

Matching
- evaluated variant
- match / miss
- priority
- miss reason (가능하면)

Winners
- Prefab
- Theme
- Layout

Application
- applied variant IDs
- patch count
```

---

# 9. Structured Diagnostics 권장

문자열만으로 UI를 만들면 parsing이 필요하다.

M5 시점에는 structured diagnostic model을 도입할 가치가 있다.

예:

```csharp
public sealed class UIResolveDiagnostics
{
    public DisplaySnapshot Display;
    public List<VariantEvaluation> Variants;
    public string PrefabWinner;
    public string ThemeWinner;
    public string LayoutWinner;
}
```

하지만 runtime core를 과하게 무겁게 만들지 않는다.

최소안:

```text
VariantDecision
- Id
- Priority
- Matched
- Reason
```

`Dump()`는 diagnostics에서 생성.

이렇게 하면 Editor UI와 Console이 같은 truth를 사용한다.

---

# 10. Miss Reason

가능하면 조건별 실패 이유를 표시한다.

예:

```text
Shop_Wide_Dark p100
MISS: theme expected Dark, actual Light

Shop_Wide p80
MATCH

Shop_Mobile p50
MISS: platform expected Mobile, actual Desktop
```

이 기능은 authoring/debugging 가치가 크다.

`VariantCondition`의 boolean `Matches`만으로는 이유를 잃는다.

M5에서:

```text
Evaluate(...) → ConditionEvaluation
```

으로 확장할지 판단한다.

M2에는 넣지 않아 범위를 분리한다.

---

# 11. Editor Window Layout

예:

```text
┌──────────────────────────────────────────────┐
│ UI Device Preview                            │
├──────────────────────────────────────────────┤
│ Screen        [ Home ▼ ]                     │
│ Device        [ Mobile 20:9 ▼ ]              │
│ Safe Area     [ Side Insets ▼ ]              │
│ Theme         [ Light ▼ ]                    │
│ Locale        [ ko-KR ]                      │
│                                              │
│ [ Resolve ] [ Refresh Visual ]               │
├───────────────────────┬──────────────────────┤
│ Input                 │ Result               │
│ 2400×1080             │ Layout: Home_Wide    │
│ Aspect 2.2222         │ Theme: Light         │
│ Mobile                │ Prefab: Home         │
│ Safe...               │ Patches: 2           │
├───────────────────────┴──────────────────────┤
│ Variant Evaluations                           │
│ ✓ Wide p100                                   │
│ × Tablet p100 : aspect class mismatch         │
│ ...                                           │
└──────────────────────────────────────────────┘
```

M5에서 예쁜 UI보다 정보 구조가 우선.

---

# 12. SafeArea Visualization

visual demo에서 toggle:

```text
Show Full Screen
Show Safe Area
```

overlay는 raycast off.

색상/스타일은 구현자가 선택.

포트폴리오 GIF에서 notch 대응을 즉시 이해할 수 있어야 한다.

---

# 13. Test Matrix Automation

M5에서 “여러 케이스를 클릭”하는 것뿐 아니라,
테이블 기반 EditMode test로 고정한다.

예:

```text
DeviceCase
- name
- resolution
- platform
- safeArea
- expectedLayoutClass
- expectedLayoutAsset
```

동일 case list를 test와 Editor preset에서 공유할지는 고민한다.

권장:

- production code에 test data 넣지 않음
- 값 중복이 크지 않으면 독립
- divergence가 실제 문제면 shared fixture asset 고려

---

# 14. Core Automatic Test Suites

## `DisplayContextTests`

M1 facts.

## `DisplayLayoutClassifierTests`

M3 thresholds.

## `VariantConditionTests`

M2 conditions.

## `UIVariantResolverTests`

priority/determinism.

## `LayoutSelectionTests`

device → layout.

## `SafeAreaTests`

M4 conversion/idempotence.

## `ResolveDiagnosticsTests`

필요한 structured output.

---

# 15. Integration Test Level

EditMode에서 GameObject/RectTransform을 만들어:

```text
UIScreen
WidgetHandle
LayoutSpecPatch.Apply
```

후 실제 RectTransform 값 확인.

최소 한 개는 필요하다.

왜냐하면 pure resolver test만 통과해도 실제 patch target nameTag가 틀리면 화면은 안 바뀐다.

Integration test 예:

```text
create UIScreen GO
register widget "PrimaryPanel"
apply Wide LayoutPatch
assert anchors/position/size
```

가능하면 actual ScriptableObject temporary instance 사용.

---

# 16. Preview Regression Workflow

각 milestone 이후 개발자가 수행 가능한 workflow:

```text
1. UIDevicePreviewWindow open
2. Screen 선택
3. 16:9
4. 20:9
5. 4:3
6. SafeArea on
7. Resolve Trace 확인
8. Test Runner
```

README에도 그대로 넣을 수 있다.

---

# 17. Implementation Tasks

## M5.1 FixedDisplayContextProvider

tests/editor reuse.

## M5.2 Diagnostic model 결정

string-only 유지 여부 평가.
Editor가 parsing 없이 결과를 표시할 최소 structure 추가.

## M5.3 Resolver-only Preview Window

가장 먼저 완성.

## M5.4 Device/Safe presets

5 × safe presets.

## M5.5 Visual refresh

demo scene integration.

## M5.6 SafeArea overlay

## M5.7 Test suite 통합

## M5.8 One-click full matrix

선택 기능:

Editor 버튼:

```text
Run Device Matrix
```

가 Test Runner를 직접 호출하는 복잡한 기능은 불필요.

대신 preview window 안에서 resolver matrix를 실행해 PASS/FAIL 표시할 수 있다.

---

# 18. Files Expected to Add

Editor:

```text
Assets/scripts/UI/Editor/Tools/UIDevicePreviewWindow.cs
Assets/scripts/UI/Editor/Internal/DevicePreviewPreset.cs
```

Display:

```text
FixedDisplayContextProvider.cs
```

Diagnostics는 Core/Patcher 적절한 위치.

Tests 다수.

---

# 19. Do Not Do

M5에서 하지 않을 것:

- Unity internal GameView API에 과도하게 의존
- 실제 스마트폰 전 모델 DB 구축
- screenshot golden-image comparison
- custom device emulator
- runtime debug console framework
- production telemetry

---

# 20. Completion Checklist

- [ ] resolver-only preview
- [ ] 16:9 preset
- [ ] 2560×1440 preset
- [ ] 19.5:9 preset
- [ ] 20:9 preset
- [ ] 4:3 preset
- [ ] safe area presets
- [ ] input summary 표시
- [ ] layout class 표시
- [ ] matched variants 표시
- [ ] winner prefab/theme/layout 표시
- [ ] SafeArea overlay
- [ ] visual refresh
- [ ] DisplayContext tests
- [ ] Classifier tests
- [ ] Variant tests
- [ ] Layout selection tests
- [ ] SafeArea tests
- [ ] 최소 1개 RectTransform integration test
- [ ] 전체 tests green
- [ ] M6 entry 승인

---

# 21. Completion Record

```text
Status: NOT STARTED

Preview Window:
Diagnostics:
Visual Preview:
SafeArea Overlay:

Preset count:

Automatic tests:
- total:
- passed:
- failed:

Integration tests:

Known editor limitations:

M6 entry approved: NO
```

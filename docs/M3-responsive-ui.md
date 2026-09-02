# M3 — Responsive UI via Variant + LayoutPatch

## Goal

기존 `UIVariantRule + VariantCondition + LayoutPatchSpec` 구조를 실제 다중 화면비 문제에 적용한다.

M3의 목표는 새로운 responsive framework를 만드는 것이 아니다.

> **현재 프로젝트가 이미 가진 Variant/Patch 설계가 왜 필요한지 실제 문제로 증명한다.**

---

# 1. Responsibility Boundary

가장 먼저 `CanvasScaler`와 Variant Layout의 책임을 분리한다.

## CanvasScaler

담당:

```text
같은 layout을 유지하면서
전체 UI 단위가 화면 크기에 따라 연속적으로 scaling
```

## Variant + LayoutPatch

담당:

```text
화면비가 의미 있게 달라졌을 때
anchor / position / size / active 정책을 구조적으로 변경
```

예:

```text
1920×1080 → 2560×1440
같은 16:9
→ CanvasScaler가 주로 처리
→ 동일 Variant

1920×1080 → 2400×1080
16:9 → 20:9
→ Wide Variant 선택
→ 일부 좌우 위치/폭 재배치

1920×1080 → 2048×1536
16:9 → 4:3
→ Compact/Tablet Variant
→ 콘텐츠 폭/버튼 배치 변경
```

---

# 2. Current Canvas Baseline

현재 SampleScene의 `CanvasForTest`는 조사 시점에:

```text
CanvasScaler
UI Scale Mode = Constant Pixel Size
Scale Factor = 1
```

이다.

M3에서 reference baseline을 정한다.

권장:

```text
UI Scale Mode = Scale With Screen Size
Reference Resolution = 1920 × 1080
Screen Match Mode = Match Width Or Height
```

`Match` 값은 대표 UI 테스트 후 결정한다.

무조건 0.5를 쓰지 않는다.

---

# 3. Reference Resolution

고정:

```text
1920 × 1080
16:9
```

이 해상도가 authored base layout의 기준이다.

즉 BaseLayout은:

> 가장 일반적인 16:9 landscape 화면의 intended layout.

Wide/Tablet은 base를 대체하는 variant layout이다.

---

# 4. Device Matrix

필수:

| ID | Resolution | Aspect | Expected Class |
|---|---:|---:|---|
| D1 | 1920×1080 | 1.7778 | Standard |
| D2 | 2560×1440 | 1.7778 | Standard |
| D3 | 2340×1080 | 2.1667 | Wide |
| D4 | 2400×1080 | 2.2222 | Wide |
| D5 | 2048×1536 | 1.3333 | Compact/Tablet |

추가 boundary test:

```text
16:10 = 1.6
3:2   = 1.5
18:9  = 2.0
21:9  ≈ 2.333
```

---

# 5. Aspect Classification

`VariantCondition.Range`만으로 모든 Screen asset에 숫자를 반복 입력하면 authored data가 퍼진다.

M3에서는 classifier 도입을 권장한다.

예:

```csharp
public enum DisplayLayoutClass
{
    Compact,
    Standard,
    Wide,
    UltraWide
}
```

정확한 threshold는 대표 UI를 보며 확정한다.

초기 실험 후보:

```text
Compact   < 1.60
Standard  1.60 ~ < 2.00
Wide      2.00 ~ < 2.30
UltraWide >= 2.30
```

하지만 이 값은 **계획 단계의 가설**이다.

실제 UI 검증 전 확정하지 않는다.

---

# 6. Classifier 위치

권장:

```text
Assets/scripts/Display/DisplayLayoutClass.cs
Assets/scripts/Display/DisplayLayoutClassifier.cs
```

입력:

```text
float aspectRatio
```

출력:

```text
DisplayLayoutClass
```

Classifier는 pure function.

`DisplayContext`는 class를 저장하지 않는다.

---

# 7. VariantCondition Integration

선택지는 두 가지다.

## A. 기존 Range만 유지

각 rule:

```text
useAspectRatio = true
Range 2.0 ~ 2.3
```

장점:
- 코드 변경 적음

단점:
- 여러 UIScreenSpec에 threshold 중복
- 정책 변경 어려움

## B. Aspect Class condition 추가

예:

```text
useLayoutClass
layoutClass = Wide
```

장점:
- authoring 명확
- Trace 명확
- centralized threshold

포트폴리오 목표에는 B가 더 적합하다.

단 기존 Range는 고급 custom case를 위해 유지할 수 있다.

권장 condition precedence:

```text
if useLayoutClass:
    classifier result match

if useAspectRatio:
    raw custom aspect rule match
```

둘 다 켜면 AND.

---

# 8. Representative Screen 선정

M0에서 선정한 하나의 화면을 M3의 canonical demo로 사용한다.

필수 layout target 예:

```text
Header
PrimaryContent
BottomControls
LeftAction
RightAction
```

또는 dialogue-style:

```text
SpeakerName
DialoguePanel
ChoiceArea
TopActions
```

중요한 것은 디자인 예쁨보다 변화가 설명 가능해야 한다.

---

# 9. Stable Layout Target Rule

현재 LayoutPatch는:

```text
WidgetLayoutPatch.nameTag
→ UIScreen.GetWidgetHandle(nameTag)
```

이다.

따라서 M3부터 authored convention을 명확히 한다.

> Responsive 대상은 GameObject name이 아니라 `WidgetSpec.nameTag`를 안정 key로 사용한다.

규칙:

- 화면 내 responsive target nameTag unique
- variant마다 같은 의미의 widget은 같은 nameTag
- display class 이름을 nameTag에 넣지 않음
  - `DialoguePanel` O
  - `DialoguePanel_Wide` X

Variant가 위치를 바꾸지 identity를 바꾸지 않는다.

---

# 10. LayoutPatch Strategy

## Base Layout

16:9 intended state.

가능하면 prefab/WidgetSpec 기본 Rect에 둔다.

`baseLayout`에는 정말 공통 patch가 필요한 경우만 둔다.

## Wide

예:

```text
PrimaryContent
- max visual width 유지
- 양 옆 공간이 늘어도 중앙 내용이 과도하게 stretch되지 않음

Left/Right Actions
- viewport edge 또는 safe content edge 정책에 맞게 이동

BottomPanel
- 필요하면 width 제한
```

## Compact / Tablet

예:

```text
horizontal spacing 축소
wide two-column → narrower stack/overlap 방지
text area max width 조정
button group 위치 변경
```

---

# 11. LayoutPatch 기능 확장 원칙

현재 기능:

```text
active
anchorMin/max
pivot
anchoredPosition
sizeDelta
```

먼저 이것으로 해결한다.

다음이 실제로 필요할 때만 추가:

```text
offsetMin/offsetMax
LayoutElement min/preferred/flexible
scale
```

추가 기준:

1. canonical screen에서 현재 patch로 표현 불가능
2. prefab 자체를 variant별로 복제하는 것보다 patch가 명확
3. 최소 2개 이상의 실제 사용처 또는 충분히 보편적인 layout property

한 화면 한 곳을 위해 generic patch engine을 크게 만들지 않는다.

---

# 12. Prefab Override Policy

UIVariantRule은 Prefab 자체도 override할 수 있다.

Responsive layout에서는 가능한 한 prefab override를 사용하지 않는다.

우선순위:

```text
1. Same Prefab + LayoutPatch
2. Same Spec + minimal alternate prefab
3. Completely separate UIScreenSpec
```

왜냐하면 이번 프로젝트의 목적은:

> 동일 authored screen이 환경에 따라 adaptive하게 변하는 것

을 보여주는 것이기 때문이다.

완전히 다른 정보 구조가 필요한 경우에만 prefab override.

---

# 13. CanvasScaler Experiment

M3 초반에 다음 matrix를 실험한다.

후보 Match:

```text
0.0 = width
0.5 = balanced
1.0 = height
```

각각에서:

- 16:9 desktop
- 20:9 mobile
- 4:3 tablet

관찰:

```text
text readability
button physical impression
vertical overflow
horizontal whitespace
```

최종 Match를 이유와 함께 기록한다.

필요하면 screen-specific scaler를 만들지 않는다.
Canvas root policy는 공통으로 유지한다.

---

# 14. Implementation Tasks

## M3.1 Canonical Device Matrix test 먼저 작성

코드 구현 전 Expected class를 test table로 작성.

## M3.2 DisplayLayoutClassifier 구현

pure tests.

## M3.3 VariantCondition class support

`useLayoutClass` 도입 여부 확정 후 연결.

## M3.4 CanvasScaler baseline 변경

SampleScene 또는 새 DemoScene 중 하나.

권장:

기존 SampleScene이 실험 흔적이 많다면:

```text
Assets/Scenes/AdaptiveUIDemo.unity
```

신규 데모 씬 생성.

기존 SampleScene은 baseline/reference로 남긴다.

## M3.5 Canonical UIScreen 작성/정리

- stable tags
- 16:9 base
- interactions 최소

## M3.6 Wide LayoutPatch asset

예:

```text
Sample_Wide_Layout.asset
```

## M3.7 Compact LayoutPatch asset

예:

```text
Sample_Compact_Layout.asset
```

## M3.8 UIScreenSpec variant rules 연결

```text
Standard → Base
Wide     → Wide Layout
Compact  → Compact Layout
```

UltraWide는 Wide와 동일해도 된다.
실제 차이가 없으면 separate asset을 만들지 않는다.

---

# 15. Automatic Tests

## Classifier

- exact thresholds
- threshold epsilon 주변
- 4:3
- 16:9
- 20:9
- 21:9

## Resolver selection

Device matrix 각각:

```text
DisplayContext
→ Resolve
→ expected Layout asset
```

## Base fallback

분류 조건 없는 경우 base layout.

## Priority interaction

예:

```text
Wide rule p100
DarkTheme rule p50
```

둘 다 match할 때:

```text
Wide Layout
Dark Theme
```

field-independent policy 확인.

---

# 16. Manual Visual Verification

각 device에서 동일 checklist.

## Geometry

- [ ] 화면 밖 widget 없음
- [ ] widget overlap 없음
- [ ] text clipping 없음
- [ ] controls accessible
- [ ] visual hierarchy 유지

## Standard

- [ ] authored base와 동일
- [ ] 1920×1080 / 2560×1440 같은 layout class

## Wide

- [ ] 중앙 콘텐츠 과도한 stretch 없음
- [ ] edge spacing 의도 유지
- [ ] 빈 공간이 구조적으로 자연스러움

## Tablet

- [ ] 가로 공간 부족으로 overlap 없음
- [ ] panel width/spacing 적절
- [ ] button group 잘림 없음

---

# 17. Screenshot Record

M6 포트폴리오를 위해 M3부터 결과를 남긴다.

```text
docs/images/m3/
├─ 1920x1080.png
├─ 2400x1080.png
└─ 2048x1536.png
```

실제 repo에 넣을지는 M6에서 결정.
일단 로컬 증거를 보관한다.

---

# 18. Files Expected to Add/Change

예상:

```text
Assets/scripts/Display/DisplayLayoutClass.cs
Assets/scripts/Display/DisplayLayoutClassifier.cs

Assets/scripts/UI/Patcher/VariantCondition.cs
```

Assets:

```text
LayoutPatchSpec assets
UIScreenSpecAsset / Catalog entries
AdaptiveUIDemo.unity (선택)
```

Tests:

```text
DisplayLayoutClassifierTests
LayoutSelectionTests
```

---

# 19. Risks

## Too many aspect classes

실제 차이가 없는 class는 만들지 않는다.

## Magic thresholds

threshold는 인기 기기 목록을 외워서 정하지 않는다.
대표 UI가 깨지는 지점을 기준으로 정한다.

## CanvasScaler와 Patch 중복

같은 문제를 둘 다 보정하면 layout이 불안정해진다.

## Pixel-perfect obsession

모든 device에서 동일 pixel 위치가 목표가 아니다.

목표는:

```text
relative composition
readability
interaction safety
```

유지다.

---

# 20. Completion Checklist

- [ ] CanvasScaler 책임 문서화
- [ ] Variant/Patch 책임 문서화
- [ ] reference resolution 확정
- [ ] canonical screen 확정
- [ ] stable nameTag convention 적용
- [ ] layout classifier thresholds 확정
- [ ] classifier automatic tests 통과
- [ ] Standard layout
- [ ] Wide layout
- [ ] Compact/Tablet layout
- [ ] 1920×1080 검증
- [ ] 2560×1440 검증
- [ ] 2340×1080 검증
- [ ] 2400×1080 검증
- [ ] 2048×1536 검증
- [ ] screenshot evidence
- [ ] Console error 0
- [ ] M4 entry 승인

---

# 21. Completion Record

```text
Status: NOT STARTED

Reference Resolution:
CanvasScaler:
- Scale Mode:
- Match:

Layout classes:
Thresholds:

Canonical screen:

Assets:
- Base:
- Wide:
- Compact:

Automatic tests:

Device matrix:
- 1920×1080:
- 2560×1440:
- 2340×1080:
- 2400×1080:
- 2048×1536:

Known visual limitations:

M4 entry approved: NO
```

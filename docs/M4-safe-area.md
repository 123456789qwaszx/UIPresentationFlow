# M4 — Safe Area Policy & Application

## Goal

노치, rounded corner, home indicator 등의 비직사각형 디바이스 제약을
단순한 “전체 UI 축소”가 아니라 **Presentation Policy**로 처리한다.

핵심:

> 화면 전체를 쓰는 요소와 사용자 안전 영역 안에 있어야 하는 요소를 분리한다.

---

# 1. Why Safe Area Comes After M3

Safe Area를 Aspect Ratio와 동시에 구현하지 않는다.

M3에서 먼저:

```text
Resolution / Aspect
→ Layout Variant
```

를 안정화한다.

M4에서는 그 결과 위에:

```text
SafeArea inset
```

을 추가한다.

이렇게 해야 문제가 생겼을 때 원인이:

- screen ratio
- safe area

중 무엇인지 분리할 수 있다.

---

# 2. DisplayContext Input

M1에서 이미 다음을 확보한다.

```text
Resolution
SafeAreaPixels
SafeAreaNormalized
```

M4는 이 데이터를 소비한다.

`Screen.safeArea`를 M4 코드에서 다시 직접 읽지 않는다.

---

# 3. Safe Area Policy Categories

모든 widget에 동일 SafeArea를 강제하지 않는다.

## Full Bleed

화면 끝까지 확장 가능:

```text
Background
Decorative Frame
Vignette
Non-interactive side art
```

## Safe Content

safe area 내부를 보장:

```text
Primary Button
Navigation
Back / Close
Currency controls
Critical HUD
Text requiring full readability
```

## Contextual

화면에 따라 결정:

```text
Dialogue panel
non-critical label
decorative badge
```

초기 framework에서는 2단계만 구현해도 충분하다.

```text
FullBleed
SafeContent
```

Contextual은 authored hierarchy로 어느 root 아래 둘지 결정.

---

# 4. Preferred Architecture

SafeArea를 각 widget patch에 매번 계산하지 않는다.

권장:

```text
Canvas
└─ ScreenRoot
   ├─ FullBleedRoot
   │   └─ ...
   │
   └─ SafeAreaRoot
       └─ ...
```

`SafeAreaRoot` RectTransform의 anchors를 DisplayContext에서 계산한다.

그러면 SafeContent widget은 기존 layout logic을 그대로 사용한다.

장점:

- patch asset에 notch 숫자 중복 없음
- SafeArea와 Variant Layout 책임 분리
- Full bleed 지원 쉬움
- Trace 명확

---

# 5. Existing UIScreen/Slot Structure와의 결합

현재 `ScreenTemplate`은 root 자체에 `UIScreen`과 `UISlot(id=Root)`가 붙어 있다.

M4에서 바로 모든 prefab hierarchy를 강제 변경하면 migration 비용이 크다.

두 전략 비교.

## A. Template에 SafeAreaRoot 추가

```text
ScreenTemplate
├─ FullBleedRoot
└─ SafeAreaRoot (UISlot Root)
```

장점:
- hierarchy 명확

단점:
- 기존 template/spec migration 필요

## B. 별도 SafeArea component가 특정 RectTransform을 조정

예:

```text
SafeAreaFitter
[target RectTransform]
```

장점:
- 기존 hierarchy 변경 적음

단점:
- FullBleed/SafeContent 구분이 덜 명시적

### 권장

포트폴리오 데모는 A 구조를 목표로 하되,
기존 ScreenTemplate migration은 canonical screen에서 먼저 검증한다.

전체 시스템을 일괄 변환하지 않는다.

---

# 6. Proposed Component

예:

```csharp
public sealed class SafeAreaFitter : MonoBehaviour
{
    [SerializeField] RectTransform target;

    public void Apply(in DisplayContext display);
}
```

하지만 `SafeAreaFitter`가 Provider를 직접 읽으면 안 된다.

호출:

```text
Factory / Presentation Applicator
→ SafeAreaFitter.Apply(display)
```

또는 SafeArea도 patch로 표현할 수 있다.

---

# 7. SafeArea as Patch? Decision

현재 시스템 언어와 맞추기 위해:

```text
SafeAreaPatch : IUIPatch
```

를 생각할 수 있다.

장점:
- Factory의 기존 patch phase 재사용

예:

```text
ThemePatch
LayoutPatch
SafeAreaPatch
```

단점:
- SafeArea는 authored `LayoutPatchSpec`과 성격이 다름
- runtime context-dependent patch

### 권장

`IDisplayPatch` 같은 새 계층을 만들기 전에,
`SafeAreaPatch`가 단순 `IUIPatch`로 충분한지 canonical screen에서 검증한다.

가능한 흐름:

```text
UIResolver
→ authored theme/layout patches

UIScreenFactory
→ compose

Display policy
→ SafeArea apply

authored layout patch와 safe area의 적용 순서 결정
```

---

# 8. Apply Order

중요한 결정.

## Option A

```text
Compose
→ SafeArea root
→ LayoutPatch
```

SafeArea root 내부 좌표 기준으로 LayoutPatch가 동작.

장점:
- authored layout이 safe content 영역을 기준으로 자연스럽게 적용

## Option B

```text
Compose
→ LayoutPatch
→ SafeArea
```

LayoutPatch 후 root가 축소됨.

대부분 결과는 비슷할 수 있지만 stretch anchor에서 차이가 난다.

### 권장

```text
SafeArea root geometry 설정
→ child LayoutPatch
```

즉 safe coordinate space를 먼저 만들고,
그 안에서 variant layout을 적용.

단 FullBleed widget은 safe root 밖에 둔다.

Factory pipeline에서 단계가 명확해야 한다.

---

# 9. Normalized Safe Area Conversion

Unity SafeArea pixel:

```text
(x, y, width, height)
```

Resolution:

```text
W, H
```

anchors:

```text
anchorMin.x = xMin / W
anchorMin.y = yMin / H
anchorMax.x = xMax / W
anchorMax.y = yMax / H
```

적용 시:

```text
offsetMin = 0
offsetMax = 0
```

또는:

```text
anchoredPosition/sizeDelta reset
```

을 보장한다.

RectTransform stale offset 때문에 두 번 적용할 때 drift하지 않아야 한다.

---

# 10. Idempotence

같은 DisplayContext로 SafeArea를 여러 번 Apply해도 결과가 같아야 한다.

필수 test:

```text
Apply(context)
capture rect
Apply(context)
rect unchanged
```

Orientation/preview switch에서도 누적 변형이 없어야 한다.

---

# 11. Mock SafeArea Presets

실제 device database를 만들지 않는다.

M4/M5 검증용 가상 preset:

## None

```text
0,0,W,H
```

## Top Notch

예:

```text
0, 60, W, H-60
```

## Side Insets

landscape:

```text
100,0,W-200,H
```

## Bottom Indicator

```text
0,40,W,H-40
```

## Combined

```text
100,40,W-200,H-80
```

정확한 실제 iPhone pixel 복제가 목적이 아니다.

layout policy를 검증할 enough adversarial cases가 목적.

---

# 12. Interaction with Aspect Variant

조합 matrix:

```text
Standard + None
Standard + Insets
Wide + None
Wide + Side Insets
Compact + None
Compact + Combined
```

최소 6 case.

M4의 핵심은 개별 기능보다 조합 안정성이다.

---

# 13. Background Policy

배경이 SafeArea에 의해 줄어드는 것을 금지한다.

Demo hierarchy에서 명확히 보여준다.

```text
FullBleed Background
SafeArea Controls
```

포트폴리오 screenshot에도 SafeArea visualization overlay를 켜서 설명할 수 있게 한다.

---

# 14. Debug Visualization

M5에서 정식 Preview UI를 만들기 전에,
M4에서 간단한 debug visualization을 둘 수 있다.

예:

- SafeArea Rect outline
- Full screen rect
- inset values log

production runtime에 항상 표시하지 않는다.

#if UNITY_EDITOR 또는 debug component로 제한.

---

# 15. Implementation Tasks

## M4.1 Safe Area policy document 확정

canonical screen widget을:

```text
FullBleed
SafeContent
```

로 분류.

## M4.2 SafeArea root/applicator prototype

canonical screen only.

## M4.3 Apply order 검증

stretch anchors 포함 widget으로 A/B 결과 확인.

## M4.4 Idempotence 보장

reset/anchor 적용 순서 확정.

## M4.5 Mock contexts 작성

test fixture/helper.

## M4.6 Aspect × SafeArea matrix 검증

M3 device cases와 조합.

## M4.7 Template migration 결정

canonical prototype 성공 후:

```text
ScreenTemplate를 공식 SafeArea hierarchy로 변경할지
또는 optional fitter를 둘지
```

결정.

---

# 16. Automatic Tests

## Normalization

resolution/safe rect → normalized anchors.

## Full area

SafeArea == full resolution
→ anchor min 0,0 max 1,1

## Insets

expected anchors tolerance.

## Idempotence

same result twice.

## Context change

A safe area → B safe area
→ B가 정확히 적용, A offset 잔여 없음.

## Invalid safe rect

다음 입력 정책 결정:

- negative
- outside resolution
- zero size

권장:
Context construction에서 clamp하지 말고 invalid를 reject하거나,
Provider boundary에서 clamp.

silent strange layout 금지.

---

# 17. Manual Tests

각 case에서:

- [ ] Background full bleed
- [ ] Back/Close safe
- [ ] primary controls safe
- [ ] text clipping 없음
- [ ] layout variant 유지
- [ ] safe area toggle 후 drift 없음
- [ ] preview 반복 적용 drift 없음

---

# 18. Files Expected to Add/Change

후보:

```text
Assets/scripts/Display/SafeAreaUtility.cs
Assets/scripts/UI/Patcher/SafeAreaPatch.cs
```

또는:

```text
Assets/scripts/UI/Core/SafeAreaFitter.cs
```

실제 구조는 prototype 결과로 결정한다.

Prefab/Scene:

```text
ScreenTemplate.prefab
AdaptiveUIDemo.unity
```

Tests:

```text
SafeAreaTests.cs
SafeAreaIntegrationTests.cs
```

---

# 19. Risks

## Root hierarchy migration

기존 UISlot Root convention과 충돌 가능.

## Nested Canvas

SafeArea root가 어떤 Canvas pixel space를 기준으로 하는지 확인 필요.

## CanvasScaler

normalized anchor 방식이면 CanvasScaler와 비교적 독립적이지만,
actual rendered geometry를 device matrix에서 확인해야 한다.

## Over-generalization

foldable hinge/cutout polygon까지 이 단계에 넣지 않는다.

---

# 20. Completion Checklist

- [ ] SafeArea policy 2종 확정
- [ ] canonical screen 분류
- [ ] normalized conversion test
- [ ] full area test
- [ ] inset test
- [ ] idempotence test
- [ ] context switching test
- [ ] Standard + None
- [ ] Standard + Insets
- [ ] Wide + None
- [ ] Wide + Insets
- [ ] Compact + None
- [ ] Compact + Insets
- [ ] background full bleed 확인
- [ ] critical controls safe 확인
- [ ] template migration 결정
- [ ] Console error 0
- [ ] M5 entry 승인

---

# 21. Completion Record

```text
Status: NOT STARTED

SafeArea architecture:
Apply order:
Invalid rect policy:
Template strategy:

Automatic tests:

Matrix:
- Standard/None:
- Standard/Inset:
- Wide/None:
- Wide/Inset:
- Compact/None:
- Compact/Inset:

Idempotence:

Known issues:

M5 entry approved: NO
```

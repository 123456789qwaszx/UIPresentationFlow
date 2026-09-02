# M7 — Optional Spine Character Presentation

## Status

**OPTIONAL**

M0~M6 Core가 완료되기 전에는 착수하지 않는다.

이 문서는 지금 구현 범위가 아니라,
Core UI 환경 모델이 완성된 뒤의 제한된 확장 계획이다.

---

# 1. Goal

Spine 기능 자체를 깊게 만드는 것이 목적이 아니다.

검증하고 싶은 질문은 하나다.

> `DisplayContext`가 UI 전용 편법이 아니라 Character Presentation에서도 재사용 가능한 공통 환경 모델인가?

따라서 M7은 Architecture Extension Test다.

---

# 2. Preconditions

다음이 전부 참이어야 한다.

- [ ] M0 DONE
- [ ] M1 DONE
- [ ] M2 DONE
- [ ] M3 DONE
- [ ] M4 DONE
- [ ] M5 DONE
- [ ] M6 DONE
- [ ] Core tests green
- [ ] Device Preview working
- [ ] README Core 완료
- [ ] Spine 없이도 portfolio story 완결

하나라도 아니면 M7 시작 금지.

---

# 3. Non-goals

하지 않는다.

```text
LipSync
Expression Graph
Animation Graph
Timeline replacement
Command framework
Move/Fade/Rotate sequence system
Dialogue integration
Yarn integration
Full character state machine
```

이 프로젝트가 `ked-presentation-runtime`과 겹치지 않게 한다.

---

# 4. Minimal Architecture

```text
Assets/scripts/Character
├─ Core
│  ├─ ICharacterView.cs
│  ├─ CharacterAnchor.cs
│  ├─ CharacterLayoutSpec.cs
│  ├─ CharacterLayoutResolver.cs
│  └─ ResolvedCharacterLayout.cs
│
└─ Spine
   └─ SpineCharacterView.cs
```

Display는 기존:

```text
Assets/scripts/Display
```

재사용.

---

# 5. CharacterView Boundary

최소:

```csharp
public interface ICharacterView
{
    void SetVisible(bool visible);
    void Play(string animation, bool loop);
}
```

필요하기 전에는 다음을 넣지 않는다.

```text
Skin
Track index
Mix duration
Attachment
Event callbacks
Expression
```

Spine API가 밖으로 새지 않게 하는 최소 adapter일 뿐.

---

# 6. Spine Component Choice

두 경로가 있다.

```text
SkeletonGraphic
= Canvas / UI

SkeletonAnimation
= World Space
```

M7 시작 시 데모 목표에 맞춰 하나만 선택.

UI와 함께 Device Layout을 보여주는 것이 목적이면
초기에는 `SkeletonGraphic`이 단순할 가능성이 높다.

하지만 “Spine은 UI 하위 기능이 아니다” 원칙은 유지한다.

Character domain은 `UI/Spine` 아래에 넣지 않는다.

---

# 7. Character Layout Model

직접 pixel position을 authored intent로 쓰지 않는다.

```text
CharacterAnchor.Left
CharacterAnchor.Center
CharacterAnchor.Right
```

Spec 예:

```text
anchor
baseOffset
scale
verticalAlignment
```

최소만.

---

# 8. Resolver Flow

```text
CharacterLayoutSpec
        +
DisplayContext
        ↓
CharacterLayoutResolver
        ↓
ResolvedCharacterLayout
        ↓
Character View / RectTransform
```

UI와 언어를 맞춘다.

```text
UI
UIScreenSpec
→ UIVariantResolver
→ ResolvedUIScreen

Character
CharacterLayoutSpec
→ CharacterLayoutResolver
→ ResolvedCharacterLayout
```

이 대칭성이 M7의 핵심 포트폴리오 가치다.

---

# 9. Layout Cases

최소 2-character composition:

```text
Left Character
Right Character
Bottom UI
```

Device matrix:

```text
1920×1080
2400×1080
2048×1536
```

확인:

- 얼굴 crop
- 상대적 distance
- UI overlap
- visible focus area
- scale consistency

---

# 10. UI Collision Policy

복잡한 collision solver를 만들지 않는다.

Character layout은 UI의 실제 Rect를 실시간 검사하지 않는다.

대신 shared authored presentation zone을 둔다.

예:

```text
Display framing
├─ Character Stage
└─ UI Reserved Area
```

필요하면 CharacterLayoutSpec에 bottom reserved margin 정도.

full constraint system은 scope 밖.

---

# 11. Shared DisplayContext Test

M7 성공 조건:

동일한 Preview Window device preset을 사용했을 때:

```text
UI Resolver
Character Resolver
```

가 같은 `DisplayContext`를 받는다.

Preview trace:

```text
Display
- 2400×1080
- Wide

UI
- Dialogue_Wide

Character
- TwoShot_Wide
```

까지 보이면 충분하다.

---

# 12. Spine Runtime Installation

M7 시작 시 당시 최신 Spine Unity Runtime과 사용하는 Spine Editor export version compatibility를 공식 문서로 재확인한다.

현재 PLAN 작성 시점에는 패키지를 설치하지 않는다.

설치 commit은 Core와 분리한다.

---

# 13. Asset Licensing

포트폴리오 공개 repository이므로 Spine sample/character asset의 배포 가능 여부 확인.

가능하면:

- 직접 만든 간단한 rig
- Spine 공식 배포 가능 sample
- repository에는 runtime만 참조하고 유료 asset 미포함

정책을 명확히 한다.

---

# 14. Tests

Spine 자체 rendering을 unit test하지 않는다.

pure test:

```text
CharacterLayoutResolver
```

- Standard
- Wide
- Compact
- Left/Right anchors

integration manual:

- animation plays
- visibility
- layout changes

---

# 15. Implementation Tasks

## M7.1 Spine dependency 확인/설치

## M7.2 ICharacterView

## M7.3 Character layout model

## M7.4 pure resolver

## M7.5 Spine adapter

## M7.6 two-character demo

## M7.7 Device Preview integration

## M7.8 README Future → Implemented section update

---

# 16. Completion Checklist

- [ ] Core M0~M6 complete
- [ ] Spine compatibility checked
- [ ] license safe demo asset
- [ ] ICharacterView minimal
- [ ] CharacterAnchor
- [ ] CharacterLayoutSpec
- [ ] CharacterLayoutResolver
- [ ] ResolvedCharacterLayout
- [ ] Spine adapter
- [ ] 16:9 character framing
- [ ] 20:9 character framing
- [ ] 4:3 character framing
- [ ] UI collision visually acceptable
- [ ] same DisplayContext used
- [ ] Preview trace shows UI + Character
- [ ] no command/timeline scope creep
- [ ] tests/manual verification complete

---

# 17. Completion Record

```text
Status: OPTIONAL / NOT STARTED

Core prerequisite:
Spine runtime version:
Spine export version:
Demo asset/license:

Character API:
Layout model:

Tests:
- Standard:
- Wide:
- Compact:

Manual:
- Play:
- Visible:
- UI overlap:

Preview integration:

M7 DONE: NO
```

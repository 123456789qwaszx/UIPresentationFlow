# UIPresentationFlow 개선 PLAN

## 1. 목표

이번 개선의 목표는 두 가지다.

1. 실제 게임 UI에 가까운 Demo를 만든다.
2. `ked-presentation-runtime`에서 검증된 좋은 요소를 `UIPresentationFlow` 방식으로 흡수한다.

최종적으로는 단순한 아키텍처 샘플이 아니라 다음을 실제 화면 전환으로 확인할 수 있는 작은 reference implementation을 만드는 것이 목표다.

```text
Root / Panel / Page
Presentation
Theme / Layout / Sprite
View Registry
Binding Lifecycle
```

---

# R0 — 실제형 Demo 구성

가장 먼저 이후 기능을 검증할 실제 화면을 만든다.

## 목표 Hierarchy

```text
RootLayer
├─ TitleUIRoot
├─ DemoGameplayRoot
└─ AlbumUIRoot

PanelLayer
└─ SystemMenuPanel
   └─ PageRoot
      ├─ SavePage
      └─ LoadPage
```

## 화면 흐름

```text
Title
├─ Start
│   ↓
│  Gameplay
│   ├─ Save
│   │   ↓
│   │  SystemMenuPanel
│   │   └─ SavePage
│   │
│   └─ Load
│       ↓
│      SystemMenuPanel
│       └─ LoadPage
│
├─ Load
│   ↓
│  SystemMenuPanel
│   └─ LoadPage
│
└─ Album
    ↓
   AlbumUIRoot
    └─ Back → Title
```

이 단계에서는 실제 게임 기능을 넣지 않는다.

## 제외 대상

```text
SaveCoordinator
ManualSaveFlow
파일 저장
실제 Load
AlbumController
Progression
서버
```

Save/Load에는 mock slot만 둔다.

예:

```text
Slot 1 — Day 2 / Scene 4
Slot 2 — Empty
Slot 3 — Day 5 / Scene 1
```

클릭 시에는 다음 정도면 충분하다.

```csharp
Debug.Log("[Demo] Save slot 1");
Debug.Log("[Demo] Load slot 1");
```

## 구현 대상

```text
Demo/Views/
├─ TitleUIRoot.cs
├─ DemoGameplayRoot.cs
├─ AlbumUIRoot.cs
├─ SystemMenuPanel.cs
├─ SaveSlotPage.cs
├─ SavePage.cs
└─ LoadPage.cs
```

Binding:

```text
VNScreenBindings.Title.cs
VNScreenBindings.Gameplay.cs
VNScreenBindings.Album.cs
VNScreenBindings.SaveLoad.cs
```

`ked-presentation-runtime`의 코드를 그대로 복사하지 않고, 현재 `UIPresentationFlow`의 API와 규칙으로 다시 작성한다.

## 완료 조건

다음 전환이 모두 정상 동작해야 한다.

```text
Title → Gameplay
Title → Album → Title
Title → LoadPage
Gameplay → SavePage
Gameplay → LoadPage
SavePage ↔ LoadPage
Panel Close
```

또한 실제 화면을 통해 다음 의미를 확인할 수 있어야 한다.

```text
Root  = global replace
Panel = stack
Page  = owner-local replace
```

---

# R1 — 작은 품질 개선

R0가 동작한 뒤, Ked에서 부담 없이 가져올 수 있는 품질 개선을 적용한다.

## R1-1. UIRefValidation

현재 각 View에서 반복되는 다음 형태를 정리한다.

```csharp
if (_foo == null)
    Debug.LogWarning(...);

if (_bar == null)
    Debug.LogWarning(...);
```

목표:

```csharp
string missing = "";

AppendMissing(
    ref missing,
    _foo,
    Refs.Foo);

AppendMissing(
    ref missing,
    _bar,
    Refs.Bar);

if (missing.Length > 0)
{
    Debug.LogWarning(
        $"[{GetType().Name}] Missing refs:\n{missing}",
        this);
}
```

하나의 View에서 여러 Ref가 빠져도 한 번의 경고로 전부 확인할 수 있게 한다.

## R1-2. Layer Raycast Guard

`RootLayer`, `PanelLayer` 같은 구조용 Container에 `Graphic`이 붙어 있는 경우 실수로 입력을 먹지 않도록 초기화 시 보장한다.

```text
Layer 자체
→ raycastTarget = false
```

별도 대형 시스템으로 만들지 않고 작은 utility 수준으로 유지한다.

## 완료 조건

- 기존 Demo 동작 변화 없음
- Missing Ref 로그가 한 View당 하나로 정리됨
- Root/Panel Layer가 자식 UI 입력을 차단하지 않음

---

# R2 — Managed View 자동 등록

현재 Bootstrap의 수동 View 등록을 개선한다.

기존:

```csharp
[SerializeField]
private UIBase[] views;
```

다만 기존 `Register()`는 유지한다.

핵심 구조는 다음과 같다.

```text
RegisterManagedViews()
        ↓
Hierarchy discovery
        ↓
IUIManagedView만 선별
        ↓
Register(view)
        ↓
기존 singleton 검증
기존 EnsureInitialized()
```

즉, discovery와 registration rule을 분리한다.

## R2-1. Marker 도입

가칭:

```csharp
public interface IUIManagedView
{
}
```

Concrete View마다 직접 구현하게 하지 않고 기본 타입이 갖도록 한다.

```text
UIRoot<T>  → IUIManagedView
UIPanel<T> → IUIManagedView
UIPage<T>  → IUIManagedView
```

반대로 다음과 같은 반복 UI는 managed registry 대상이 아니다.

```text
VNSaveSlotButton
AlbumEntryWidget
기타 반복 UI
```

이렇게 하면 기존 invariant도 그대로 유지된다.

```text
concrete View type
=
one registered singleton View instance
```

## R2-2. Discovery API

예상 형태:

```csharp
public void RegisterManagedViews(
    Transform root)
```

내부 흐름:

```text
GetComponentsInChildren<UIBase>(true)
→ IUIManagedView 필터
→ Register(view)
```

중복 검증 등의 정책은 이 API에서 다시 구현하지 않는다.

실제 등록 규칙과 검증은 기존 `Register()`가 계속 책임진다.

## R2-3. Bootstrap 정리

기존:

```text
Inspector
views[]
```

수동 배열을 제거한다.

대신:

```text
UI Hierarchy
→ 자동 discovery
→ registration
```

으로 변경한다.

## 완료 조건

Inspector에 View 배열을 일일이 등록하지 않아도 다음 View가 모두 자동 등록되어야 한다.

```text
TitleUIRoot
DemoGameplayRoot
AlbumUIRoot
SystemMenuPanel
SavePage
LoadPage
```

그리고 중복된 concrete View가 있으면 기존 `Register()` invariant가 그대로 실패시켜야 한다.

---

# R3 — Sprite Presentation

이번 개선 작업의 핵심 단계다.

Ked의 아이디어는 가져오되 구현은 `UIPresentationFlow`의 Presentation 언어에 맞게 새로 작성한다.

현재:

```text
UIPresentationSpec
├─ Theme
│  └─ Text
└─ Layout
```

을 다음으로 확장한다.

```text
UIPresentationSpec
├─ Theme
│  ├─ Text
│  └─ Image / Sprite
└─ Layout
```

## R3-1. Image Ref Capability

현재 `IUIPresentationRefProvider`에는 다음 capability가 있다.

```text
TryGetRect
TryGetText
TryGetTextRole
```

여기에 다음을 추가하는 방향으로 설계한다.

```csharp
bool TryGetImage(
    string refId,
    out Image image);
```

일반 View 코드는 여전히 typed Ref API를 사용한다.

```csharp
View.Image(Refs.TitleBG_Image)
```

Presentation만 string `refId`를 통해 접근한다.

## R3-2. Sprite Patch 데이터 모델

Ked의 다음 규칙을 그대로 가져오지는 않는다.

```text
ui/{Theme}/{Port}
```

`UIPresentationFlow`에서는 Sprite 변경도 Presentation 데이터여야 한다.

후보:

```text
ThemeSpec
or
SpritePatchSpec
```

어느 책임이 더 적절한지는 R3 시작 시 결정한다.

핵심 원칙은 다음과 같다.

> 어떤 Sprite를 사용할지는 Presentation이 결정한다.

예:

```text
Light
TitleBG_Image → TitleBG_Light

Dark
TitleBG_Image → TitleBG_Dark
```

## R3-3. Resolver 연결

현재 흐름을 유지한다.

```text
UIPresentationSpec
↓
UIResolver
↓
ResolvedUIPresentation
↓
UIPresentationApplier
```

Sprite 역시:

```text
Resolve
→ Result에 포함
→ Applier에서 적용
```

한다.

`UIManager`가 직접 Sprite를 고르지 않는다.

## R3-4. Sprite Baseline Ownership

현재 `UIPresentationApplier`에는 다음 baseline이 있다.

```text
RectBaseline
TextBaseline
```

여기에:

```text
ImageBaseline
또는
SpriteBaseline
```

을 추가한다.

예:

```text
View authored sprite = A

Presentation Light
→ B 적용

Presentation Dark
→ B를 baseline으로 착각하면 안 됨
→ authored A 복원
→ C 적용
```

기존 ownership 원칙을 Sprite에도 그대로 적용한다.

```text
Restore previously owned value
→ Capture newly owned property
→ Apply new Presentation
```

## R3-5. Image Role 여부 결정

처음부터 role을 확정하지 않는다.

후보 1:

```csharp
TitleBG_Image
```

라는 refId만 사용하는 방식.

후보 2:

```csharp
[UIRefImageRole(UIImageRole.Background)]
TitleBG_Image
```

처럼 semantic role까지 두는 방식.

첫 구현에서는 refId만으로 충분하면 role을 넣지 않는다.

실제 Demo Theme 설계에서 같은 역할을 여러 Image가 공유해야 할 필요가 생겼을 때 role을 추가한다.

## R3-6. 실제 Demo 검증

R0에서 만든 화면을 그대로 사용한다.

예:

```text
Light Theme

Title
→ 밝은 Title BG

Gameplay
→ 밝은 UI Frame

Album
→ 밝은 Album Frame

Save / Load
→ 밝은 System Menu BG
```

Dark Theme에서는 다른 Sprite로 전환한다.

이를 통해 Sprite Presentation이 다음 전체 계층에 정상 적용되는지 검증한다.

```text
Root
Panel
Page
```

## 완료 조건

같은 View instance에서 Theme만 변경했을 때 다음이 정상 동작해야 한다.

```text
Text Theme 변경
Layout 유지/변경
Sprite 변경
```

그리고 Presentation을 바꿨을 때 이전 Sprite가 남지 않아야 한다.

---

# 이번 작업에서 하지 않을 것

이번 PLAN에서는 다음을 명시적으로 제외한다.

```text
Overlay
Top
Addressables
진짜 async Sprite loading
generation/show ticket
PresentationSet
실제 Save/Load
실제 Album 시스템
서버 연동
```

특히 async generation ticket은 구조만 기억해둔다.

```text
현재
Presentation Apply = synchronous

미래
Addressables / Remote Asset
        ↓
async apply
        ↓
stale completion 가능
        ↓
generation ticket 도입
```

실제로 비동기 asset loading이 필요해지는 시점에 추가한다.

---

# 전체 작업 순서

```text
R0
실제형 Demo 구축
        ↓
테스트

R1
RefValidation
Layer Raycast Guard
        ↓
테스트

R2
Managed View
Hierarchy Discovery
Bootstrap 수동 views[] 제거
        ↓
테스트

R3
Image Ref
Sprite Presentation Data
Resolver
Baseline Ownership
Theme Sprite 전환
        ↓
전체 Demo 테스트
```

각 단계는 이전 단계가 정상 동작한 뒤에만 다음으로 넘어간다.

---

# 권장 커밋 단위

```text
feat: add game-style ui demo flow

refactor: simplify ui ref validation

refactor: discover managed ui views from hierarchy

feat: add sprite presentation support
```

R3가 커질 경우 더 작게 나눈다.

```text
feat: expose image presentation refs
feat: resolve sprite presentation patches
feat: preserve authored sprite baselines
```

---

# 최종 방향

이번 작업의 순서는 다음 원칙을 따른다.

```text
실제 사용 예시를 먼저 만든다.
        ↓
기반 코드를 정돈한다.
        ↓
View 등록 편의성을 높인다.
        ↓
마지막에 Presentation 능력을 확장한다.
```

이를 통해 `UIPresentationFlow`를 단순한 아키텍처 샘플에서 실제 게임 UI에 적용 가능한 작은 reference implementation으로 발전시킨다.

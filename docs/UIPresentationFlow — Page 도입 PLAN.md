# UIPresentationFlow — Page 도입 PLAN

## 0. 목표

현재 UI 시스템의 세 번째 공식 View 개념으로 `Page`를 추가한다.

최종 모델은 다음으로 고정한다.

```text
Root
    global replace

Panel
    stack

Page
    owner-local replace
```

Page는 독립적인 전역 Layer가 아니다.

특정 `Root` 또는 `Panel` 내부의 `PageRoot`에 존재하며, 동일 Owner 안에서 한 시점에 하나의 Page만 Current가 된다.

```text
GameplayRoot

SystemMenuPanel
├─ Navigation
└─ PageRoot
   └─ SettingsPage

ConfirmPanel
```

위 상태에서 `ConfirmPanel`을 Push해도 `SettingsPage`는 유지된다.

ConfirmPanel을 Pop하면 기존 SettingsPage가 그대로 다시 드러난다.

---

# P0. 작업 기준선 정리

## 목표

Page 기능과 무관한 repository 구조 문제를 먼저 제거한다.

공식 코드 경로는 다음 하나만 사용한다.

```text
Assets/Scripts
```

원격 `dev`에도 다음이 존재하지 않는 상태에서 Page 작업을 시작한다.

```text
Assets/scripts
Assets/scripts.meta
```

## 완료 조건

```text
Assets/Scripts
    Demo
    Display
    Presentation
    UI
    ...
```

하나만 존재한다.

Page 기능 커밋과 directory casing 정리 커밋은 분리한다.

추천 커밋:

```text
chore: normalize Scripts directory casing
```

---

# P1. Page 타입 계약 추가

## 목표

Root / Panel과 같은 수준의 공식 View type으로 Page를 추가한다.

추가:

```csharp
public interface IUIPage
{
}

public abstract class UIPage<TRefs> : UIBase<TRefs>, IUIPage
    where TRefs : struct, Enum
{
}
```

Page 역시 기존 `UIManager.Register()`에 등록되는 singleton View로 사용한다.

따라서 identity 규칙은 그대로 유지한다.

```text
Concrete View Type
        =
one registered runtime View instance
```

예:

```text
TitleUIRoot        singleton
GameplayUIRoot     singleton

SystemMenuPanel    singleton
ConfirmPanel       singleton

SaveLoadPage       singleton
SettingsPage       singleton
VoicePage          singleton
```

반복되는 요소만 prefab instance로 유지한다.

```text
SaveSlotButton
AlbumThumbnail
ChoiceButton
ListItem
```

## 완료 조건

Root / Panel / Page 모두 동일한 View registration 정책을 따른다.

Page를 위해 별도의 factory나 instance registry를 만들지 않는다.

---

# P2. Page Owner capability 추가

## 목표

모든 Root / Panel이 Page를 소유한다고 가정하지 않는다.

Page를 표시할 수 있는 View만 명시적으로 capability를 가진다.

추가:

```csharp
public interface IUIPageOwner
{
    RectTransform PageRoot { get; }
}
```

`IUIPageOwner`는 navigation type이 아니라 capability다.

```text
IUIRoot
    나는 Root다.

IUIPanel
    나는 Panel이다.

IUIPageOwner
    나는 Page를 표시할 content coordinate space를 제공한다.
```

따라서:

```csharp
public sealed class SystemMenuPanel
    : UIPanel<SystemMenuPanel.Refs>,
      IUIPageOwner
{
    public RectTransform PageRoot => _pageRoot;
}
```

처럼 필요한 concrete View만 구현한다.

`UIRoot<T>` / `UIPanel<T>` 자체에는 `IUIPageOwner`를 붙이지 않는다.

---

# P3. Page의 물리적 ownership 고정

## 목표

singleton Page를 여러 Owner 사이에서 자유롭게 reparent하지 않는다.

Page는 해당 Owner의 `PageRoot` 아래에 authored child로 둔다.

```text
SystemMenuPanel
└─ PageRoot
   ├─ SaveLoadPage
   ├─ SettingsPage
   └─ VoicePage
```

각 Page는 singleton `UIBase`로 별도로 Register하지만 hierarchy상 위치는 안정적으로 유지한다.

UIManager가 `SwitchPage()` 할 때 Page를 임의로 다른 Owner로 옮기지 않는다.

대신 다음을 검증한다.

```text
Page의 parent == Owner.PageRoot
```

잘못된 Owner/Page 조합이면 즉시 실패시킨다.

## 이유

이 방식이면:

- singleton identity 유지
- parent coordinate 안정
- Presentation baseline 안정
- arbitrary reparenting 방지
- Owner 관계를 hierarchy에서도 확인 가능

하다.

별도의 Page affinity registry까지 만들 필요도 없다.

---

# P4. UIManager Page lifecycle 추가

새 partial:

```text
UIManager.Page.cs
```

를 추가한다.

UIManager가 기억할 상태는:

```text
Owner → Current Page
```

이다.

개념적으로:

```csharp
private readonly Dictionary<UIBase, PageState> _pages = new();
```

`PageState`에는 최소한:

```text
Page
Presentation
```

만 저장한다.

Navigation history는 저장하지 않는다.

다음과 같은 전역 상태는 만들지 않는다.

```text
CurrentPage
_pageStack
PushPage
PopPage
PageHistory
```

Page는 Owner-local 상태다.

---

# P5. SwitchPage / ClosePage 구현

핵심 API는 다음 의미를 갖는다.

```csharp
UI.SwitchPage<SettingsPage>(
    systemMenuPanel,
    settingsPresentation,
    ...);
```

Owner 인자는 `UIBase`로 받고 내부에서 다음을 검증한다.

```text
Owner is IUIPageOwner

그리고

Owner가 CurrentRoot이거나
현재 Panel stack에 존재함
```

Page는:

```text
TPage : UIBase, IUIPage
```

이어야 한다.

## Switch 규칙

현재:

```text
SystemMenuPanel
└─ SaveLoadPage
```

에서 SettingsPage를 열면:

```text
SaveLoadPage
    Close
    Hide

SettingsPage
    Resolve Presentation
    Show

SystemMenuPanel
    그대로 유지
```

가 된다.

같은 Page를 다시 Switch하는 경우에는 Close하지 않는다.

```text
SettingsPage
    ↓
SwitchPage<SettingsPage>(다른 Presentation)
```

이면 같은 Root와 동일하게 Presentation을 다시 Resolve한다.

---

# P6. Owner lifecycle과 Page lifecycle 연결

Page의 lifetime은 Owner보다 길 수 없다.

공식 규칙:

```text
Owner가 navigation context에서 제거됨
        ↓
Owner의 Current Page도 자동 Close
```

### Panel Pop

```text
SystemMenuPanel
└─ SettingsPage
```

에서 SystemMenuPanel을 Pop하면:

```text
SettingsPage Close
SystemMenuPanel Pop
```

순서로 처리한다.

### Panel 위에 다른 Panel Push

```text
SystemMenuPanel
└─ SettingsPage

ConfirmPanel
```

에서는 SettingsPage를 닫지 않는다.

Panel stack에 Owner가 남아 있기 때문이다.

### PopUntil

```text
Panel A
└─ Page A

Panel B
└─ Page B

Panel C
└─ Page C
```

에서 기존 Panel A로 돌아간다면 제거되는 B/C의 Page도 자동 Close한다.

A의 Page는 그대로 유지한다.

### Root Switch

```text
TitleRoot
└─ AlbumPage

        ↓

GameplayRoot
```

이면 TitleRoot의 AlbumPage부터 Close한다.

---

# P7. lifecycle callback 정리

Page를 추가하면서 UIManager의 cleanup callback 의미를 맞춘다.

현재 Panel의 `afterPopped`는 Panel만 제거된다는 의미를 갖지만, 앞으로 Owner가 제거될 때 Page도 함께 lifecycle에서 빠질 수 있다.

따라서 callback 이름을 보다 일반적인:

```text
afterClosed
```

의 의미로 정리한다.

목표는 Root / Panel / Page 모두 UIManager가:

```text
View lifecycle 종료
    ↓
Bindings에 Unbind 통보
```

할 수 있게 만드는 것이다.

이를 통해 application layer가 Page lifecycle을 별도로 추적하지 않게 한다.

---

# P8. Binding을 BindView 하나로 통합

현재:

```text
BindMain
BindPanel
```

로 나뉜 helper를 공통:

```csharp
BindView(...)
```

로 정리한다.

기존 cleanup storage는 그대로 유지한다.

```csharp
Dictionary<UIBase, List<Action>> _cleanupByOwner
```

즉 Binding infrastructure는 계속 `UIBase` 단위다.

새로운:

```text
PageBindingManager
PageCleanupRegistry
```

같은 것은 만들지 않는다.

## lifecycle 규칙

```text
Root binding
    CurrentRoot인 동안 유지

Panel binding
    Panel stack에 존재하는 동안 유지

Page binding
    해당 Owner의 CurrentPage인 동안 유지
```

Page binding을 Panel이 소유하지 않는다.

예:

```text
SystemMenuPanel
    자기 binding 소유

SettingsPage
    자기 binding 소유
```

이다.

SettingsPage에서 SaveLoadPage로 바뀌면 SettingsPage는 즉시 Unbind한다.

Owner Panel이 닫힐 때까지 기다리지 않는다.

핵심 원칙:

```text
Binding mechanism은 공통.
Binding lifetime은 각 View lifecycle을 따른다.
```

---

# P9. SafeArea ownership 분리

공식 계약:

```text
Root / Panel
    presentation boundary
    SafeArea 소유 가능

Page
    Parent content coordinate space 사용
    기본적으로 SafeArea를 다시 적용하지 않음
```

예:

```text
SystemMenuPanel
└─ SafeAreaRoot
   └─ PageRoot
      └─ SettingsPage
```

SettingsPage는 이미 Parent가 계산한 좌표계를 사용한다.

따라서 Page 안에서 SafeArea를 한 번 더 적용하지 않는다.

현재 Presentation pipeline에서:

```text
EnsureInitialized
SafeArea
Resolve
Apply
```

로 되어 있는 부분을 다음 책임으로 정리한다.

```text
Root / Panel
    SafeArea
    Resolve
    Apply

Page
    Resolve
    Apply
```

별도의 Page resolver는 만들지 않는다.

Page 역시 기존 `UIResolver`와 `UIPresentationApplier`를 그대로 사용한다.

---

# P10. Parent / Page Presentation 책임 분리

Page 역시 독립적인 `UIPresentationSpec`을 가진다.

단 Owner와 Page가 동일한 UI property를 동시에 소유하지 않도록 한다.

예:

```text
SystemMenuPanel Presentation
    panel shell
    navigation
    PageRoot geometry

SettingsPage Presentation
    SettingsPage 내부 content
```

즉 Parent가 Page의 내부 layout까지 변경하지 않는다.

Page도 Parent shell을 변경하지 않는다.

---

# P11. Display 변경 Reapply 확장

현재 표시 중인 Page는 Display 변경 시 반드시 다시 Resolve한다.

등록된 모든 Page를 Resolve하지 않는다.

오직 현재 presentation tree에 참가하는 Page만 처리한다.

## 순서

반드시:

```text
Owner
    ↓
Current Page
```

순서다.

전체적으로:

```text
CurrentRoot
    ↓
Root의 CurrentPage

live Panel A
    ↓
Panel A의 CurrentPage

live Panel B
    ↓
Panel B의 CurrentPage
```

순서로 적용한다.

Panel은 기존 keep-alive 정책을 그대로 따른다.

따라서:

```text
live Panel
    current Page도 Reapply

inactive/deep Panel
    Page Reapply하지 않음
```

이다.

deep Panel이 다시 live 상태가 될 때 최신 DisplayContext로 다시 Resolve하면 된다.

---

# P12. Panel stack 상태와 Page 상태 검증

이 단계에서 주요 lifecycle 시나리오를 확인한다.

### A. Page replace

```text
SystemMenuPanel
└─ SaveLoadPage

→ Settings 선택

SystemMenuPanel
└─ SettingsPage
```

확인:

- SaveLoadPage hidden
- SaveLoadPage unbound
- SettingsPage visible
- SettingsPage bound

### B. Page 위 Panel

```text
SystemMenuPanel
└─ SettingsPage

→ ConfirmPanel Push
```

확인:

- SettingsPage Current 유지
- SettingsPage binding 유지
- ConfirmPanel이 위에 표시

### C. 위 Panel Pop

```text
ConfirmPanel Pop
```

확인:

- SettingsPage 그대로 복귀
- 재생성하지 않음
- 재binding 불필요

### D. Owner Panel Pop

```text
SystemMenuPanel Pop
```

확인:

- SettingsPage Close
- SettingsPage Unbind
- SystemMenuPanel Close

### E. Root 교체

Root-owned Page가 있을 경우:

```text
OldRoot Page Close
OldRoot Close
NewRoot Open
```

확인.

---

# P13. 최소 Demo 구성

현재 generic repository에서 실제 Page 동작을 확인할 수 있는 최소 Demo를 만든다.

VN 전체 메뉴를 구현하지 않는다.

최소 구성:

```text
AdaptiveDemoUIRoot

DemoMenuPanel
├─ PageRoot
│  ├─ DemoPageA
│  └─ DemoPageB
│
├─ PageA Button
├─ PageB Button
└─ Confirm Button

DemoConfirmPanel
```

검증 흐름:

```text
Root
→ MenuPanel
→ PageA
→ PageB
→ ConfirmPanel
→ ConfirmPanel Pop
→ PageB 유지
→ MenuPanel Pop
→ PageB Close
```

Page Presentation에도 최소한 Wide / Compact 차이를 하나 넣어 Display reapply를 눈으로 확인한다.

---

# P14. 테스트

새 기능의 핵심 규칙만 테스트한다.

우선순위:

1. 같은 Page type은 singleton registration 규칙을 따른다.
2. Owner 안에서는 Page 하나만 Current다.
3. Page switch 시 이전 Page가 hidden 된다.
4. Page switch 시 이전 Page state가 제거된다.
5. 같은 Page 재선택은 새 instance를 만들지 않는다.
6. 잘못된 PageRoot에 속한 Page는 거부한다.
7. Panel Push는 아래 Panel의 Current Page를 닫지 않는다.
8. Panel Pop은 해당 Panel의 Current Page를 닫는다.
9. PopUntil은 제거되는 Panel의 Page만 닫는다.
10. Root Switch는 이전 Root의 Page를 닫는다.
11. Page Presentation에는 SafeArea를 재적용하지 않는다.
12. Display 변경 시 live Owner → Current Page 순으로 재적용된다.

테스트를 위해 production abstraction을 새로 만드는 것은 피한다.

현재 구조로 검증하기 어려운 부분은 Demo/PlayMode 검증으로 남긴다.

---

# P15. 문서화

최종 문서에 세 UI primitive를 명확하게 정의한다.

```text
Root
    Application-level base View.
    Globally replaced.

Panel
    Overlay View.
    Managed as a stack.

Page
    Singleton content View owned by a Root or Panel.
    Replaced locally within the Owner.
```

추가로 명시:

```text
Page has no stack.
Page has no global CurrentPage.
Page does not own SafeArea by default.
Page cannot outlive its Owner.
Panel Push does not close underlying Pages.
Page bindings belong to the Page itself.
Page Presentation is resolved after its Owner.
```

---

# 구현 후 예상 구조

```text
UIBase
├─ UIRoot
├─ UIPanel
└─ UIPage

IUIRoot
IUIPanel
IUIPage

IUIPageOwner
└─ RectTransform PageRoot
```

UIManager:

```text
UIManager
├─ Register / View singleton registry
│
├─ Root
│  └─ CurrentRoot
│
├─ Panel
│  └─ PanelStack
│
├─ Page
│  └─ Owner → CurrentPage
│
└─ Presentation
   └─ Owner → Page 순 Reapply
```

최종 navigation 관계:

```text
Root
    global replace

Panel
    stack

Page
    owner-local replace
```

그리고 가장 중요한 구조는:

```text
GameplayRoot

SystemMenuPanel
└─ PageRoot
   └─ SettingsPage

ConfirmPanel
```

이다.

Page 때문에 Panel의 단순한 stack semantics를 변경하지 않는다.

Page 때문에 새로운 Modal 계층도 만들지 않는다.

Page 때문에 별도의 Binding 시스템도 만들지 않는다.

Page 때문에 별도의 Presentation/Resolver 시스템도 만들지 않는다.

기존 시스템 안에 **Owner-local replace라는 하나의 lifecycle만 추가한다.**
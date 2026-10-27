# PLAN — ked-presentation-runtime에서 돌아온 것

2026-10-27~. A · C는 `feat/from-ked`(← `dev` @ `cf3fa98`)에서 하고 `dev`에 합쳤다(`4a4af6f`). B는 `feat/authoring-tools` ← `dev` @ `4a4af6f`.

비주얼 노벨 연출 런타임 `ked-presentation-runtime`의 예제 UI 12화면을 이 프레임워크 위에 다시 세웠다. 프레임워크는 복사해 들였고, 화면은 빌드 명세와 빌더로 지었다. 그 과정에서 드러난 것을 이 레포로 되돌린다.

블랙홀 키우기 때와 같은 방식이다. 게임에서 생긴 것 가운데 어느 게임에도 쓸 것을 원본으로 옮긴다.

## 규율

1. **원본에서 먼저 고친다.** 사본(ked의 `Assets/UIPresentationFlow`)만 고치지 않는다.
2. **고친 원본을 ked로 다시 들인다.** 원본 커밋의 blob을 풀어 덮어쓰고, 대조(`diff -r`)로 차이 0을 확인한다(ked의 `vendoring.md`).
3. **ked 쪽 임시 해결책을 지운다.** 원본이 맡게 된 일을 ked가 두 번 하지 않게 한다.
4. **Unity 6000.3에서도 컴파일되어야 한다.** ked가 6000.3이라, 원본 코드는 ked 기준(6000.3 참조 어셈블리)으로 오프라인 컴파일을 먼저 거친다.
5. **에셋은 내리지 않는다.** 이 레포(6000.6)에서 직렬화한 프리팹·에셋은 ked로 옮기지 않는다. 넘어가는 것은 코드와 셰이더뿐이다.

## 범위

| | 무엇 | 상태 |
|---|---|---|
| **C. 문서** | `docs/AUTHORING.md`(화면 제작 원칙). README에 복사 안내 · ked 실사용 · 설계 결정 한 줄 · A 반영 | `dev`에 합침(`4a4af6f`) |
| **A. 런타임 빈틈** | 실제로 쓰다 드러난 셋(아래) | `dev`에 합침(`4a4af6f`) |
| **B. 도구 추출** | ked 빌더 가운데 어느 게임에도 쓸 부분을 이 레포의 에디터 도구로 옮긴다(아래) | 2026-10-27, Unity 확인(10-27) |
| **D. ked에 남는 것** | 화면 명세 8개, KedUI 그림과 부품 모양(버튼 스타일 · 그림 이름), 화면 흐름 · 호스트 포트, 앱 빌드(화면 목록 · 캔버스) | — |

## A. 런타임 빈틈

### A-1. 버튼 위에서 시작한 끌기가 목록으로 넘어가지 않는다

- **증상** — 스크롤 목록 안의 버튼(백로그 줄의 이동 버튼) 위에서 끌면 목록이 끌리지 않는다.
- **원인** — `UI_EventHandler`가 끌기 인터페이스를 구현한다. 그래서 EventSystem이 끌기를 그 버튼에 보내고, 부모 `ScrollRect`까지 가지 않는다.
- **ked의 임시 해결책** — `KedDragToScroll`. 끌기를 받아 부모 `ScrollRect`에 그대로 넘기는 컴포넌트를 버튼마다 붙였다.
- **원본의 해결** — 그 처리기에 끌기를 받는 쪽(`OnBeginDrag` · `OnDrag` · `OnEndDrag` 구독)이 없으면, 끌기를 가장 가까운 부모의 끌기 처리기로 넘긴다.
  - `IInitializePotentialDragHandler`도 받아 같이 넘긴다.
  - 끌기 도중에 꺼지면 넘긴 끌기를 끝내 준다.
  - 끌기를 직접 쓰는 곳(블랙홀 설정 줄의 슬라이더)은 지금처럼 자기가 받는다.
  - 같은 오브젝트에 끌기를 받는 다른 컴포넌트(Slider · Scrollbar · ScrollRect)가 있어도 넘기지 않는다. EventSystem은 그 오브젝트의 처리기 모두에 끌기를 보내므로, 넘기면 목록 안의 슬라이더가 목록까지 끈다.
  - 끌기가 시작되면 클릭을 막는 기존 규칙도 그대로다.

### A-2. Panel이 열려도 Root가 키보드 · 패드 입력을 받는다

- **증상** — 메뉴를 연 채 방향키 · Enter를 누르면 아래 Root의 버튼(타이틀 · 퀵메뉴 · 선택지)에 닿는다.
- **원인** — UIManager는 가려진 Panel만 막고 Root는 막지 않는다. 딤은 포인터만 막는다.
- **ked의 임시 해결책** — 흐름이 Panel을 쌓고 꺼낼 때마다 Root의 `CanvasGroup.interactable`을 직접 맞췄다.
- **원본의 해결** — Panel이 하나라도 있으면 지금 Root의 `CanvasGroup.interactable`을 끈다. 쌓기 · 꺼내기 · Root 바꾸기 때마다 맞춘다.
  - 기본으로 켜 두고, 끄는 속성 `BlockRootUnderPanels`을 둔다.
  - `blocksRaycasts`와 투명도는 건드리지 않는다. 딤 없는 Panel 바깥을 누른 포인터가 Root를 지나 아래(게임 화면)로 빠지지 않게 하려는 것이다. Root의 버튼은 `IsInteractable()`이 부모 CanvasGroup을 따르므로 눌리지 않는다.
  - 아래 화면도 계속 조작되어야 하는 창은 Overlay에 둔다.

### A-3. Panel을 닫아도 포커스가 돌아오지 않는다

- **증상** — 메뉴를 닫으면 선택이 닫힌 창 안에 남아, 마우스를 쓰기 전까지 키 입력이 먹지 않는다.
- **ked의 임시 해결책** — 흐름이 Panel을 열 때의 선택을 쌓아 두었다가 닫을 때 되돌렸다.
- **원본의 해결** — Panel을 쌓을 때 그 순간의 선택(EventSystem)을 기억했다가, 꺼낼 때 그것이 아직 보이고 막혀 있지 않으면 되돌린다.
  - 전부 꺼내면 맨 처음 Panel을 쌓기 전의 선택으로 돌아간다.
  - 아래 Panel로 되돌아가면(`PushPanel`의 되돌아가기) 그 Panel 안의 선택으로 돌아간다.
  - 되돌릴 수 없으면, 닫힌 창 안의 것이 선택으로 남지 않게만 비운다. 대신 무엇을 고를지는 앱이 정한다. ked는 재생 화면이면 선택지로 간다.

### 확인

- **블랙홀 데모**
  - 설정의 음량 슬라이더를 끌 수 있다(A-1에서 넘기지 않는 쪽).
  - 일시 정지 · 설정을 연 채 방향키 · Enter가 아래 화면에 닿지 않는다(A-2).
  - 창을 닫으면 연 버튼으로 포커스가 돌아온다(A-3).
- **ked** — 다시 들여오고 임시 해결책을 지운 뒤 확인한다.
  - 백로그 목록을 이동 버튼 위에서 끌 수 있다.
  - 메뉴를 연 채 키 입력이 재생 화면에 닿지 않는다.
  - 메뉴를 닫으면 포커스가 돌아온다.

## B. 도구 추출

ked의 빌더(`Assets/Samples/KedUI/Common/Editor`) 가운데 게임에 매이지 않은 부분을 `Assets/Editor/UI/Authoring`으로 옮겼다. 쓰임은 README "화면 짓기 도구", 원칙은 AUTHORING "도구 — 누가 무엇을 맡나".

### 정한 것 (2026-10-27)

| | 정한 것 | 까닭 |
|---|---|---|
| 경계 | 게임에 매이지 않은 것만 — 시안 좌표 노드, 저장 · 플레이 중 거부, Presentation 한 번 만들기, 그림 이름 색인, 화면 묶음 규칙 · 감사, 검사, 동적 글꼴. 버튼 · 슬라이더 꼴은 게임에 남긴다 | 사례 하나(ked)로 부품 꼴을 정하면 다음 게임에 맞지 않는다 |
| 명세 찾기 | 앱의 빌드 메뉴가 명세를 순서대로 부르고 뿌리를 코드 상수로 넘긴다. 이 레포는 부품만 준다 | 순서 · 의존(Page 다음 그 주인, 항목 다음 캔버스)이 코드에 드러난다. 설정 자산이 없다 |
| 경로 | 묶음 하위 폴더 이름(Scripts · Prefabs · Presentation · Sprites · Data · Editor)은 고정, 뿌리와 "묶음 밖에서 가리켜도 되는 곳"은 앱이 넘긴다. 묶음이 아닌 앱은 자리를 직접 적는다 | |
| 증명 | 블랙홀 일시 정지 창을 명세로 다시 짓는다 — 같은 자리 · 같은 GUID, 데모 폴더는 그대로 | 원본(손 프리팹)이 git에 있어 비교된다 |
| 에디터 asmdef | `Editor/UI`에 `UIPresentationFlow.Editor`를 둔다 | asmdef를 쓰는 앱(ked의 `Ked.Samples.UI.Editor`)은 `Assembly-CSharp-Editor`를 참조할 수 없다. 기존 `Editor/UI` 코드는 데모에 매이지 않아 그대로 든다 |

### 한 것

- `Editor/UI/Authoring/` — `UINode` · `UIAnchor` · `UINodes`, `UIPrefabs` · `UISerialized`, `UIPresentationAssets`, `UISpriteIndex`, `UIScreenUnits`, `UIScreenValidation`(+ `UIScreenChecks`, 메뉴 `Tools/UI Presentation/Validate Screens`), `UIFontAssets`
  - ked 코드에서 옮기며 넓힌 것: `Node`에 앵커와 다른 기준점(블랙홀의 "버튼 아래에 매단 글자"), 그림 색인의 폴더 빼기와 "겹치면 그 이름만 안 찾기"(블랙홀은 테마 판이 같은 이름), 위젯 Refs · `presentationId`를 고르는 엄한 검사(이 레포의 데모는 위젯이 Refs 일부만 쓴다), 플레이 중 빌드 거부
- `Editor/UI/UIPresentationFlow.Editor.asmdef`
- 블랙홀: `Editor/BlackHoleScreenKit` · `Editor/Screens/PausePanelSpec` · `Editor/BlackHoleScreenBuild`(메뉴 `UI Demo/Build BlackHole Screens`)
- ked: 다시 들여와 빌더를 갈아탔다 — `KedNode` · `KedAnchor` · 자리 계산 · 저장 · Presentation 만들기 · 그림 색인 · 묶음 규칙 · 감사 · 검사 · 글꼴 만들기 · 비우기를 지우고 이 레포의 것을 부른다. 남은 것은 부품 꼴(`KedUIKit`), 설정(`KedUIUnits` · `KedUIFonts`의 자리), 엄한 검사 메뉴, 명세, 앱 빌드
- 오프라인 컴파일(6000.3 참조 어셈블리, ked 기준): 오류 0 · 경고 0. 블랙홀 명세 셋도 같은 기준으로 따로 확인

### 이 레포의 검사 결과

- 블랙홀 프리팹 7: 문제 0(설정 창의 `SettingRow` 템플릿은 위젯이라 Refs 일부만 있어도 된다)
- Adaptive UI 데모: `TitleUIRoot`에 Refs 넷(`LoadBtn_*` · `AlbumBtn_*`), `AdaptiveDemoScreen`에 `SafeAreaRoot`가 없다 — 등록할 때도 경고가 나는 것이고, 화면 코드는 없을 때 경고하고 넘어간다. 손 프리팹이라 이번에는 두었다
- Adaptive UI 데모 Presentation 넷은 `presentationId`가 비었다 — 기본 검사에서는 문제가 아니다

### 확인 (2026-10-27, 아미야 "문제 없는 것을 확인함")

- 블랙홀 `UI Demo/Build BlackHole Screens`(편집 모드): 보고 문제 0. 데모 씬의 일시 정지 창이 전과 같다
- 일시 정지 창의 프리팹 비교: 파일 안 fileID가 모두 새로 매겨져 git diff로는 보이지 않는다(손 프리팹을 처음 다시 지은 경우 — AUTHORING "재생성"). 이름 경로 · 컴포넌트 종류로 짝지어 fileID를 뺀 값끼리 비교하니 구조가 같고, 값은 셋만 달랐다
  - 레이어 Default → UI(빌더는 UI 레이어에 짓는다)
  - 버튼의 Selected 색 1 → 0.96(Unity 기본값. Sprite Swap 버튼이라 쓰지 않는다)
  - TMP의 `m_Color` · `m_fontColor32` · 글자 스타일 해시 — 그릴 때 TMP가 다시 채우는 칸이다. 보이는 색(`m_fontColor`)은 같다
- `Tools/UI Presentation/Validate Screens`: 위 Adaptive UI 다섯 줄
- ked: `Build Sample UI`(편집 모드) 뒤 git 변경은 저장/불러오기 패널의 fileID뿐, `Validate Sample UI` 문제 0

## 그 다음

- 독립 저장소에서 화면 묶음을 만들어 게임에 반입하기
- 효율 측정 — 저장소 분리의 효과, 프리팹 생성 방식의 효과. 처음 만들 때와 고칠 때를 함께 잰다.

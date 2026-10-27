# 비주얼 노벨 UI 샘플 — ked의 VN UI 묶음을 들이고 Adaptive UI 데모를 걷는다

## 왜

블랙홀 데모는 Root · Panel · Overlay, `SetContext`, Safe Area를 보인다. 보이지 않는 것이 있다.

- **Page** — Owner 안의 교체 영역(`SwitchPage`)
- 쪽을 넘기는 목록(저장 슬롯), 버튼 위에서 끌어도 넘어가는 목록(대화 기록)
- 화면비 Variant
- 빌드 명세로 지은 화면 전부(블랙홀은 일시 정지 창 하나)

그 자리는 Adaptive UI 데모가 맡았지만 손 프리팹과 바인딩 시험이 섞인 채 다듬다 멈췄다. Refs가 빠졌고 `presentationId`도 비어 있다([PLAN_from-ked](PLAN_from-ked.md) "이 레포의 검사 결과").

ked-presentation-runtime의 비주얼 노벨 UI는 위의 것을 모두 담는다. 화면은 12개(Root 2 · Panel 6 · Page 4)이고, 명세 · 부품 키트 · 검사도 갖췄다.

## 정한 것 (2026-10-27)

| | 정한 것 | 까닭 |
|---|---|---|
| 원본 | **ked가 원본**, 여기는 복사본 | 블랙홀 데모와 같다. 고칠 일은 ked에서 고쳐 다시 들인다 |
| 나누기 | ked에서 먼저 **재생 런타임에 매인 것을 `KedUI/App/`으로** 뺐다. 복사 규칙은 "KedUI에서 `App/`과 `Art/`를 뺀 것" | 묶음이 재생 런타임을 알면 들여올 수 없다. `Art/`는 아무 화면도 쓰지 않는 그림이다 |
| 앱 | 데모 앱(`VisualNovel/App`)은 **이 레포가 원본**이다. 재생 런타임 대신 가짜 재생과 메모리 저장을 쓴다 | 화면 흐름 · 목록 · 글자 표는 앱의 몫이다(AUTHORING "화면 묶음") |
| Adaptive UI 데모 | **전부 걷는다** — 코드 · 에셋 · 바인딩 · 씬 · 빌드 설정의 씬 | 같은 것을 더 잘 보이는 샘플이 들어온다. 반쯤 된 데모를 둘 까닭이 없다 |
| Variant | **ked 원본에 하나 더했다** — 재생 화면 21:9 `UltraWide_HudInBoard` | 샘플이 화면비 Variant를 보이려면 원본에 있어야 한다(복사본만 고치지 않는다) |

## ked에서 한 것 (원본)

`feat/uipf-ui`: `c86873cb` 묶음과 App 나누기 · 21:9 Variant 명세, `a91f1a12` 그림 임포트 설정, `82ac9340` Variant 산출물, `e340d72a` 화면 키 고정(아래 "Unity를 처음 열고 드러난 것"). ked Unity(6000.3)에서 확인했다 — 빌드 · 검사 문제 0, 재생 · 메뉴 · 선택지 · 백로그 · 설정이 전과 같고, 21:9 ↔ 16:9에서 꼬리표 · 퀵메뉴가 오간다(10-27, 아미야). 복사본은 `82ac9340`에서 떴고, `e340d72a`에서 다시 떴다.

- **묶음** `Ked.Samples.UI`는 이 프레임워크 · TMP · uGUI만 참조한다
- **App** `Ked.Samples.UI.App`이 재생 런타임 쪽을 맡는다. 흐름 · 호스트, 대사창 · 선택지 상자의 런타임 계약 어댑터, 선택지 항목(본체 형식), 연출 배치 DB, 글자 표, 캔버스, 앱 빌드
- 재생 화면은 대사창 · 선택지 상자를 **UI 부품**(`PlayDialogueBox` · `PlayOptionsBox` — 그리기만)으로 내놓는다. App이 그 위에 계약을 잇고, 데모는 같은 부품에 가짜 재생을 잇는다
- 뿌리는 런타임 asmdef가 있는 폴더로 찾는다(`KedUIUnits.Root`). 이 프레임워크 코드의 자리도 asmdef로 찾는다. 그래서 다른 자리로 복사해도 그대로 돈다
- 21:9 Variant: 재생 화면 명세가 `PlaySpec.EnsureVariants`로 처음 한 번 더한다(`UIPresentationAssets.EnsureLayoutVariant`). 장면 꼬리표와 퀵메뉴를 화면 가장자리에서 1600×900 판 안쪽 윗줄로 옮긴다
- 그림 임포트 설정(`KedUIArtImporter`)은 PNG가 없는 manifest 줄을 건너뛰고 알린다 — 복사본에 `Art/`가 없어서다

## 이 레포에서 한 것

```text
Assets/Demo/VisualNovel/
├─ KedUI/                     ked 묶음의 복사본(.meta째 — GUID가 ked와 같다)
│   ├─ Ked.Samples.UI.asmdef · asset-manifest.json
│   ├─ Common/                부품 키트 · 그림 · 글꼴(Noto Sans KR, OFL) · 검사 메뉴
│   ├─ Screens/{화면}/        Scripts · Editor(명세) · Prefabs · Presentation · Sprites
│   └─ Documentation~/vendoring.md
└─ App/                       데모 앱 — 이 레포가 원본
    ├─ Runtime/               VisualNovelDemo.asmdef
    │   ├─ VNDemoHost         캔버스 루트 — UIManager · 화면 등록 · 글자 표 · 흐름의 수명(ked KedUIHost 자리)
    │   ├─ VNDemoFlow.*       화면 흐름(ked KedUIFlow 자리) — 타이틀 · 재생 · 메뉴 · 저장/불러오기 · 기록 · 설정 · 모달 · 키보드
    │   ├─ VNDemoPlayer       가짜 재생 — 대본을 대사창 · 선택지 상자에 그린다(타자기 · 자동 · 빨리 넘기기 · 되감기 · 기록)
    │   ├─ VNDemoScript       대본(글자는 글자 표의 키)
    │   ├─ VNDemoSaves        메모리 저장
    │   └─ VNDemoOptionItem   선택지 항목
    ├─ Editor/                VisualNovelDemo.Editor.asmdef
    │   ├─ VNDemoBuild        메뉴 UI Demo > Build Visual Novel Demo
    │   └─ VNDemoOptionItemSpec
    ├─ Texts/VNDemoTexts.csv  ked 글자 표에서 화면 글자를 옮기고, 데모 글자를 더했다
    ├─ Prefabs/ · Scenes/     빌드 산출물
```

- 빌드는 ked 앱 빌드와 같은 순서로 묶음의 명세를 부른다. 그림 임포트 설정 → 글꼴 → 선택지 항목 → Page 넷 → Root 둘 → Panel 여섯 → 글꼴 묶음 · Presentation → 씬 → 검사. 캔버스 프리팹 대신 씬을 짓고, 씬을 빌드 설정 목록 맨 앞에 넣는다
- 데모 흐름은 ked 흐름을 줄여 옮겼다. 차이는 셋이다. 재생 본체 대신 `VNDemoPlayer`를 쓰고, 저장은 메모리에만 하고, 설정은 저장하지 않는다(플레이를 멈추면 기본값)
- 키보드는 설정 창의 "키보드 조작" 표와 같다. Space · Enter 다음 줄, Ctrl 누르는 동안 빨리 넘기기, A 자동, S 빠르게 재생, R 되감기, Esc 맨 위 창 닫기 · 숨긴 UI 다시 보기. 선택지는 방향키 · Enter로 고른다
- 이 프로젝트는 Input System만 켜 있다(`activeInputHandler` 1). 그래서 데모는 `UnityEngine.InputSystem`의 `Keyboard`를 읽고, EventSystem에는 `InputSystemUIInputModule`을 붙인다. 묶음에는 `UnityEngine.Input`을 쓰는 곳이 없다

## 걷은 것 — Adaptive UI 데모

| 자리 | 무엇 |
|---|---|
| `Assets/Scripts/Demo/` | 화면 8 · `VNScreenBindings` 6 · 진단 3 · 부트스트랩 1(1,100줄) — **런타임 asmdef 안**에 있었다 |
| `Assets/Demo/AdaptiveUI/` | 프리팹 2 · Presentation 5 · 레이아웃 2 |
| `Assets/UI/` | Image · Text Binding, 테마 · 팔레트 · 글꼴 묶음 |
| `Assets/Scenes/AdaptiveUIDemo.unity` | 씬, 그리고 빌드 설정의 씬 줄 |

- 걷기 전에 확인했다. 지우는 에셋 51개의 GUID를 바깥에서 가리키는 곳은 빌드 설정 하나뿐이었다. 지우는 코드의 타입을 바깥 코드가 쓰는 곳은 없었다(이름이 같은 것 말고는)
- 걷어야 하는 까닭이 하나 더 있었다. 데모의 `SystemMenuPanel`은 전역 이름공간에 있었고 런타임 어셈블리 안에 있었다. 그래서 `using Ked.Samples.UI;`로 쓰는 샘플 코드에서는 이것이 묶음의 `SystemMenuPanel`을 가렸다(C#은 using보다 선언된 이름을 먼저 찾는다)
- 걷으며 런타임 · 에디터 코드는 바꾸지 않았다. ked는 데모를 들이지 않으므로 ked의 사본은 그대로 맞았다(뒤에 실험 칸을 고친 `7da70e6`은 다시 들였다 — 아래)
- 그대로 둔 것: 바인딩 도구의 기본 자리 `Assets/UI/TextBindings` · `Assets/UI/ImageBindings`(`UIBindingEditorUtil`). 도구의 규칙이고, 쓸 때 폴더를 만든다. Binding을 보이는 것은 블랙홀 데모다 — 게임에서 가져온 Binding(Image Theme 둘 · Text Binding 카탈로그)을 제 폴더(`Demo/BlackHole/ImageBindings` · `TextBindings`)에 둔다. 비주얼 노벨 샘플은 Binding 없이 키트가 그림 · 글꼴을 정한다

## Unity를 처음 열고 드러난 것 (10-27)

UIPF Unity(6000.6)로 처음 열자 경고가 넷 났다.

- **화면 키 셋이 겹쳤다** — `TitleScreen` · `ConfirmPanel` · `SettingsPanel`이 블랙홀 데모에도 있다. 데모마다 제 화면만 등록해서 실행에는 지장이 없었지만, 규칙("데이터 키는 프로젝트에서 하나")을 어긴다. **ked 원본에서 묶음의 키를 `Ked.`로 고정했다**(아미야 결정). 블랙홀 쪽은 게임 파일 그대로라는 전제가 있고, 묶음은 옮겨 다니니 흔한 이름은 어디서든 부딪힌다. ked `e340d72a`에서 화면 12 · 위젯 2에 `[UIViewKey("Ked.{타입}")]`를 붙이고, Presentation 12개의 파일 · 오브젝트 이름과 `presentationId`를 옮기고(GUID 그대로), 키 검사를 더했다. 그 커밋에서 다시 떴다. 글자 표 키(`{타입}.{Ref}`)는 그대로다 — `ViewTexts`는 타입 이름을 쓴다
- **UAC1001 — 실제 버그였다.** Variant 조건의 실험 칸 둘(`experimentKey` · `experimentVariantId`)이 에셋에 저장되지 않았다. `VariantId`에는 `[Serializable]`이 없었고, `ExperimentKey`는 있었지만 값 칸이 readonly라 Unity가 건너뛰었다. README가 말하는 "실험 배정" 조건은 코드로 만든 규칙에서만 돌았던 것이다. `7da70e6`에서 값을 비공개 `[SerializeField]` 칸으로 옮겼다(쓰는 쪽 코드는 그대로). ked 사본도 다시 들였다

## 확인

Unity(6000.6.2f1)에서 한다. 결과는 확인한 뒤 여기에 적는다.

1. Unity를 닫은 채 받는다(파일이 옮겨지고 빌드 설정이 바뀐다)
2. 메뉴 **UI Demo > Build Visual Novel Demo**(편집 모드) → 보고 문제 0
3. 플레이 → README "비주얼 노벨 UI 샘플"의 표대로
4. `Tools/Ked UI/Validate Sample UI` → 문제 0, `Tools/UI Presentation/Validate Screens` → Adaptive UI 줄이 없다, 키 경고 · UAC1001이 없다
5. 블랙홀 데모가 전과 같다

# 이 폴더는 복사 반입본이다

ked-presentation-runtime의 **VN UI 묶음**이 원본이다. 원본 `Assets/Samples/KedUI`에서 `App/`과 `Art/`를 뺀 것을 이 폴더에 그대로 둔다.

```
원본   ked-presentation-runtime   Assets/Samples/KedUI/   (Ked.Samples.UI.asmdef · asset-manifest.json · Common/ · Screens/)
반입   UIPresentationFlow         Assets/Demo/VisualNovel/KedUI/   ← 이 폴더
```

**사본만 고치지 않는다.** 화면 스크립트 · 빌드 명세 · 부품 키트 · 그림 · 글꼴 · manifest를 고칠 일이 생기면 ked에서 먼저 고치고 다시 들인다. ked가 이 저장소의 프레임워크를 들일 때와 같은 규율이다(방향만 반대).

이 폴더를 쓰는 앱(화면 흐름 · 가짜 재생 · 선택지 항목 · 글자 표 · 빌드 메뉴)은 `../App`이고, 그것은 이 저장소가 원본이다.

## 스냅샷

| | |
|---|---|
| 원본 | ked `feat/uipf-ui` @ `82ac9340` (2026-10-27) |
| 반입 | 2026-10-27 · 원본 커밋의 blob을 `git archive`로 풀어 복사(`.meta`째) |
| 원본 Unity | 6000.3.16f1. 이 저장소는 6000.6.2f1이다 — 높은 버전으로 여는 것이라 에셋도 들인다 |
| GUID | 원본 `.meta` 160개 그대로. 이 저장소의 `.meta`와 겹침 0. 이 폴더 자신의 `KedUI.meta`만 이 저장소에서 만든 것 |

**원본과 갈리면 이 표를 고친다.**

## 가져오지 않은 것

- `App/` — ked 앱. 재생 런타임에 잇는 것(흐름 · 호스트 · 대사창 · 선택지 어댑터 · 선택지 항목 · 연출 배치 DB · 글자 표 · 캔버스 · 앱 빌드)
- `Art/` — 받았지만 어느 화면도 쓰지 않는 그림 14장. `asset-manifest.json`에는 그 줄이 남아 있고, 그림 임포트 설정(`KedUIArtImporter`)이 PNG 없는 줄을 건너뛴다

## 사본에서 달라지는 것 — 이 저장소의 빌드 산출물

메뉴 **UI Demo > Build Visual Novel Demo**가 이 저장소의 Unity로 다시 짓는 것은 원본 blob과 달라진다(직렬화 버전 · fileID). 원본과 대조할 때 뺀다.

- `Screens/*/Prefabs/*.prefab`
- `Common/Fonts/NotoSansKR-* SDF.asset`, `Common/Fonts/KedUIFontSet.asset`(글꼴 칸을 빌드마다 다시 건다)
- 그림 `.png.meta`(임포트 설정을 manifest대로 다시 건다 — 값은 같고 직렬화만 바뀔 수 있다)

Presentation은 처음 한 번 만드는 데이터라 빌드가 덮지 않는다 — 원본 그대로다.

## 대조

```bash
git -C <ked> archive <원본 커밋> Assets/Samples/KedUI | tar -x -C /tmp/ked
diff -rq -x App -x App.meta -x Art -x Art.meta /tmp/ked/Assets/Samples/KedUI Assets/Demo/VisualNovel/KedUI
```

다른 것은 `Documentation~`(이 문서)와 위 산출물뿐이어야 한다.

## 다시 들이기

1. ked에서 고치고 커밋한다(ked의 Unity에서 `Tools/Ked UI/Build Sample UI` · 확인까지)
2. 이 폴더를 `Documentation~`만 남기고 비운 뒤, 원본 커밋의 blob을 풀어 `App/` · `Art/`를 빼고 넣는다(`.meta`째)
3. 이 저장소의 Unity에서 **UI Demo > Build Visual Novel Demo** — 산출물을 다시 짓는다
4. 위 표의 원본 커밋을 고친다

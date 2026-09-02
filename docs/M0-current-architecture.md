# M0 — Current Architecture Audit & Baseline

## Goal

현재 UI 시스템이 실제로 어떤 순서로 동작하고, 무엇이 살아 있으며, 무엇이 임시/Legacy 상태인지 확정한다.

M0의 목적은 리팩터링이 아니다.

> **변경 전에 현재 시스템의 실제 기준선을 확보한다.**

M1 이후의 모든 변경은 이 기준선과 비교하여 회귀 여부를 판단한다.

이 문서는 동시에 **시스템 전체를 읽는 지도** 역할을 한다. §4의 파일별 분류표가 그 지도다.

---

# 1. Current State (조사 결과로 확정)

## Repository baseline

| 항목 | 값 |
|---|---|
| Repository | `123456789qwaszx/UIPresentationFlow` |
| 작업 브랜치 | `해상도및비례대응과스파인` (로컬 `dev`와 동일 커밋) |
| HEAD | `0c968ab` (docs: 작업계획 수립) |
| 코드 tree 기준 커밋 | `3ffd08e` (chore: reorganize folders and remove unused classes) |
| `origin/dev`, `origin/main` | `3ffd08e` — docs 커밋은 미푸시 |
| Unity | `6000.2.7f2 (2b518236b676)` — Editor.log로 확인 |
| 렌더 | URP 2D, UGUI, TextMeshPro |
| Test Framework | `com.unity.test-framework 1.6.0` 설치됨 |
| asmdef | **0개** — 전 코드가 `Assembly-CSharp` / `Assembly-CSharp-Editor` |
| Tests 디렉터리 | 없음 |
| Spine Runtime | 없음 |
| C# 파일 | 52개, 5,760줄 |
| MonoBehaviour | `UIScreen`, `UISlot` 2개만 (Legacy 주석 제외) |
| 오서링된 UI asset | **0개** (`UIScreenCatalog`, `UIScreenSpecAsset`, `LayoutPatchSpec`, `ThemeSpec` 전부 없음) |
| 씬 | `SampleScene.unity` 1개 — 프로젝트 스크립트 참조 **0개** |
| 프리팹 | `ScreenTemplate.prefab` (UIScreen + UISlot), `UISlotPrefab.prefab` (UISlot) |

## 결정적 발견 요약

1. **런타임 컴포지션 루트가 없다.** `3ffd08e`에서 `GameBootstrap.cs`, `UISystemInitializer.cs`, `UIBootStrap.cs`가 삭제됐다. `new UIRouter` / `new UIResolver(` / `new UIScreenFactory` / `catalog.Init()` 호출처가 저장소 전체에 **0건**이다.
2. **오서링된 asset이 없다.** `332e575`, `3ffd08e`에서 `UIScreenCatalog.asset`, `UIScreenSpecAsset.asset`, `Layout_MainMenu_*.asset`, `New Theme Spec.asset`이 삭제됐다. 현재 `Assets` 아래 `.asset`은 URP 관련 4개뿐이다.
3. **asmdef가 없어 테스트 어셈블리를 만들 수 없다.** asmdef 테스트 어셈블리는 predefined `Assembly-CSharp`를 참조할 수 없다. M1의 테스트 게이트가 이 결정에 막힌다 (→ D0-5).
4. **52개 중 23개 파일(44%), 3,850줄(67%)이 제거 대상이다.** 주석 처리 12개 + 아무것도 발견하지 못하는 키 발견(discovery) 서브시스템 7개 + 참조 0인 static holder 1개 + Composer 데이터 오서링 전용 Editor 창 2개와 그 preset SO 1개 (→ §4, D0-6).
5. 논지를 지탱하는 코어(§4의 Keep/Refactor)는 **약 1,000줄**이다. 런타임 위젯 합성(Composer) 계열 ~500줄은 adaptive presentation과 별개의 논지이며 M3 결과로 판단한다 (→ D0-8).

---

# 2. Runtime Flow (소스 의도 기준)

```text
UIRouter.Navigate(UIActionKey)
        │
        ▼
RouteKeyResolver.TryGetRouteKey
        │  UIScreenCatalog._routeMap : route string → ScreenKey
        ▼
UIResolver.Resolve(screenKey, action)
        │
        ├─ UIScreenCatalog.TryGetScreenSpec → UIScreenSpec
        │
        ▼
UIVariantResolver.Resolve(spec, UIContext)
        │  base prefab/theme/layout
        │  → ScreenOverrides(forced variantId)
        │  → variants priority desc 순회 (Array.Sort — 원본 배열 정렬)
        ▼
ResolvedUIScreen { Prefab, Theme, Layout, AppliedVariantIds, DecisionTrace }
        │
        ├─ Theme.BuildPatches  → ThemeSpecPatch
        └─ Layout.BuildPatches → LayoutSpecPatch
        │
        ▼
UIResolveResult { Resolved, Patches, Trace }
        │
        ▼
UIScreenFactory.Create(result)
        │
        ├─ Object.Instantiate(resolved.Prefab, uiRoot)
        ├─ GetComponent<UIScreen>()          (없으면 strict 예외)
        ├─ screen.BuildSlotMap(binder, spec) (루트 슬롯만, strict:false)
        ├─ UIComposer.Compose(screen, spec)  (UISlot BFS → WidgetFactory → nameTag 맵)
        └─ UIPatchApplier.Apply(screen, patches)
        │
        ▼
UIScreen (nameTag → WidgetHandle 보유)
```

**실제 상태**: 이 흐름을 시작하는 코드가 없다. 위 다이어그램은 "코드가 의도하는 순서"이지 "현재 실행되는 순서"가 아니다.

책임 분리 자체는 유지 가치가 높다.

```text
Router   = 어떤 화면으로 갈 것인가        ← 논지와 직교, 축소 대상 (D0-7)
Resolver = 어떤 표현 결과를 사용할 것인가  ← 논지의 핵심
Factory  = 결과를 Unity Object로 만든다   ← 경계로서 유지
Composer = authored widget 구조를 생성     ← 별개 논지, 결정 필요 (D0-8)
Patch    = 생성된 결과의 theme/layout 변형 ← 논지의 핵심
```

---

# 3. 논지 파이프라인 (무엇을 이해해야 하는가)

이 프로젝트가 증명하려는 것은 한 줄이다.

> 환경을 명시적 사실(Context)로 만들고 → 선언적 규칙(Condition)으로 판정해 → 결정적 결과(Resolved)를 내고 → sparse diff(Patch)로 적용한다.

```text
UIContext (콘텐츠 사실) ─┐
DisplayContext (환경 사실, M1 신설) ┴→ VariantCondition → UIVariantRule → UIVariantResolver
                                                                              ↓
                                                              ResolvedUIScreen + DecisionTrace
                                                                              ↓
                                                        LayoutPatchSpec / ThemeSpec  (sparse diff)
                                                                              ↓
                                                              IUIPatch → UIPatchApplier
                                                                              ↓
                                          UIScreen ← WidgetHandle(nameTag) ← UIScreenFactory
```

여기서 **개념**과 **현재 구현**을 구분해야 한다.

| 개념 (살릴 것) | 현재 구현 (바꿔도 되는 것) |
|---|---|
| 콘텐츠 사실과 환경 사실의 분리 | `UIContext` struct + M1에서 만들 `DisplayContext` |
| 선언적 조건 | `VariantCondition` — 단 `Screen.*` 직접 조회는 제거 |
| 결정적 Resolver | `UIVariantResolver` — 단 `Array.Sort` mutation, priority 불일치 수정 |
| 결과 + 설명 가능성 | `ResolvedUIScreen` — `string DecisionTrace`는 M5에서 구조화 |
| sparse presentation diff | `LayoutPatchSpec` / `RectTransformPatch`의 `overrideX` 플래그 |
| 안정적 semantic target identity | `string nameTag` + `WidgetHandle` component bag — 축소 후보 |

---

# 4. 파일별 분류표 (Keep / Refactor / Decision / Remove)

판정 기준: **"adaptive presentation을 설명하는 데 필요한가?"**

- **Keep** — 논지를 지탱. 손대지 않거나 M2 순수성 보강만
- **Refactor** — 논지에 필요하지만 현재 구현에 결함
- **Decision** — 논지와 직교. 별도 결정 필요 (D0-7, D0-8)
- **Remove** — 아무 기능도 하지 않음. 존재 자체가 인지 부하

## 4.1 Core (13 files, 1,208 lines)

| 파일 | 줄 | 역할 | 판정 | 근거 |
|---|---:|---|---|---|
| `Core/ResolvedUIScreen.cs` | 32 | Resolve 결과. 불변. `AppliedVariantIds` + `DecisionTrace` | **Keep** | 결과와 근거를 함께 보유하는 출력 경계 |
| `Core/UIResolver.cs` | 60 | ScreenKey → Spec → VariantResolver → Patch 목록 + `UIResolveTrace` | **Refactor** | M2: DisplayContext 인자, catalog miss strict 처리, trace 중복 제거 |
| `Core/UIScreenFactory.cs` | 62 | Instantiate → BuildSlotMap → Compose → Apply | **Keep** | 순서가 정확. Composer 제거 시 축소되지만 "materialize 단계" 경계는 유지 |
| `Core/UIScreen.cs` | 192 | nameTag → WidgetHandle 맵 보유, 슬롯 맵 | **Refactor** | `GetWidgetDirect<T>` 영역(~60줄)은 사용처 0, 논지와 정반대(GameObject.name DFS) → 제거. `BuildRequiredTemplateSlotIds`는 Composer 전용 규칙 |
| `Core/UIScreenSpec.cs` | 86 | `UIScreenSpec` / `SlotSpec` / `WidgetSpec` / enums | **Refactor** | `UIScreenSpec`의 `screenKey/templatePrefab/baseTheme/baseLayout/variants[]`는 Keep. `SlotSpec/WidgetSpec`는 D0-8 종속 |
| `Core/SO/UIScreenSpecAsset.cs` | 8 | `UIScreenSpec` SO 래퍼 | **Keep** | — |
| `Core/SO/UIScreenCatalog.cs` | 182 | ScreenKey→Spec 레지스트리 + route→ScreenKey + Editor 검증 | **Refactor** | Spec 레지스트리 절반 Keep. route map 절반은 D0-7 종속. M2에서 variantId 검증 추가 |
| `Core/WidgetHandle.cs` | 39 | nameTag + Button/Text/Image/Toggle/Slider/CanvasGroup bag | **Refactor** | 개념(semantic target)은 Keep. component bag은 `{Id, RectTransform}` 마커로 축소 후보. `ThemeSpecPatch`가 `Text/TextRole`에 의존하므로 함께 결정 |
| `Core/UISlot.cs` | 33 | 슬롯 마커 (`id → RectTransform`) | **Decision (D0-8)** | nameTag 마커와 같은 개념("ID → RectTransform")이 생성 주체로 갈라진 것. Composer 제거 시 하나로 합류 후보 |
| `Core/UISlotBinder.cs` | 120 | UISlot 스캔 → 슬롯 맵. 마커 없으면 name fallback | **Decision (D0-8)** | 실제 호출은 항상 `strict:false`. 이중 경로(marker/name)는 정리 필요 |
| `Core/UIComposer.cs` | 127 | SlotSpec 기반 런타임 위젯 트리 생성 (UISlot BFS) | **Decision (D0-8)** | adaptive presentation과 별개의 논지("data-driven UI 구축") |
| `Core/WidgetFactory.cs` | 197 | WidgetSpec → 타입별 프리팹 인스턴스 | **Decision (D0-8)** | 생성자가 타입당 프리팹 필드 1개. `BindActionIfNeeded`는 키 계산 후 no-op → 이 메서드와 `//IUiActionBinder` 주석 배선은 **Remove** |
| — | — | — | — | — |

## 4.2 Patcher (12 files + Action 6, 827 lines)

| 파일 | 줄 | 역할 | 판정 | 근거 |
|---|---:|---|---|---|
| `Patcher/UIContext.cs` | 29 | `ThemeId / LocaleId / Experiments / ScreenOverrides` — 콘텐츠 축 | **Keep** | DisplayContext와 분리되는 한쪽 축 |
| `Patcher/VariantCondition.cs` | 133 | 조건 판정. theme/locale/experiment/platform/aspect | **Refactor** | `Application.platform`(L89), `Screen.height`(L130), `Screen.width`(L132) 직접 조회 3곳 → M1/M2 제거. `VariantPlatform.Console` 도달 불가(default→Desktop). `Any` enum값은 `usePlatform=false`와 중복 |
| `Patcher/UIVariantRule.cs` | 16 | variantId / priority / condition / 3 override 필드 | **Keep** | — |
| `Patcher/UIVariantResolver.cs` | 123 | 판정 핵심 | **Refactor** | ① `Array.Sort(spec.variants)` — authored 배열 mutation ② `Array.Sort`는 **unstable** — 동순위 순서 비결정 ③ prefab만 lock, theme/layout은 매치마다 덮어씀 → priority 의미 불일치 |
| `Patcher/IUIPatch.cs` | 3 | `Apply(UIScreen)` | **Keep** | 최소 plumbing. SafeAreaPatch(M4)가 그대로 꽂힘 |
| `Patcher/UIPatchApplier.cs` | 11 | patch foreach | **Keep** | — |
| `Patcher/LayoutPatchSpec.cs` | 52 | `WidgetLayoutPatch[nameTag]` + `RectTransformPatch` sparse diff SO | **Keep** | M3 responsive의 핵심 재사용 자산 |
| `Patcher/LayoutSpecPatch.cs` | 86 | LayoutPatchSpec을 WidgetHandle에 적용 | **Keep** | `GetWidgetHandle` 실패 시 경고가 `UIScreen`과 여기서 2번 찍힘 → 정리 |
| `Patcher/ThemeSpec.cs` | 21 | 폰트/크기/색 SO (스텁) | **Decision (동결)** | UIContext 축에서 시각적 출력을 가진 유일한 조건. M2 priority 버그가 theme/layout 간 불일치 그 자체. 확장 금지, 삭제도 보류 |
| `Patcher/ThemeSpecPatch.cs` | 54 | 전 위젯 순회, `TextRole`별 fontSize/color 대입 + `UITextRole` enum | **Decision (동결)** | `WidgetHandle.Text/TextRole`에 의존 — WidgetHandle 축소 시 함께 조정 |
| `Patcher/WidgetRectApplier.cs` | 32 | `WidgetSpec.rectMode == OverrideInSlot`일 때 rect 대입 | **Decision (D0-8)** | Composer 전용 |
| `Patcher/WidgetPresetCatalog.cs` | 21 | Editor용 rect preset SO | **Remove** | 유일 소비자 `UIScreenSpecEditorWindow` 삭제로 소비자 0 |
| `Patcher/Action/*.cs` (6) | 212 | `IUiActionBinder`, `UIActionBinder`, `CompositeUiActionBinder`, `RouteActionBinder`, `HudPresenter`, `IHudView` | **Remove** | **전량 주석 처리.** `HudPresenter/IHudView`는 게임 레이어 관심사로 UI 프레임워크 소속도 아님. 파일명 `IUIActionBinder.cs` ↔ 타입명 `IUiActionBinder` 불일치 |

## 4.3 Keys (8 files, 155 lines)

| 파일 | 줄 | 역할 | 판정 | 근거 |
|---|---:|---|---|---|
| `Keys/ScreenKey.cs` | 17 | `[Serializable] struct`, 값 객체 | **Keep** | — |
| `Keys/VariantId.cs` | 35 | 값 객체, string 암시 변환 | **Keep** | — |
| `Keys/ExperimentKey.cs` | 28 | 값 객체 + `UIExperiments.HomeLayoutTest` 샘플 상수 | **Keep** | 상수는 샘플, 필요 시 이동 |
| `Keys/UIActionKey.cs` | 39 | 값 객체 (internal ctor, 생성 경로 제한) | **Decision (D0-7)** | Router가 축소되면 소비자 없음 |
| `Keys/UIActionKeyRegistry.cs` | 20 | string → UIActionKey 캐시 | **Remove** | 유일한 살아있는 호출처가 `WidgetFactory.BindActionIfNeeded`(no-op) |
| `Keys/UIScreenKeyAttribute.cs` | 7 | `[UIScreenKey]` 마커 | **Remove** | **살아있는 적용처 0** (주석 처리된 Legacy에만 존재) |
| `Keys/UIRouteDefinitionAttribute.cs` | 5 | `[UIRouteDefinition]` 마커 | **Remove** | **살아있는 적용처 0** |
| `Keys/UIRouteKeyAttribute.cs` | 4 | `[UIRouteKey]` PropertyAttribute | **Remove** | `UIScreenCatalog.UIRouteEntry.route`에 적용돼 있으나 drawer가 항상 plain string으로 폴백 |

## 4.4 Editor (6 files, 3,479 lines)

| 파일 | 줄 | 역할 | 판정 | 근거 |
|---|---:|---|---|---|
| `Editor/Internal/UIScreenCatalogEditor.cs` | 30 | Catalog Inspector + "Verify Route Mapping" 버튼 | **Keep** | M2 validation 확장 지점 |
| `Editor/Internal/UIScreenKeyDiscovery.cs` | 63 | **AppDomain 전체 어셈블리를 리플렉션 순회**해 `[UIScreenKey]` 수집 | **Remove** | 수집 결과 항상 0개 |
| `Editor/Internal/ScreenKeyDropdownDrawer.cs` | 39 | ScreenKey 드롭다운 | **Remove** | `UIScreenKeyDiscovery.All`이 비어 있어 항상 빈 드롭다운 |
| `Editor/Internal/UIRouteKeyDrawer.cs` | 56 | route 드롭다운 (`TypeCache<UIRouteDefinitionAttribute>`) | **Remove** | 후보 0개 → 항상 `PropertyField` 폴백 |
| `Editor/Tools/UIScreenSpecEditorWindow.cs` | 2,787 | Spec 오서링 창. Slot/Widget 편집, preset, clipboard, slot graph | **Remove** | `SlotSpec/WidgetSpec`(Composer 데이터) 오서링 전용. 논지에 필요한 `variants/baseTheme/baseLayout/templatePrefab`은 전부 `[Serializable]` 평범한 필드라 **기본 Inspector로 충분**. 편집할 asset도 현재 0개. (`CenterMinWidth/MaxWidth` 뒤바뀜 F10은 삭제로 해소) |
| `Editor/Tools/UIScreenSlotImporterWindow.cs` | 504 | 프리팹 UISlot 구조 → UIScreenSpecAsset slots 흡수 | **Remove** | Composer 데이터 오서링 전용. 복원: `git checkout 3ffd08e -- Assets/scripts/UI/Editor/Tools/` |

## 4.5 Legacy + Root (7 files, 132 lines)

| 파일 | 줄 | 판정 | 근거 |
|---|---:|---|---|
| `Legacy/UIBootStrap.cs` | 49 | **Remove** | 전량 주석. 과거 컴포지션 루트 — 부활이 아니라 새로 작성 (D0-1) |
| `Legacy/UITestDriver.cs` | 33 | **Remove** | 전량 주석 |
| `Legacy/UIOpener.cs` | 27 | **Remove** | 전량 주석 |
| `Legacy/DefaultActionKeys.cs` | 8 | **Remove** | 전량 주석 |
| `Legacy/DefaultRouteKeys.cs` | 8 | **Remove** | 전량 주석 |
| `Legacy/DefaultScreenKeys.cs` | 4 | **Remove** | 전량 주석 |
| `UIRuntimeRouter.cs` | 3 | **Remove** | 소유자 없는 static holder. 실참조 0 (주석 처리된 Legacy 한 줄만) |

## 4.6 집계

| 판정 | 파일 | 줄 | 비고 |
|---|---:|---:|---|
| Keep | 13 | ~530 | 논지 그 자체 |
| Refactor | 6 | ~750 | 논지에 필요, M1~M2에서 수정 |
| Decision — Router (D0-7) | 2 | ~100 | `UIRouter`, `UIActionKey` |
| Decision — Composer 런타임 (D0-8) | 6 | ~510 | `UIComposer`, `WidgetFactory`, `WidgetRectApplier`, `UISlot`, `UISlotBinder` (+ `UIScreenSpec.cs` 내 `SlotSpec/WidgetSpec`) |
| Decision — Theme (동결) | 2 | 75 | |
| **Remove (확정)** | **23** | **3,850** | 죽은 코드 20 (538줄) + Composer 오서링 도구 3 (3,312줄). 파일 수 44%, **줄 수 67%** |
| **합계** | **52** | **5,760** | |

> 논지 코어(Keep+Refactor)는 약 1,280줄이고 그 중 `UIScreen`의 우회로 등을 빼면 **~1,000줄**이 진짜 이해해야 할 범위다. Remove 실행 결과 저장소는 **29 파일 / 1,819줄**이 됐다.

---

# 5. 확인된 결함 목록 (M0에서 수정하지 않음, 이후 마일스톤 참조용)

| # | 위치 | 결함 | 처리 마일스톤 |
|---|---|---|---|
| F1 | `VariantCondition.cs:89,130,132` | `Application.platform`, `Screen.width/height` 직접 조회 — hidden input | M1/M2 |
| F2 | `UIVariantResolver.cs:41` | `Array.Sort(spec.variants)` — authored 배열 mutation | M2 |
| F3 | `UIVariantResolver.cs:41` | `Array.Sort`는 unstable → 동순위 순서 비결정 | M2 |
| F4 | `UIVariantResolver.cs:49-72` | prefab만 lock, theme/layout은 매치마다 덮어씀 → priority 의미 불일치 | M2 |
| F5 | `UIResolver.cs:44-48` | catalog miss 시 `LogError` 후 `baseSpec=null`로 진행 → `UIVariantResolver`에서 `ArgumentNullException`. 실패 원인 소실 | M2 |
| F6 | `UIRouter.cs:44-56` | 같은 key 재진입 시에만 기존 화면 파괴 → 다른 화면으로 이동하면 **이전 화면이 살아서 겹침**. 매 내비게이션마다 `Debug.Log(trace)` 무조건 출력 | D0-7 |
| F7 | `VariantCondition.cs:87-102` | `DetectCurrentPlatform` default→Desktop. `VariantPlatform.Console` 도달 불가. OSX/Linux 미명시 | M1 매핑표 |
| F8 | `UIScreen.cs:19` + `LayoutSpecPatch.cs:27` | 같은 nameTag miss에 경고 2회 | M2/M6 |
| F9 | `WidgetFactory.cs:146-150` | `BindActionIfNeeded` 키 계산 후 no-op | D0-6 |
| F10 | `UIScreenSpecEditorWindow.cs:52-53` | `CenterMinWidth=3000f`, `CenterMaxWidth=380f` 뒤바뀜 | D0-6 삭제로 해소 |
| F11 | `VariantCondition.cs` | `aspectMin > aspectMax` 검증 없음 | M2 validation |
| F12 | `SampleScene.unity` CanvasScaler | `Constant Pixel Size / Scale 1 / Ref 800×600` — responsive baseline 아님 | M3 |

---

# 6. Runtime Bootstrap / Legacy 상태 — 결론

M0 문서 초안의 네 가지 가설 중:

```text
A. 다른 활성 Bootstrap이 존재한다.               → 아니오
B. SampleScene이 과거 직렬화 상태에 의존한다.      → 아니오 (씬에 프로젝트 스크립트 0개)
C. 현재 runtime demo는 깨져 있고 Editor authoring만 살아 있다.  → ✅ 확정
D. 별도의 수동 wiring 절차가 있다.                → 아니오
```

**결론 C.** 근거:

- `3ffd08e` diff: `Assets/scripts/Game/GameBootstrap.cs`, `UISystemInitializer.cs`, `Assets/scripts/CPS/UIBootStrap.cs` 삭제
- `SampleScene.unity`의 `m_Script` GUID 전부를 `Assets/scripts/**/*.cs.meta`와 대조 → 일치 0
- 씬 구성: `CanvasForTest`, `EventSystem`, `Main Camera`, `Global Light 2D`, 위젯 프리팹 루즈 오브젝트 6개(`TextWidgetPrefab`, `ButtonWidgetPrefab`, `ImagePrefab`, `TogglePrefab`, `SliderPrefab`, `EmptyObjectPrefab`)
- `Assets/Prefabs/ScreenTemplate.prefab`: root에 `UIScreen` + `UISlot` 정상 부착 (`CanvasRenderer` 포함) — 프리팹은 살아 있음

---

# 7. M0 Decisions

## D0-1 Runtime composition root — **없음. 신설.**

Legacy `UIBootStrap`을 부활시키지 않는다. M3에서 `AdaptiveUIDemo.unity`와 함께 현재 구조 기준으로 최소 Installer를 새로 작성한다 (이름 후보: `UIPresentationInstaller`). 그 전까지 runtime은 "존재하지 않음"으로 기록한다.

## D0-2 Catalog Init owner — **신설 Installer.**

현재 소유자 없음. D0-1의 Installer가 `catalog.Init()`을 소유한다.

## D0-3 Resolver failure policy — **Strict.**

`UIScreenFactory`가 이미 예외 기반이므로 일관성상 `Resolve`도 unknown ScreenKey에 명확한 예외(`KeyNotFoundException`, 메시지에 ScreenKey 포함)를 던진다. M2 문서 §9 권고와 일치. Router의 unknown route fallback은 별도 책임으로 유지(축소 후에도).

## D0-4 Responsive sample screen — **없음. M3에서 오서링 프리팹으로 신설.**

기존 asset 0개. M3 canonical screen은 **`UIComposer` 없이 오서링된 프리팹 + `nameTag` 마커**로 먼저 시도한다. 이 시도가 D0-8의 판단 근거가 된다.

## D0-5 Test location — **DECISION REQUIRED (M1 진입 조건)**

```text
권고: Assets/Tests/EditMode/UIPresentationFlow.Tests.asmdef
```

**블로커**: asmdef는 predefined `Assembly-CSharp`를 참조할 수 없다. 따라서 테스트 어셈블리를 만들려면 Runtime 코드도 asmdef로 분리해야 한다.

선택지:

| | 방법 | 장점 | 단점 |
|---|---|---|---|
| ⓐ | M1에서 `UIPresentationFlow.Runtime.asmdef` + `.Editor.asmdef` + `.Tests.asmdef` 3개 도입 (M6 작업 선행) | 원칙 4(테스트 없이 DONE 금지) 준수. 컴파일 경계 확보 | M6 예정 작업이 앞당겨짐. Editor 폴더 특수 규칙 대신 asmdef 참조로 전환 |
| ⓑ | 테스트를 `Assembly-CSharp-Editor`에 두고 Test Framework의 predefined assembly 지원 사용 | 구조 변경 0 | Unity 버전별 지원 편차, Editor 코드와 테스트 혼재 |
| ⓒ | `BLOCKED` 기록 후 M1을 테스트 없이 진행 | — | 원칙 4 위반. 이후 모든 마일스톤의 테스트 게이트가 연쇄 지연 |

**결정: ⓐ 채택. M0 종료 시점에 실행 (M1.4 선행).**

```text
Assets/scripts/UIPresentationFlow.Runtime.asmdef        refs: UnityEngine.UI, Unity.TextMeshPro
Assets/scripts/UI/Editor/UIPresentationFlow.Editor.asmdef   refs: Runtime / Editor only
Assets/Tests/EditMode/UIPresentationFlow.Tests.asmdef   refs: Runtime, TestRunner / nunit / UNITY_INCLUDE_TESTS
Assets/Tests/EditMode/UIVariantResolverSmokeTests.cs    배선 검증용 2개 (base 반환, theme rule → layout override)
```

- Runtime asmdef를 `Assets/scripts/` 루트에 두어 M1의 `Assets/scripts/Display/`가 자동 포함된다.
- Editor 폴더는 하위 asmdef로 분리되어 Runtime 어셈블리에서 제외된다 ("Editor" 특수 폴더 규칙 대신 `includePlatforms: Editor`).
- Smoke test는 platform/aspect 조건을 켜지 않으므로 `Screen`/`Application` 접근이 없다 — M2 이전에도 EditMode에서 안전.
- 참조 이름은 GUID 대신 어셈블리 이름 문자열 사용.

## D0-6 Dead code cleanup — **M1 진입 전 실행. 별도 커밋.**

§4의 **Remove 23개 파일 + 파일 내부 3곳**을 제거한다.

- 죽은 코드 20개: 기능 영향 0 (전부 주석 처리 또는 참조 0 확인됨)
- Composer 오서링 도구 3개: 대체 수단 있음 (기본 Inspector). 편집 대상 asset 0개. **런타임 Composer는 D0-8까지 유지** — 도구 삭제와 런타임 경로 삭제는 별개 결정

```text
# 죽은 코드
Assets/scripts/UI/Legacy/                       (6 files)
Assets/scripts/UI/Patcher/Action/               (6 files)
Assets/scripts/UI/UIRuntimeRouter.cs
Assets/scripts/UI/Keys/UIActionKeyRegistry.cs
Assets/scripts/UI/Keys/UIScreenKeyAttribute.cs
Assets/scripts/UI/Keys/UIRouteDefinitionAttribute.cs
Assets/scripts/UI/Keys/UIRouteKeyAttribute.cs
Assets/scripts/UI/Editor/Internal/UIScreenKeyDiscovery.cs
Assets/scripts/UI/Editor/Internal/ScreenKeyDropdownDrawer.cs
Assets/scripts/UI/Editor/Internal/UIRouteKeyDrawer.cs

# Composer 오서링 도구 (복원: git checkout 3ffd08e -- <path>)
Assets/scripts/UI/Editor/Tools/UIScreenSpecEditorWindow.cs
Assets/scripts/UI/Editor/Tools/UIScreenSlotImporterWindow.cs
Assets/scripts/UI/Patcher/WidgetPresetCatalog.cs

파일 내부:
UIScreen.cs        — #region 프리팹전용 우회로 (GetWidgetDirect, FindChildByName, _directWidgetCache)
WidgetFactory.cs   — BindActionIfNeeded + 호출부 + //IUiActionBinder 주석 배선
UIScreenCatalog.cs — UIRouteEntry.route의 [UIRouteKey] 어트리뷰트

부수:
Assets/Docs/CPS_Screen-SlotConvention.txt   → docs/CPS_Screen-SlotConvention.txt 로 이동
                                              (§3 nameTag 유일 규칙은 M3 convention의 원형. §1-2는 Composer 규칙 — D0-8 참조용)
.idea/ tracked 상태                          → untrack + .gitignore (별도 커밋)
```

**순서**: 이 문서(M0 기록) 커밋 → cleanup 커밋. 발견이 기록된 뒤에 지워야 "죽은 코드 38%를 발견하고 제거했다"가 서사로 성립한다.

**실행 기록**: 문서 커밋 `c20dec9` → cleanup 커밋 (다음). 삭제 후 `grep`으로 삭제 심볼 잔여 참조 0 확인. Unity 재컴파일 확인은 Editor 포커스 시 수행.

## D0-7 Router 축소 — **`Show(ScreenKey)` 1홉으로.**

현재 `UIActionKey → route string → ScreenKey`는 3홉이고, F6 결함(화면 누적)이 있다. 백스택/레이어/전환/생명주기를 만들 계획이 없으므로 반쯤 만든 라우터는 감점 요인이다. 데모 네비게이션은 `Show(ScreenKey)`로 충분하다.

영향: `UIRouter` 축소, `RouteKeyResolver` 제거, `UIActionKey` 제거 후보, `UIScreenCatalog.routes` 제거 후보, `WidgetSpec.onClickRoute` 제거 후보.

실행 시점: D0-6 직후 또는 M1 중. M2 이전 완료.

## D0-8 Composer 런타임 — **M3 canonical screen 결과로 판단. 지금 삭제하지 않음.**

`UIComposer`, `WidgetFactory`, `WidgetRectApplier`, `UISlot/UISlotBinder`, `UIScreenSpec.cs` 내 `SlotSpec/WidgetSpec` (~510줄). 오서링 도구는 D0-6에서 먼저 제거된다.

이것은 "data-driven UI 구축"이라는 **별개의 논지**다. PLAN이 주장하는 adaptive presentation과 직교한다.

런타임 경로를 도구와 함께 지우지 않는 이유: 도구는 대체 수단(기본 Inspector)이 있지만, 런타임 경로는 "프리팹 경로만으로 adaptive가 성립한다"가 증명되기 전에 지우면 실패 시 급한 복원이 된다.

판단 절차:

1. M3 canonical screen을 오서링 프리팹 + nameTag 마커로 작성
2. `Instantiate → Register targets → Apply patches`만으로 adaptive pipeline이 성립하는지 확인
3. 성립하면 Composer 계열은 논지 밖 → 제거 또는 별도 브랜치 보존
4. 이때 `UISlot`(id→RectTransform)과 `nameTag`(tag→widget)를 하나의 마커(`UIPresentationTarget { Id, RectTransform }` 후보)로 합류

`ThemeSpec`은 이 결정과 별개로 **동결**한다 — UIContext 축의 시연 필드로 남기되 확장하지 않는다.

---

# 8. Implementation Tasks — 결과

| Task | 상태 | 결과 |
|---|---|---|
| M0.1 Branch baseline 기록 | ✅ | §1 |
| M0.2 Runtime call-flow 확인 | ✅ 정적 | §2, §6. 실행 검증은 실행 대상이 없어 불가 (결론 C) |
| M0.3 Active/Legacy 분류 | ✅ | §4 |
| M0.4 SampleScene baseline | ✅ | §6, §10 Gate B |
| M0.5 Editor baseline | ✅ (목적 변경) | §10 Gate C — Tools 창 삭제로 컴파일 통과만 baseline |
| M0.6 Test baseline | ✅ | asmdef 0, 테스트 0 → D0-5 ⓐ 실행: asmdef 3개 + smoke test 2개 |

---

# 9. Static Verification 결과

| 항목 | 결과 |
|---|---|
| Runtime composition root | 없음 (§6) |
| Catalog Init caller | 없음 |
| `Screen.*` / `Application.platform` 사용처 | `VariantCondition.cs` 3곳만 |
| `GetWidgetDirect` 사용처 | 0 |
| Legacy type 사용처 | 0 (전량 주석) |
| `spec.variants` mutation 위치 | `UIVariantResolver.cs:41` |
| `[UIScreenKey]` / `[UIRouteDefinition]` 적용처 | 0 (주석 처리된 Legacy만) |
| tests / asmdef | 0 / 0 |
| 오서링 asset | 0 |

---

# 10. Manual Verification

## Gate A — Project ✅

Editor.log (`%LOCALAPPDATA%\Unity\Editor\Editor.log`, 2026-09-03 세션) 기준:

```text
Version 6000.2.7f2 (2b518236b676)
projectpath = C:\Users\river\Documents\GitHub\UIPresentationFlow
Tundra build success (16.91 seconds)
AssetDatabase: script compilation time: 17.729347s
error CS   : 0
Assets/scripts 경고 : 0
```

- [x] Unity 6000.2.7f2에서 열림
- [x] Compile Error 0

## Gate B — Scene ✅ (사용자 확인, D0-6 cleanup 후)

정적 결론(씬에 프로젝트 스크립트 0)이 실행으로 확인됐다.

- [x] SampleScene 열림
- [x] Play Mode 진입 가능
- [x] UI 표시 — 예상대로: 위젯 프리팹 루즈 오브젝트만, UIScreen 생성 없음
- [x] navigation — 불가 (Router 인스턴스 없음)
- [x] Console — cleanup 후 재컴파일 error 0

## Gate C — Editor (D0-6 확장으로 목적 변경)

두 Tools 창은 D0-6에서 삭제 확정되어 "열림 확인" baseline의 의미가 없어졌다. 컴파일 통과(Gate A)로 존재 baseline은 확보됨. 기록 목적으로만 선택 확인:

- [ ] (선택) `Tools/UI/UIScreen Spec Editor` 열림 — 삭제 전 마지막 기록
- [ ] (선택) `Tools/UI/Slot Importer` 열림
- 유지되는 Editor 코드: `UIScreenCatalogEditor`(Inspector) — 확인 대상 asset 0개라 M3에서 Catalog 생성 후 확인

---

# 11. Completion Checklist

- [x] baseline SHA 기록
- [x] Runtime composition root 확인 → 없음
- [x] `UIScreenCatalog.Init()` 소유자 확인 → 없음
- [x] 실제 Runtime flow 확인 → 의도 흐름 문서화, 실행 흐름 부재
- [x] Legacy 사용 여부 확인 → 0
- [x] `GetWidgetDirect` 사용 여부 확인 → 0
- [x] Resolver mutation/priority 문제 기록 → F2, F3, F4
- [x] SampleScene baseline 확인 (정적)
- [x] CanvasScaler baseline 확인 → F12
- [x] Editor Window baseline 확인 → Gate C (Tools 창 삭제로 컴파일 통과 baseline만)
- [x] Test assembly baseline 확인 → 0 → D0-5 ⓐ 실행
- [x] M1 변경 대상 파일 확정 → D0-6 (삭제), M1 문서 §13 (추가)
- [x] 모든 수동 검증 결과 기록 → Gate A/B 완료, smoke test green은 Test Runner 확인 대기

---

# 12. Completion Record

```text
Status: DONE (pending: Test Runner에서 smoke test 2개 green 확인)

Baseline commit/tree:  HEAD 0c968ab / code tree 3ffd08e
Branch:                해상도및비례대응과스파인 (= local dev)
Unity version:         6000.2.7f2 (2b518236b676)

Runtime composition root:  없음 (3ffd08e에서 삭제). 결론 C.
Catalog init owner:        없음. → D0-1 Installer 신설

SampleScene:
- Compile:     0 errors, 0 project warnings (Editor.log) / cleanup 후 재컴파일 0 errors (사용자 확인)
- Play Mode:   진입 가능
- Navigation:  불가 (Router 인스턴스 없음) — 예상대로
- Variant:     불가 — asset 0개
- Trace:       불가 — 실행 경로 없음
- Console:     error 0

Editor Tool:
- Spec Editor:    D0-6에서 삭제 — baseline은 컴파일 통과로 대체
- Slot Importer:  D0-6에서 삭제
- 유지:           UIScreenCatalogEditor (Inspector) — 대상 asset 없어 M3에서 확인

Tests:
- Existing test assembly: 없음 → D0-5 ⓐ 실행: Runtime / Editor / Tests asmdef 3개 생성
- Existing test count:    0 → smoke test 2개 추가 (Test Runner green 확인 대기)

Legacy:
- Active dependencies: 0 (Legacy 6 + Action 6 전량 주석)

Remove 확정: 23 files / 3,850 lines
  - 죽은 코드 20 files / 538 lines
  - Composer 오서링 도구 3 files / 3,312 lines (기본 Inspector로 대체)
  - 파일 내부: UIScreen 우회로 ~60줄, WidgetFactory no-op, [UIRouteKey] 어트리뷰트
  - 실행 후: 29 files / 1,819 lines ✅

Known blockers:
- 없음 (D0-5 해소)

Decision summary:
- D0-1 Installer 신설 (M3)
- D0-2 Installer가 Init 소유
- D0-3 Resolve strict
- D0-4 canonical screen = 오서링 프리팹 (M3)
- D0-5 asmdef ⓐ 채택 + 실행 (Runtime/Editor/Tests)
- D0-6 실행 완료 — 23 files / 3,850 lines 제거, 잔존 29 files / 1,819 lines (cleanup 커밋). 잔여 참조 0 (grep 검증)
- D0-7 Router → Show(ScreenKey)
- D0-8 Composer 런타임 — M3 결과로 판단, ThemeSpec 동결

M1 entry approved: CONDITIONAL
  → Test Runner (Window > General > Test Runner > EditMode) 에서
    UIVariantResolverSmokeTests 2개 green 확인 시 YES
```

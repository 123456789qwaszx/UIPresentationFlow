# UIPresentationFlow Rebuild Plan

## 0. 문서 목적

이 문서는 기존 `UIPresentationFlow`를 단순 보수하는 것이 아니라, 현재 구조를 다시 정리하고 실제 다중 해상도/화면비 문제에 적용하여 **Adaptive UI Presentation Framework**로 발전시키기 위한 전체 계획서다.

핵심 목표는 다음과 같다.

> 다양한 디바이스 환경을 하나의 환경 모델로 표현하고, UI가 그 환경을 직접 조회하지 않고 Resolver를 통해 일관되게 해석되도록 만든다.

Spine 연동은 본 계획의 핵심 범위가 아니다. UI 구조와 환경 모델, 해상도/화면비 대응, Safe Area, Preview/Trace/Test까지 마무리한 뒤 선택적으로 연결한다.

---

# 1. 현재 저장소 상태

## 1.1 프로젝트 환경

- Repository: `123456789qwaszx/UIPresentationFlow`
- Working branch target: `dev`
- Unity: `6000.2.7f2`
- URP 2D 사용
- UGUI 사용
- Unity Test Framework 포함
- Spine Unity Runtime은 현재 설치되어 있지 않음

현재 주요 코드 경로는 다음과 같다.

```text
Assets/scripts/UI
├─ Core
│  ├─ ResolvedUIScreen.cs
│  ├─ UIComposer.cs
│  ├─ UIResolver.cs
│  ├─ UIRouter.cs
│  ├─ UIScreen.cs
│  ├─ UIScreenFactory.cs
│  ├─ UIScreenSpec.cs
│  ├─ UISlot.cs
│  ├─ UISlotBinder.cs
│  ├─ WidgetFactory.cs
│  └─ WidgetHandle.cs
│
├─ Editor
│  ├─ Internal
│  └─ Tools
│     ├─ UIScreenSlotImporterWindow.cs
│     └─ UIScreenSpecEditorWindow.cs
│
├─ Keys
│
├─ Legacy
│
└─ Patcher
   ├─ Action
   ├─ IUIPatch.cs
   ├─ LayoutPatchSpec.cs
   ├─ LayoutSpecPatch.cs
   ├─ ThemeSpec.cs
   ├─ ThemeSpecPatch.cs
   ├─ UIContext.cs
   ├─ UIPatchApplier.cs
   ├─ UIVariantResolver.cs
   ├─ UIVariantRule.cs
   ├─ VariantCondition.cs
   ├─ WidgetPresetCatalog.cs
   └─ WidgetRectApplier.cs
```

---

## 1.2 현재 구조의 핵심 설계

현재 코드에는 이미 다음 흐름이 존재한다.

```text
UIScreenSpec
    │
    ├─ templatePrefab
    ├─ baseTheme
    ├─ baseLayout
    └─ variants[]
           │
           ▼
    UIVariantResolver
           │
           ├─ UIContext
           └─ VariantCondition
           │
           ▼
    ResolvedUIScreen
           │
           ▼
    Composer / Patch / Unity UI
```

즉 현재 프로젝트의 중요한 설계 자산은 이미 존재한다.

```text
Spec
→ Context
→ Resolver
→ Resolved Result
→ Patch / Compose
→ Unity Object
```

이번 리빌드에서는 이 흐름을 버리지 않는다.

오히려 실제 해상도/화면비 문제를 이 구조로 해결하면서 설계 의도를 검증한다.

---

# 2. 현재 코드에서 확인된 핵심 문제

## 2.1 VariantCondition이 환경 탐색까지 책임진다

현재 `VariantCondition`은 규칙 정의 객체이면서 동시에 다음 Unity API를 직접 조회한다.

```text
Screen.width
Screen.height
Application.platform
```

즉 현재는 다음 책임이 섞여 있다.

```text
VariantCondition
├─ 규칙 표현
├─ 규칙 판정
└─ 현재 실행 환경 탐색
```

목표 구조는 다음과 같다.

```text
Unity Runtime
     │
     ▼
DisplayContextProvider
     │
     ▼
DisplayContext
     │
     ▼
VariantCondition
```

`VariantCondition`은 전달받은 Context만 읽어야 한다.

---

## 2.2 UIContext와 Display 환경의 성격이 다르다

현재 `UIContext`에는 다음 정보가 들어 있다.

```text
ThemeId
LocaleId
Experiments
ScreenOverrides
```

이들은 콘텐츠/운영/UI 정책에 가까운 정보다.

반면 다음 값은 디바이스/표시 환경에 해당한다.

```text
Resolution
AspectRatio
SafeArea
Orientation
Platform
```

따라서 한 객체에 모두 넣지 않고 별도의 `DisplayContext`를 둔다.

목표:

```text
Presentation Context
├─ UIContext
│  ├─ Theme
│  ├─ Locale
│  ├─ Experiments
│  └─ Overrides
│
└─ DisplayContext
   ├─ Resolution
   ├─ AspectRatio
   ├─ SafeArea
   ├─ Orientation
   └─ Platform
```

---

## 2.3 Resolver가 입력 Spec을 변경할 가능성이 있다

현재 `UIVariantResolver`는 우선순위 정렬을 위해 `spec.variants` 배열 자체를 정렬하는 방식에 가깝다.

Resolver의 이상적인 계약은 다음과 같다.

```text
Spec = authored input
Context = runtime input
Resolve = read only calculation
Resolved = output
```

즉 Resolver는 원본 Spec을 변경하지 않는 순수한 판정 단계에 가까워야 한다.

이번 리빌드에서는 이 원칙을 명확히 한다.

---

## 2.4 기존 LayoutPatch는 재사용 가치가 높다

현재 `LayoutPatchSpec`은 위젯 단위로 다음 값을 선택적으로 덮어쓸 수 있다.

```text
active
anchorMin
anchorMax
pivot
anchoredPosition
sizeDelta
```

따라서 새로운 Responsive UI 시스템을 별도로 만들지 않는다.

기존 Variant + LayoutPatch 구조를 이용해 실제 화면비 대응을 구현한다.

```text
DisplayContext
      ↓
VariantCondition
      ↓
UIVariantResolver
      ↓
LayoutPatchSpec
      ↓
ResolvedUIScreen
```

---

# 3. 프로젝트 최종 목표

본 프로젝트의 핵심 질문은 다음과 같다.

> 다양한 디바이스 환경에서 동일한 UI Spec을 어떻게 일관된 규칙으로 해석하고, 화면비와 Safe Area에 따라 안정적으로 표현할 것인가?

최종 구조는 다음 방향을 목표로 한다.

```text
Authored Data
   │
   ▼
UIScreenSpec
   │
   ├───────────────┐
   │               │
   ▼               ▼
UIContext      DisplayContext
   │               │
   └───────┬───────┘
           ▼
     UIVariantResolver
           │
           ▼
     ResolvedUIScreen
           │
           ▼
     Patch / Composer
           │
           ▼
        Unity UI
```

그리고 개발/검증 도구까지 포함한다.

```text
Runtime
Editor Preview
Resolve Trace
EditMode Test
Representative Device Cases
README / Demo
```

---

# 4. 지원할 대표 화면 환경

초기 기준 해상도는 다음과 같이 둔다.

```text
Reference Resolution
1920 × 1080
16:9
```

최소 검증 대상:

| 분류 | 해상도 | 화면비 | 목적 |
|---|---:|---:|---|
| Standard | 1920 × 1080 | 16:9 | 기준 |
| Standard High | 2560 × 1440 | 16:9 | 동일 비율 스케일 |
| Wide Mobile | 2340 × 1080 | 19.5:9 | 와이드 대응 |
| Wide Mobile | 2400 × 1080 | 20:9 | 와이드 대응 |
| Tablet | 2048 × 1536 | 4:3 | 좁은 가로폭 대응 |

향후 필요 시 Portrait 환경을 별도 단계로 확장한다.

---

# 5. 범위

## 포함

- 기존 UI 아키텍처 감사 및 정리
- Display 환경 모델 도입
- UIContext / DisplayContext 책임 분리
- Runtime 환경 Provider 분리
- Resolver 입력 불변성 정리
- 화면비 분류
- 기존 Variant 시스템과 화면비 연동
- 기존 LayoutPatch를 통한 Responsive UI
- Safe Area 대응
- 대표 해상도 테스트
- Editor Device Preview
- Resolve Trace 개선
- EditMode Test
- README 및 포트폴리오용 구조 설명

## 제외

다음 항목은 이 계획의 핵심 범위에 포함하지 않는다.

- 캐릭터 이동/연출 시스템
- fade / move / scale / rotate command framework
- Timeline 대체 시스템
- VN progression
- Dialogue system
- Animation graph
- LipSync
- Expression system
- Spine Character Presentation

Spine은 모든 핵심 단계 완료 후 Optional milestone에서만 다룬다.

---

# 6. 개발 원칙

## 원칙 1 — Environment API는 경계에서만 읽는다

`Screen`, `Application.platform`, `Screen.safeArea` 등의 Unity API를 도메인 규칙 객체가 직접 읽지 않는다.

```text
Unity API
→ Provider
→ Context
→ Resolver
```

## 원칙 2 — Resolver는 입력을 변경하지 않는다

```text
Spec + Context
→ Resolved Result
```

원본 Spec의 배열, ScriptableObject 데이터, Runtime Context를 Resolve 과정에서 수정하지 않는다.

## 원칙 3 — Responsive 대응은 새 시스템이 아니라 Variant의 실제 사용 사례다

해상도 대응만을 위한 별도 프레임워크를 중복 작성하지 않는다.

기존:

```text
VariantCondition
UIVariantRule
LayoutPatchSpec
```

을 실제 화면비 대응 문제에 사용한다.

## 원칙 4 — 한 단계가 완료되기 전 다음 단계로 넘어가지 않는다

각 Milestone은 반드시 다음 순서를 따른다.

```text
구현
→ 정적 검토
→ EditMode/수동 테스트
→ 체크리스트 기록
→ 완료 판정
→ 다음 Milestone
```

테스트를 수행하지 못한 단계는 완료 처리하지 않는다.

## 원칙 5 — 포트폴리오에서 설명 가능한 복잡도만 유지한다

기능 수보다 구조의 명확성을 우선한다.

모든 추상화는 다음 질문에 답할 수 있어야 한다.

> 이 객체가 없으면 어떤 책임이 다시 섞이는가?

---

# 7. Milestone Overview

| 단계 | 목표 |
|---|---|
| M0 | 현재 구조 감사 및 Rebuild 기준선 확정 |
| M1 | DisplayContext 및 환경 Provider 도입 |
| M2 | Resolver 순수성 및 Context 경계 정리 |
| M3 | Aspect Ratio 기반 UI Variant 대응 |
| M4 | Safe Area 대응 |
| M5 | Device Preview / Resolve Trace / 테스트 도구 |
| M6 | 통합 검증 및 포트폴리오 마감 |
| M7 | Optional — Spine Character Presentation 연결 |

M0~M6가 본 프로젝트의 완료 조건이다.

M7은 M0~M6 완료 후에만 착수한다.

---

# 8. M0 — Current Architecture Audit

## 목표

현재 프로젝트를 실행 가능한 기준선으로 만들고, 유지/수정/제거할 구조를 확정한다.

## 작업

- `Core`, `Patcher`, `Editor`, `Keys`, `Legacy` 책임 조사
- Runtime 진입점 확인
- 실제 사용 중인 클래스와 죽은 코드 구분
- `Legacy` 의존 여부 조사
- `UIScreenSpec → Resolver → Resolved → Compose/Patch` 실제 호출 흐름 확인
- 현재 SampleScene 실행 여부 확인
- 현재 Editor Tool 실행 여부 확인
- 현재 VariantCondition의 Platform/Aspect 의존 위치 기록
- 현재 Resolver의 입력 변경 가능성 기록
- 기존 LayoutPatch 적용 경로 확인
- 테스트 Assembly 존재 여부 확인

## 산출물

```text
docs/M0-current-architecture.md
```

문서에는 최소 다음을 포함한다.

- 실제 Runtime call flow
- 각 주요 클래스 책임
- Keep / Refactor / Remove 후보
- 알려진 위험 요소
- M1 진입 조건

## 테스트 게이트

- Unity 프로젝트가 현재 버전에서 열린다.
- SampleScene의 기존 UI 흐름을 재현한다.
- 기존 Editor Window를 열 수 있다.
- 기존 화면 Resolve 결과를 최소 1회 확인한다.
- 현재 상태에서 Console compile error가 없어야 한다.

## 완료 조건

- [ ] 현재 call flow 문서화
- [ ] Legacy 의존 관계 확인
- [ ] 핵심 클래스 책임 분류
- [ ] 기존 동작 baseline 확보
- [ ] 다음 단계에서 변경할 파일 목록 확정
- [ ] 테스트 결과 기록

---

# 9. M1 — Display Environment Model

## 목표

Unity 환경 API 접근을 하나의 경계로 모으고 `DisplayContext`를 UI Resolver에 전달할 수 있도록 만든다.

## 예상 구조

```text
Assets/scripts/Display
├─ DisplayContext.cs
├─ DisplayPlatform.cs
├─ AspectRatioClass.cs
├─ IDisplayContextProvider.cs
└─ UnityDisplayContextProvider.cs
```

초기 `DisplayContext` 후보:

```text
Resolution
AspectRatio
SafeArea
Orientation
Platform
```

SafeArea 적용 자체는 M4에서 처리한다.
M1에서는 데이터 모델만 포함할 수 있다.

## 작업

- `DisplayContext` 정의
- Platform 표현 방식 확정
- Aspect ratio 계산 위치 확정
- Unity Runtime Provider 구현
- 테스트용 Context를 직접 만들 수 있도록 설계
- `Screen.width`, `Screen.height`, `Application.platform` 직접 접근 위치 제거 준비

## 테스트 게이트

EditMode에서 최소 다음 Context를 직접 생성/검증한다.

```text
1920×1080
2400×1080
2048×1536
```

Runtime Provider가 현재 Unity 화면 정보를 올바르게 Context로 변환하는지 확인한다.

## 완료 조건

- [ ] DisplayContext 생성
- [ ] Runtime Provider 생성
- [ ] Unity API 접근 경계 확정
- [ ] 수동 Context 생성 가능
- [ ] 기본 단위 테스트 작성
- [ ] 테스트 통과

---

# 10. M2 — Resolver Boundary & Purity

## 목표

Resolver가 외부 환경을 직접 읽지 않고, 입력 Spec을 변경하지 않는 구조로 정리한다.

## 목표 호출 형태

예시:

```text
Resolve(
    UIScreenSpec spec,
    UIContext uiContext,
    DisplayContext displayContext)
```

또는 두 Context를 묶는 별도 PresentationContext 도입 여부를 이 단계에서 결정한다.

## 작업

- `VariantCondition.Matches`가 Context만 사용하도록 변경
- `Screen.*` 직접 참조 제거
- `Application.platform` 직접 참조 제거
- Resolver 내부 원본 배열 정렬 제거
- Resolver가 원본 `UIScreenSpec`을 변경하지 않는지 검증
- Forced override / priority 정책 회귀 테스트
- Trace가 Display 입력을 기록하도록 개선 준비

## 테스트 게이트

- 같은 Spec + 같은 Context를 여러 번 Resolve해도 동일 결과
- Resolve 전후 `spec.variants` 순서 동일
- Runtime Unity API 없이 EditMode에서 화면비 조건 판정 가능
- 기존 Theme/Locale/Experiment 조건 회귀 없음

## 완료 조건

- [ ] Environment direct read 제거
- [ ] Resolver input mutation 제거
- [ ] 반복 Resolve 결과 안정성 확인
- [ ] 기존 Variant 기능 회귀 테스트
- [ ] 테스트 통과

---

# 11. M3 — Responsive UI via Existing Variant System

## 목표

기존 Variant + LayoutPatch 구조가 실제 다중 화면비 대응을 수행하도록 만든다.

## 화면 분류

초기 후보:

```text
Tablet / Compact Landscape
Standard 16:9
Wide
UltraWide
```

정확한 임계값은 테스트 후 확정한다.

분류 이름 자체보다 중요한 것은 숫자 비교 규칙이 한 곳에 존재하는 것이다.

## 작업

- Aspect Ratio classification 규칙 확정
- VariantCondition에서 명시적으로 DisplayContext 사용
- 샘플 UI 하나를 대표 화면으로 선정
- Standard layout 작성
- Wide layout patch 작성
- Tablet layout patch 작성
- 기존 `LayoutPatchSpec` 재사용
- 필요할 경우 LayoutPatch의 부족한 최소 기능만 확장

## 대표 검증 UI

```text
┌──────────────────────────────┐
│                              │
│           Content            │
│                              │
│                              │
│   [ Primary UI / Controls ]  │
└──────────────────────────────┘
```

검증 항목:

- 주요 UI가 화면 밖으로 나가지 않는가
- 기준 정렬 관계가 유지되는가
- 지나친 stretch가 발생하지 않는가
- Tablet에서 과도한 가로 여백/겹침이 없는가
- Wide에서 불필요하게 중앙 콘텐츠가 퍼지지 않는가

## 테스트 게이트

필수 해상도:

```text
1920 × 1080
2560 × 1440
2340 × 1080
2400 × 1080
2048 × 1536
```

각 해상도별 Expected Variant와 실제 Variant가 일치해야 한다.

## 완료 조건

- [ ] 화면비 분류 규칙 확정
- [ ] Standard 대응
- [ ] Wide 대응
- [ ] Tablet 대응
- [ ] 기존 LayoutPatch 재사용
- [ ] 대표 해상도 전체 검증
- [ ] 테스트 통과

---

# 12. M4 — Safe Area

## 목표

노치/홈 인디케이터 등의 영역을 UI 정책으로 처리한다.

## 핵심 원칙

Safe Area는 모든 UI를 무조건 축소시키는 시스템으로 만들지 않는다.

UI 요소별 정책을 구분한다.

```text
Background     → Full Bleed
Decorative UI  → Full Bleed 가능
Main Controls  → Safe Area 내부
Navigation     → Safe Area 내부
Dialogue/Info  → 정책에 따라 선택
```

## 작업

- DisplayContext.SafeArea 사용
- SafeArea normalization 방식 확정
- SafeArea root/container 설계
- FullBleed / SafeContent 책임 분리
- Editor에서 mock SafeArea 입력 가능하도록 구성

## 테스트 게이트

- 좌우 notch mock
- 상단 notch mock
- 하단 home indicator mock
- SafeArea가 없는 화면

검증:

- 중요 버튼 침범 없음
- 배경은 불필요하게 줄지 않음
- 기존 Aspect Variant와 충돌 없음

## 완료 조건

- [ ] SafeArea 정책 문서화
- [ ] SafeContent 적용
- [ ] FullBleed 분리
- [ ] Mock SafeArea 검증
- [ ] Aspect + SafeArea 조합 테스트
- [ ] 테스트 통과

---

# 13. M5 — Device Preview, Trace & Verification Tools

## 목표

환경 대응을 코드로만 설명하지 않고 Editor에서 즉시 관찰하고 검증할 수 있도록 한다.

## Device Preview

최소 preset:

```text
[16:9 Desktop]
[20:9 Mobile]
[19.5:9 Mobile]
[4:3 Tablet]

[Safe Area Off]
[Safe Area Preset]
```

Preview는 실제 `Screen` 상태와 Resolver 입력을 분리해서 조작할 수 있어야 한다.

## Resolve Trace

최소 출력 예:

```text
Resolution       : 2400 × 1080
Aspect Ratio     : 2.2222
Aspect Class     : Wide
Platform         : Mobile
Safe Area        : Applied

Screen           : Sample
Matched Variant  : Sample_Wide
Layout           : Sample_Wide_Layout
Theme            : Default
```

기존 `UIVariantResolver`의 Trace 구조를 확장한다.

## 자동 테스트

최소 테스트 유형:

```text
DisplayContextTests
VariantConditionTests
UIVariantResolverTests
LayoutSelectionTests
SafeAreaPolicyTests
```

## 완료 조건

- [ ] Device preset 제공
- [ ] Resolver preview 가능
- [ ] Display trace 출력
- [ ] 주요 Resolver 단위 테스트
- [ ] 대표 Device Matrix 테스트
- [ ] 테스트 통과

---

# 14. M6 — Integration & Portfolio Finish

## 목표

기술 데모가 아니라 타인이 읽고 실행하고 이해할 수 있는 포트폴리오 프로젝트로 마무리한다.

## 코드 정리

- Legacy 최종 처리
- namespace / folder 책임 정리
- 죽은 코드 제거
- public API 최소화
- 불필요한 추상화 제거
- 주석 정리

대규모 폴더 이동은 기능 검증 이후에만 수행한다.

최종 구조 후보:

```text
Assets/scripts
├─ Display
├─ UI
│  ├─ Core
│  ├─ Patcher
│  └─ Editor
└─ Legacy        # 필요 시 제거
```

향후 Character가 들어올 경우:

```text
Assets/scripts
├─ Display
├─ UI
└─ Character
```

## README

최소 포함 내용:

1. Problem
2. Design Goal
3. Architecture
4. DisplayContext
5. Spec → Resolve → Apply
6. Responsive Variant
7. SafeArea Policy
8. Device Preview
9. Resolver Trace
10. Tests
11. Trade-offs
12. Future / Optional Spine Integration

## 데모 자료

- 16:9 화면
- 20:9 화면
- 4:3 화면
- SafeArea ON/OFF 비교
- Device Preview 사용 장면
- Resolve Trace 출력

## 최종 검증 게이트

- Fresh clone 가능
- Unity project open 가능
- Console compile error 없음
- Sample demo 실행 가능
- M3 device matrix 통과
- M4 safe area matrix 통과
- EditMode tests 전체 통과
- README만 보고 구조 파악 가능

## 완료 조건

- [ ] 코드 정리
- [ ] Legacy 처리
- [ ] 전체 테스트 통과
- [ ] Demo scene 완성
- [ ] README 완성
- [ ] 포트폴리오 스크린샷/GIF 준비
- [ ] M0~M6 완료 선언

---

# 15. M7 — Optional Spine Integration

> M0~M6가 모두 완료되기 전에는 착수하지 않는다.

## 목적

UI 환경 모델이 UI에만 종속되지 않는 공통 Presentation 입력으로 사용 가능한지 검증한다.

Spine 자체를 깊게 추상화하는 것이 목적이 아니다.

## 최소 범위

```text
Character
├─ CharacterView
├─ CharacterLayoutSpec
├─ CharacterLayoutResolver
├─ ResolvedCharacterLayout
└─ Spine
   └─ SpineCharacterView
```

초기 기능:

```text
Play(animation)
SetVisible(bool)
```

정도로 제한한다.

캐릭터 위치도 직접 좌표보다 의미 기반 Anchor를 사용한다.

```text
CharacterAnchor.Left
CharacterAnchor.Center
CharacterAnchor.Right
```

목표 흐름:

```text
CharacterLayoutSpec
        ↓
DisplayContext
        ↓
CharacterLayoutResolver
        ↓
ResolvedCharacterLayout
        ↓
SpineCharacterView
```

## 제외

- LipSync
- Expression graph
- Advanced track policy
- Timeline replacement
- Command framework
- Cinematic transition framework

## 완료 기준

- UI와 Character가 동일 DisplayContext 사용
- 16:9 / Wide / Tablet에서 framing 유지
- UI와 캐릭터 주요 영역 충돌 없음

---

# 16. 단계 진행 규칙

각 단계 시작 시 해당 문서를 별도로 작성한다.

```text
PLAN.md

docs/
├─ M0-current-architecture.md
├─ M1-display-context.md
├─ M2-resolver-boundary.md
├─ M3-responsive-ui.md
├─ M4-safe-area.md
├─ M5-preview-and-tests.md
├─ M6-portfolio-finish.md
└─ M7-spine-optional.md
```

각 세부 문서에는 반드시 다음 구조를 둔다.

```text
Goal
Current State
Decisions
Implementation Tasks
Files to Change
Tests
Manual Verification
Checklist
Completion Record
```

---

# 17. 완료 처리 규칙

Milestone은 코드 작성만으로 완료되지 않는다.

완료 순서:

```text
1. 구현
2. 코드 검토
3. 자동 테스트
4. 수동 검증
5. 체크리스트 기록
6. 발견된 문제 수정
7. 재검증
8. Completion Record 작성
9. 다음 단계 시작
```

실행 환경 문제로 테스트를 할 수 없는 경우:

```text
BLOCKED
```

로 기록한다.

테스트하지 않은 상태에서 `DONE`으로 표시하지 않는다.

---

# 18. 프로젝트 완료 정의

본 프로젝트의 Core 완료는 `M6`이다.

완료 시 다음 문장이 사실이어야 한다.

> UIPresentationFlow는 UI가 `Screen`과 같은 전역 Unity 환경에 직접 의존하지 않고, 명시적인 DisplayContext를 통해 디바이스 환경을 해석한다. 동일한 UIScreenSpec은 화면비, 플랫폼, Safe Area에 따라 Resolver에서 결정적으로 다른 Presentation 결과를 만들며, 이 결과는 Editor Preview와 자동 테스트를 통해 검증할 수 있다.

이 상태가 만들어지면 기존 프로젝트는 단순한 UI Router/Factory 예제가 아니라 다음 성격을 갖게 된다.

> **Data-driven Adaptive UI Presentation Framework for Unity**

Spine은 이 구조가 Character Presentation에도 확장 가능하다는 것을 보여주는 선택적 후속 검증으로 취급한다.

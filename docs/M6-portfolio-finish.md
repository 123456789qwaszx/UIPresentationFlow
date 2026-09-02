# M6 — Integration, Cleanup & Portfolio Finish

## Goal

M0~M5에서 검증된 기능을 하나의 읽을 수 있는 프로젝트로 정리하고,
fresh clone한 타인이 구조와 실행 방법을 이해할 수 있는 상태로 만든다.

M6는 새로운 핵심 기능을 추가하는 단계가 아니다.

> **검증된 설계를 정리하고 증거를 남기는 단계다.**

---

# 1. Definition of Core Done

M6 완료 시 다음이 사실이어야 한다.

> UIPresentationFlow는 Unity 전역 Display API를 UI 규칙 내부에서 직접 조회하지 않고, 명시적인 DisplayContext를 통해 화면 환경을 판정한다. UIScreenSpec은 UIContext와 DisplayContext를 입력으로 결정적인 ResolvedUIScreen을 만들며, 기존 Variant/LayoutPatch 시스템으로 화면비별 UI를 구성한다. Safe Area 정책은 FullBleed와 SafeContent를 구분하고, Editor Preview와 자동 테스트에서 결과를 재현할 수 있다.

이 문장이 거짓이면 M6를 완료하지 않는다.

---

# 2. Cleanup Order

정리 순서는 중요하다.

```text
1. 전체 tests green
2. Demo visual baseline capture
3. dead code usage search
4. Legacy cleanup
5. folder/namespace cleanup
6. 다시 compile/test
7. README
8. final fresh clone test
```

구조 이동을 먼저 하지 않는다.

---

# 3. Legacy Cleanup

M0에서 분류한 결과에 따라 처리한다.

현재 `Legacy/UIBootStrap.cs`, `UITestDriver.cs`는 전체 주석 상태로 확인됐다.

가능한 처리:

## Remove

사용처 없고 history로 충분하면 삭제.

## Archive not recommended

`Legacy/` 안에 죽은 코드를 계속 두는 것은 포트폴리오 readability를 낮춘다.

## Rewrite into real Composition Root

Bootstrap 개념 자체가 필요하면 새 이름/새 구현으로 Core 바깥에 둔다.

예:

```text
UIRuntimeInstaller
UIPresentationBootstrap
```

“과거 commented code를 살리는 것”이 아니라 현재 구조로 다시 작성.

---

# 4. UIRuntimeRouter Static Holder

현재:

```text
UIRuntimeRouter.Router { get; set; }
```

M6에서 필요성을 재평가.

질문:

- 누가 실제로 접근하는가?
- DI/composition root에서 직접 reference 전달 가능하지 않은가?
- Editor demo 편의를 위해서만 필요한가?

사용처가 명확하면 유지.
없으면 제거 후보.

글로벌 접근을 없애는 것 자체가 목적은 아니다.

---

# 5. `GetWidgetDirect` Bypass

M0 usage audit 결과에 따라:

- 사용처 없음 → 제거
- 특정 legacy rig만 사용 → 별도 compatibility component로 격리
- 정식 필요 → API 이름/문서 명확화

Responsive framework의 canonical path는:

```text
WidgetSpec.nameTag
→ WidgetHandle
→ LayoutPatch
```

로 유지한다.

---

# 6. Folder Structure

대규모 이동은 M6에서만 고려.

권장 최소 구조:

```text
Assets/scripts
├─ Display
│  ├─ DisplayContext
│  ├─ Provider
│  └─ Classification
│
└─ UI
   ├─ Core
   ├─ Keys
   ├─ Patcher
   └─ Editor
```

Optional M7 이후:

```text
Assets/scripts
├─ Display
├─ UI
└─ Character
```

`Presentation/` wrapper folder는 실제로 UI+Character가 공존하기 전에는 꼭 필요하지 않다.

---

# 7. Namespace

현재 코드가 global namespace 중심이다.

M6에서 namespace 도입은 선택 사항.

가능:

```text
Ked.Presentation.Display
Ked.Presentation.UI
Ked.Presentation.UI.Editor
```

하지만 파일 전체를 이동하면서 namespace까지 한 번에 바꾸면 diff가 커진다.

포트폴리오 readability와 package화 계획이 있다면 도입.

그렇지 않으면 기능과 문서가 더 중요하다.

결정 기준:

```text
UPM package로 분리할 계획 있음 → 도입 가치 높음
단일 demo repository 유지 → 필수 아님
```

---

# 8. Assembly Definitions

M1/M5 test 구성 과정에서 필요성이 드러났다면 M6에서 정식 분리.

후보:

```text
UIPresentationFlow.Runtime.asmdef
UIPresentationFlow.Editor.asmdef
UIPresentationFlow.Tests.asmdef
```

의존:

```text
Editor → Runtime
Tests → Runtime
Runtime ↛ Editor
```

장점:

- 컴파일 경계
- package readiness
- tests 구조 명확

단점:

- 기존 Assembly-CSharp refs migration

기능 검증 후에만 수행.

---

# 9. README Structure

README는 코드 목록이 아니라 문제-설계-검증 순서로 작성.

## 1. What Problem Does This Solve?

예:

```text
동일 UI를 16:9, 20:9, 4:3에서 유지할 때
단순 CanvasScaler만으로는 composition이 깨질 수 있다.
```

## 2. Design Goals

- data-driven
- explicit environment
- deterministic resolver
- reusable authored spec
- testable without actual device

## 3. Architecture

```text
UIScreenSpec
 + UIContext
 + DisplayContext
      ↓
UIVariantResolver
      ↓
ResolvedUIScreen
      ↓
Compose / Patch
```

## 4. DisplayContext

왜 Screen을 직접 읽지 않는지.

## 5. Responsive Variant

CanvasScaler와 LayoutPatch 책임 차이.

## 6. Safe Area

FullBleed / SafeContent.

## 7. Device Preview

GIF/shot.

## 8. Resolve Trace

실제 example.

## 9. Tests

matrix.

## 10. Trade-offs

- aspect class threshold는 UI policy
- not full constraint solver
- not device emulator
- not animation framework

## 11. Optional Spine

Core 완료 후 extension.

---

# 10. Portfolio Evidence

최소 이미지:

```text
01_architecture.png
02_16x9.png
03_20x9.png
04_4x3.png
05_safearea.png
06_preview_trace.png
```

GIF 후보:

```text
device_preview_switch.gif
```

한 GIF에서:

```text
16:9 → 20:9 → 4:3 → SafeArea
```

전환이 보이면 강력하다.

---

# 11. Code Comments

주석 정리 원칙:

- 코드가 말하는 것을 반복하지 않음
- 왜 그런 정책인지 기록
- old experiment emoji/temporary comments 제거
- Korean/English 혼용은 프로젝트 기준 선택

README가 영어라면 code comment도 영어 통일을 고려.

사용자 학습용 repository 성격을 유지하고 싶다면 한국어도 가능.
일관성이 더 중요.

---

# 12. Validation / Authoring Errors

최종 Editor validation에서 최소:

```text
duplicate ScreenKey
missing spec asset
duplicate route
route → undefined screen
duplicate variantId
invalid aspect range
duplicate responsive nameTag (가능 범위)
missing layout patch target (preview 시)
```

모든 것을 pre-build validator로 만들 필요는 없다.

현재 Catalog `ValidateAll`을 적당히 확장하는 정도.

---

# 13. Fresh Clone Gate

최종에서 반드시 새로운 위치로 clone 또는 clean checkout해 검증.

절차:

```text
1. dev checkout
2. Unity 6000.2.7f2 open
3. package restore/import
4. compile
5. EditMode tests
6. AdaptiveUIDemo open
7. Play
8. Preview Window open
9. device matrix
```

로컬 Library/cache에 의존하는 설정이 없어야 한다.

---

# 14. Git Hygiene

M6에서 확인:

- `.idea`가 이미 tracked 되어 있음
- Unity-generated / IDE-generated 파일 정책 검토
- Library/Temp/Logs ignored
- demo assets만 유지
- 불필요한 TextMeshPro samples 여부는 무리하게 제거하지 않음

`.idea` tracked 상태를 정리할지는 별도 commit으로 하는 편이 history가 깔끔하다.

---

# 15. Performance Sanity

이 프로젝트는 high-frequency UI loop가 아니다.

그래도 확인:

- Resolve 때 원본 mutation 없음
- unnecessary per-frame resolve 없음
- SafeArea apply는 screen/context change 시
- preview/editor allocations은 runtime concern 아님
- `UIComposer` BFS는 screen create 시

Profiler optimization을 과도하게 하지 않는다.

---

# 16. Accessibility / Localization Boundary

현재 `UIContext.LocaleId`가 있지만 실제 localization framework가 핵심이 아니다.

M6 README에서:

```text
Locale is a variant input supported by the resolver,
but full localization is out of scope.
```

정도로 설명.

접근성/폰트 dynamic size도 Future work.

---

# 17. Final Automated Gate

모든 suites:

```text
DisplayContextTests
DisplayLayoutClassifierTests
VariantConditionTests
UIVariantResolverTests
LayoutSelectionTests
SafeAreaTests
LayoutPatchIntegrationTests
```

`0 failed`.

warning도 test log에서 의미 있는 것만 남긴다.

---

# 18. Final Manual Device Matrix

| Resolution | Class | SafeArea | Result |
|---|---|---|---|
| 1920×1080 | Standard | None | |
| 2560×1440 | Standard | None | |
| 2340×1080 | Wide | None | |
| 2400×1080 | Wide | Side | |
| 2048×1536 | Compact | None | |
| 2048×1536 | Compact | Combined | |

각 행:

- visual PASS/FAIL
- selected layout
- screenshot path

기록.

---

# 19. Implementation Tasks

## M6.1 Dead code audit 재실행

## M6.2 Legacy 처리

## M6.3 Optional folder/asmdef cleanup

## M6.4 Validation cleanup

## M6.5 Demo scene polish

UI art 자체에 과도한 시간을 쓰지 않는다.
layout 차이가 읽히는 정도.

## M6.6 README 작성

## M6.7 diagrams/screenshots/GIF

## M6.8 fresh clone test

## M6.9 Core completion record

---

# 20. Do Not Add

M6에서 금지:

- 새로운 animation command system
- navigation history framework
- Addressables
- localization package
- DI framework
- reactive framework
- Spine
- performance micro-optimization

마감 단계에서 scope가 다시 커지지 않게 한다.

---

# 21. Completion Checklist

- [ ] all automated tests green
- [ ] Legacy 결정 완료
- [ ] dead code 제거/문서화
- [ ] static holder 결정
- [ ] direct widget bypass 결정
- [ ] folder 구조 정리
- [ ] asmdef 결정
- [ ] validation 정리
- [ ] Demo scene 완성
- [ ] 6-case final matrix PASS
- [ ] README
- [ ] architecture diagram
- [ ] 16:9 screenshot
- [ ] 20:9 screenshot
- [ ] 4:3 screenshot
- [ ] SafeArea screenshot
- [ ] Preview/Trace screenshot
- [ ] GIF 선택
- [ ] fresh clone compile
- [ ] fresh clone tests
- [ ] fresh clone demo
- [ ] M0~M6 Core DONE 선언

---

# 22. Completion Record

```text
Status: NOT STARTED

Core tests:
- total:
- passed:
- failed:

Legacy final state:
Runtime composition root:
Static globals:
Assembly layout:
Namespace:

Final device matrix:
- 1920×1080:
- 2560×1440:
- 2340×1080:
- 2400×1080 + safe:
- 2048×1536:
- 2048×1536 + safe:

README:
Screenshots:
GIF:

Fresh clone:
- compile:
- tests:
- demo:
- preview:

Core completion: NO

M7 may start: NO
```

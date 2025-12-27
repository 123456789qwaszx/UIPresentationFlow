# CPS.UI 프로토콜 — Route & 액션 키 규약

이 문서는 **CPS.UI에서 사용하는 문자열 기반 프로토콜(route)** 를 정의한다.

목표:

- UI 스펙 / 디자이너 / 툴이 **단순하고 안정적인 언어(문자열 route)** 로 소통하게 만들고
- 런타임 시스템은 **타입이 있는 개념**(`UIActionKey`, `ScreenKey`)만 바라보게 하며
- 중구난방으로 지어지는 이름들을 막고, 규칙 있는 체계를 유지하는 것

`WidgetSpec.onClickRoute`, `UIActionKeyRegistry`, `UIRouteKeyRegistry` 를 건드린다면  
이미 이 프로토콜을 사용하고 있는 것이다.

---

## 1. 기본 개념

1. **Route는 프로토콜 키(문자열)** 다.

   예:

   - `"nav/home"`
   - `"nav/shop/special-offer"`
   - `"hud/gold"`
   - `"dialog/next"`

2. **UIActionKey는 route의 런타임 표현** 이다.

   ```csharp
   public readonly struct UIActionKey
   {
       public string Value { get; }   // 원래 route 문자열
       // 선택적으로: Kind, TargetScreen 등의 메타데이터도 가질 수 있음
   }
ScreenKey는 “화면”에만 쓰는 키 다.

csharp
코드 복사
public enum ScreenKey
{
    Home,
    Shop,
    // ...
}
route 중 일부만 ScreenKey로 매핑된다. (네비게이션용)

UI(스펙/버튼)는 ScreenKey를 몰라도 된다.

스펙과 위젯은 오직 route 문자열만 안다.

코어 시스템이 route를 해석해서:

UIActionKeyRegistry → UIActionKey

UIRouteKeyRegistry → ScreenKey (해당되는 경우에만)

이렇게 데이터/툴 레이어와 도메인/런타임 레이어를 분리한다.

2. Route 포맷
기본 형식:

text
코드 복사
<prefix>/<name>[/<detail>...]
prefix = 액션의 종류 (네비게이션, HUD, 대화, 디버그 등)

name = 주요 대상

detail = 선택적인 세부 정보(변형, 진입 경로, 탭 등)

2.1. 네이밍 규칙
전부 소문자

구분자는 /

한 단어 안에서 여러 단어가 필요하면 - 사용

✅ 좋은 예:

nav/home

nav/shop/special-offer

hud/gold

dialog/next

dialog/choice/yes

debug/toggle-fps

❌ 피해야 할 예:

OpenHome (camelCase, 동사 포함)

NAV_HOME (enum 스타일)

home (prefix 없음, 의미 모호)

shopSpecialEntry (대소문자/구분자 규칙 없음)

Route는 유저에게 보이는 문구가 아니라, 시스템이 이해하는 식별자다.

3. 예약된 prefix
다음 prefix들은 CPS.UI에서 예약된 의미를 가진다.

추가 prefix를 만들 수는 있지만,
이미 정의된 prefix의 의미를 바꿔서는 안 된다.

3.1. nav/ — 네비게이션 (화면 이동)
화면 전환/열기에 사용하는 prefix.

예:

nav/home

nav/shop

nav/shop/special-offer

nav/result/from-battle

동작:

UIScreenCatalog 의 UIRouteEntry 에서

route → ScreenKey 매핑을 정의한다.

UIRouteKeyRegistry + RouteKeyResolver 가 이 매핑을 사용.

UIRouter 는 ScreenKey 를 기준으로 UIScreen을 Resolve & Create 한다.

가이드:

nav/<screen> 은 가능하면 ScreenKey와 1:1로 대응시키는 것이 좋다.

<detail> 세그먼트는 **목적지 화면이 아니라 “진입 컨텍스트”**를 위한 용도:

예: nav/shop/from-home, nav/shop/from-result

3.2. hud/ — HUD / 오버레이 액션
HUD(상단 바, 상태 표시 UI 등) 와 관련된 액션에 사용.

예:

hud/gold

hud/hp

hud/gem

사용 예시:

골드 텍스트를 하이라이트/펄스시키는 연출

특정 HUD 섹션을 열거나 갱신하도록 요청

이 prefix의 route는 보통 ScreenKey로 매핑되지 않는다.

3.3. dialog/ — 대화 / 비주얼 노벨
대화 시스템(CPS.Dialogue 등)과 연결되는 액션.

예:

dialog/next

dialog/skip

dialog/auto/toggle

dialog/choice/yes

dialog/choice/no

구체적인 의미는 대화 시스템에서 정의한다.

예를 들어:

dialog/next → 현재 노드/스텝 진행

dialog/skip → 스킵 모드 토글

dialog/choice/* → 분기 선택

3.4. debug/ — 디버그 / 툴
개발·테스트용 UI에 사용하는 prefix.

예:

debug/toggle-fps

debug/reload-scene

debug/teleport/home

이 route들은 실제 게임 플레이 로직에 의존하면 안 된다.
환경·상황에 따라 자유롭게 바뀔 수 있는 디버그용 프로토콜이다.

3.5. system/ (선택) — 시스템 레벨 액션
게임/앱 전체에 공통되는 시스템 단위 행동을 표현할 때 사용.

예:

system/quit

system/open-settings

system/open-credits

이 route가 ScreenKey로 매핑될지,
아니면 바로 특정 시스템 동작을 호출할지는 프로젝트 설계에 따라 결정한다.

4. Route → ScreenKey 매핑
네비게이션용 route는 UIScreenCatalog 의 UIRouteEntry로 설정한다:

csharp
코드 복사
[Serializable]
public struct UIRouteEntry
{
    public string route;   // 예: "nav/home"
    public ScreenKey key;  // 예: ScreenKey.Home
}
4.1. 규칙
네비게이션 route는 반드시 nav/로 시작해야 한다.

여러 개의 route가 하나의 ScreenKey로 매핑될 수 있다.

예:

text
코드 복사
"nav/shop"               → ScreenKey.Shop
"nav/shop/special-offer" → ScreenKey.Shop
"nav/shop/from-result"   → ScreenKey.Shop
각 ScreenKey마다 대표 route를 하나 정해두는 것을 추천:

예: nav/home, nav/shop, nav/result 등

4.2. Fallback (선택)
UIRouteKeyRegistry는 필요하다면 다음과 같은 fallback을 둘 수 있다:

route 문자열이 ScreenKey.ToString() 과 같고 (예: "Home")

그 문자열에 대한 UIRouteEntry가 별도로 정의되어 있지 않다면

"Home"을 ScreenKey.Home의 임시/레거시 route로 취급

이는 마이그레이션 / 임시 테스트 편의를 위한 기능일 뿐이다.
새롭게 추가되는 route는 항상 nav/<name> 포맷을 쓰는 것을 원칙으로 한다.

5. UIActionKey 레지스트리
UIActionKeyRegistry 는 route 문자열을 UIActionKey로 변환하고 캐싱하는 중심 지점이다.

대표적인 구현 예:

csharp
코드 복사
public static class UIActionKeyRegistry
{
    static readonly Dictionary<string, UIActionKey> _cache = new();

    public static UIActionKey Get(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return UIActionKey.None;

        raw = raw.Trim();

        if (_cache.TryGetValue(raw, out var key))
            return key;

        key = new UIActionKey(raw);
        _cache[raw] = key;
        return key;
    }
}
그리고 공용으로 자주 쓰는 route는 상수로 빼서 관리할 수 있다:

csharp
코드 복사
public static class UIActionKeys
{
    public static readonly UIActionKey OpenHome =
        UIActionKeyRegistry.Get("nav/home");

    public static readonly UIActionKey OpenShop =
        UIActionKeyRegistry.Get("nav/shop");

    public static readonly UIActionKey HudGold =
        UIActionKeyRegistry.Get("hud/gold");
}
가이드라인:

여러 곳에서 공유되는 중요한 route는 상수로 이름을 붙인다.

특정 화면에서만 쓰이는 1회성 route는
스펙 안에서 문자열로만 유지해도 괜찮다.

6. 스펙에서의 사용 방식
WidgetSpec.onClickRoute 는 항상 route 문자열이다:

csharp
코드 복사
public sealed class WidgetSpec
{
    public string onClickRoute;
    // ...
}
예:

네비게이션 버튼:

text
코드 복사
onClickRoute = "nav/home"
HUD 관련 버튼/트리거:

text
코드 복사
onClickRoute = "hud/gold"
컴포즈/바인드 시점에는 다음 흐름을 따른다:

onClickRoute → UIActionKeyRegistry.Get(route) → UIActionKey

여러 개의 IUiActionBinder 구현체가 이 UIActionKey를 받아 판단:

RouteActionBinder:

route가 nav/* 이고 ScreenKey로 매핑된다면

버튼의 onClick을 UIRouter.Navigate(new UIRequest(actionKey))에 연결

그 밖의 바인더:

HUD용, 대화용, 디버그용 등으로 확장 가능

즉, UI 스펙은 route만 알고, route → 실제 행동은 바인더들이 해석한다.

7. Do & Don’t
Do
✅ 명확한 prefix 사용:

nav/, hud/, dialog/, debug/, system/ …

✅ route를 짧지만 의미 있게 짓기

✅ route를 공용 프로토콜로 생각하기

막 바꾸면 스펙·툴이 깨지는 API라는 인식을 갖기

✅ 공용 route는 중앙에서 관리:

네비게이션: UIScreenCatalog.routes

코드용: UIActionKeys

Don’t
❌ route 문자열에 복잡한 로직/파라미터를 다 때려넣지 말 것

예: nav/shop?tab=sale&source=result 같은 스타일은 피하기

❌ 유저에게 보이는 문구를 route로 쓰지 말 것

라벨은 언제든 바뀔 수 있지만, route는 오래 가야 한다.

❌ prefix 규칙을 섞어 쓰지 말 것

home, goHome, HomeScreen → nav/home 같은 통일된 패턴으로

❌ ScreenKey.ToString()에 의존해서 프로토콜을 만들지 말 것

정말 필요할 때만 fallback 용도로 사용

8. 프로토콜 확장하기
새 prefix가 필요할 때는 다음 순서를 따른다:

먼저 기존 prefix에 넣을 수 있는지 생각해본다.

화면 전환인가? → nav/

HUD 관련인가? → hud/

대화 관련인가? → dialog/

정말로 새로운 영역이라면:

새 prefix를 정의 (menu/, inventory/, card/ 등)

이 문서에:

prefix의 의미

자주 쓰일 예시

ScreenKey와의 관계 (매핑 여부)
를 적어둔다.

목표는:

prefix 개수는 적고

의미는 분명하며

시간이 지나도 안정적으로 유지되는 것

9. 버전 / 안정성
현재 이 프로토콜은:

“실험 중이지만 수렴하고 있는 상태” 로 간주한다.

실제 프로젝트에 써도 되지만,
아키텍처가 바뀌면 함께 조정될 수 있다.

프로토콜이 충분히 안정되면:

prefix와 주요 route 이름들은 공개 API로 취급하고,

변경 시에는:

가능한 한 하위 호환을 유지하거나

명시적인 마이그레이션(구 → 신 매핑 레이어)을 거쳐야 한다.

그 전까지는:

이름을 다듬는 건 괜찮지만

일관성이 없는 1회성 패턴을 마구 늘리는 일은 피하는 것을 원칙으로 한다.
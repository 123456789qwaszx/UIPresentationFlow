 CPS.UI Protocol — Route & Action Key Conventions

This document defines the **string-based protocol** used by CPS.UI for UI actions and navigation.

The goal is to:

- Keep UI specs, designers, and tools speaking a **simple, stable language** (routes)
- Let runtime systems work with **typed concepts** (`UIActionKey`, `ScreenKey`)
- Avoid ad-hoc, one-off naming that slowly turns into chaos

If you touch `WidgetSpec.onClickRoute`, `UIActionKeyRegistry`, or `UIRouteKeyRegistry`,  
you are working with this protocol.

---

## 1. Core Ideas

1. **Routes are protocol keys (strings)**  
   Examples:
   - `"nav/home"`
   - `"nav/shop/special-offer"`
   - `"hud/gold"`
   - `"dialog/next"`

2. **UIActionKey is the runtime representation of a route**

   ```csharp
   public readonly struct UIActionKey
   {
       public string Value { get; }          // original route string
       // Optional: Kind, TargetScreen, etc.
   }
ScreenKey is for screens only

csharp
코드 복사
public enum ScreenKey
{
    Home,
    Shop,
    // ...
}
Some routes map to a ScreenKey (navigation), most don’t.

UI never needs to know ScreenKey

Specs & widgets only see routes (strings).

Core systems interpret routes:

UIActionKeyRegistry → UIActionKey

UIRouteKeyRegistry → ScreenKey (if applicable)

2. Route Format
General rule:

text
코드 복사
<prefix>/<name>[/<detail>...]
prefix = category of action (navigation, HUD, dialogue, etc.)

name = primary target

detail = optional qualifiers (variant, source, tab, etc.)

2.1. Naming Rules
All lowercase

Segments separated by /

Use - for word breaks inside a segment

✅ Good:

nav/home

nav/shop/special-offer

hud/gold

dialog/next

dialog/choice/yes

debug/toggle-fps

❌ Avoid:

OpenHome (camelCase, verbs embedded)

NAV_HOME (enum-style)

home (no category, ambiguous)

shopSpecialEntry (mixed casing, no clear segments)

Routes are identifiers, not user-facing labels.

3. Reserved Prefixes
These prefixes are reserved for CPS.UI core usage.

You can extend with more, but you should not change the meaning of existing ones.

3.1. nav/ — Navigation
Used for actions that open / switch screens.

Examples:

nav/home

nav/shop

nav/shop/special-offer

nav/result/from-battle

Behavior:

UIRouteEntry in UIScreenCatalog defines:

route → ScreenKey

UIRouteKeyRegistry + RouteKeyResolver use this mapping.

UIRouter uses ScreenKey to resolve and create the target UIScreen.

Guideline:

nav/<screen> should map 1:1 to a ScreenKey whenever possible.

Additional segments are allowed for entry context, not for target screen identity:

e.g. nav/shop/from-home, nav/shop/from-result

3.2. hud/ — HUD / Overlay Actions
Used for HUD-related actions (status bar, top overlay, etc).

Examples:

hud/gold

hud/hp

hud/gem

Typical uses:

Trigger small presentation effects (pulse, highlight, pop-up)

Request the HUD system to show/update certain regions

These routes usually do not map to ScreenKey.

3.3. dialog/ — Dialogue / VN
Used for dialogue system actions (if CPS.Dialogue is integrated).

Examples:

dialog/next

dialog/skip

dialog/auto/toggle

dialog/choice/yes

dialog/choice/no

Semantics are defined by the dialogue system:

dialog/next → advance current node/step

dialog/skip → toggle skipping mode

dialog/choice/* → select branch, etc.

3.4. debug/ — Debug & Tools
Used for development / debug UI.

Examples:

debug/toggle-fps

debug/reload-scene

debug/teleport/home

These routes should never be relied on in shipping gameplay.
They are allowed to be more volatile and environment-specific.

3.5. system/ (optional) — System-Level Actions
Reserve this for non-UI but globally visible actions:

system/quit

system/open-settings

system/open-credits

Whether these map to a screen or a direct behavior is up to the app.

4. Route → ScreenKey Mapping
Navigation routes are configured in UIScreenCatalog via UIRouteEntry:

csharp
코드 복사
[Serializable]
public struct UIRouteEntry
{
    public string route;   // e.g. "nav/home"
    public ScreenKey key;  // e.g. ScreenKey.Home
}
4.1. Rules
Every navigation route MUST start with nav/.

Multiple routes may map to the same ScreenKey.

Example:

text
코드 복사
"nav/shop"                → ScreenKey.Shop
"nav/shop/special-offer"  → ScreenKey.Shop
"nav/shop/from-result"    → ScreenKey.Shop
For each ScreenKey, it is recommended to define one “canonical” route:

e.g. nav/home, nav/shop, nav/result

4.2. Fallback
UIRouteKeyRegistry may optionally add fallback mappings:

If a route is equal to ScreenKey.ToString() (e.g. "Home"),

and no explicit UIRouteEntry exists,

the registry can treat "Home" as a legacy/fallback route for ScreenKey.Home.

This is for migration and testing convenience only.
New routes should always use the nav/<name> format.

5. UIActionKey Registry
UIActionKeyRegistry is the central place that maps route strings to UIActionKey.

Typical pattern:

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
And convenience constants:

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
Guidelines:

Shared/important routes should be given a named constant.

One-off routes (used only in a single screen) can remain as raw strings in specs.

6. Usage in Specs
WidgetSpec.onClickRoute is always a route string:

csharp
코드 복사
public sealed class WidgetSpec
{
    public string onClickRoute;
    // ...
}
Examples:

Navigation button:

text
코드 복사
onClickRoute = "nav/home"
HUD-related button/trigger:

text
코드 복사
onClickRoute = "hud/gold"
At compose/bind time:

onClickRoute → UIActionKey via UIActionKeyRegistry.Get(route)

IUiActionBinder implementations decide how to bind:

RouteActionBinder:

If route is nav/* and mapped to a ScreenKey, bind to UIRouter.Navigate(...)

Other binders:

e.g. HUD binder, dialogue binder, debug binder, etc.

7. Do & Don’t
Do
✅ Use clear prefixes: nav/, hud/, dialog/, debug/, system/

✅ Keep routes short but descriptive

✅ Treat routes as a public protocol:

Changing them is a breaking change for specs & tools

✅ Centralize shared routes in:

UIScreenCatalog.routes (for navigation)

UIActionKeys (for code-side usage)

Don’t
❌ Don’t encode complex logic in the route string

e.g. avoid nav/shop?tab=sale&source=result style

❌ Don’t use user-facing text as routes

labels can change; routes should be stable

❌ Don’t mix prefixes randomly

home, goHome, HomeScreen → prefer nav/home

❌ Don’t rely on ScreenKey.ToString() as your main protocol

it’s only acceptable as a temporary fallback

8. Extending the Protocol
When you need a new category:

Decide if it fits an existing prefix.

If it is screen navigation → nav/

If it is HUD-related → hud/

If it is dialogue-related → dialog/

If it genuinely doesn’t fit:

Add a new prefix (menu/, inventory/, card/, etc.)

Document it in this file with:

What prefix means

Typical examples

Whether it maps to ScreenKey or not

Try to keep prefixes:

Small in number

Clear in purpose

Stable over time

9. Versioning / Stability
For now, this protocol is considered:

“Experimental but converging” — good enough to use in a project,

but still allowed to change if the architecture evolves.

Once the protocol stabilizes:

Prefixes and common route names should be treated as public API.

Changes should be:

Backwards compatible, or

Done via explicit migrations (e.g. old → new mapping layer).

Until then, feel free to refine names,
but try not to introduce one-off, inconsistent patterns.
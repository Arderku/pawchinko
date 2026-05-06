# UI Toolkit — USS, Styling, Animation

> Doc 2 of 3. Companions: [UI_TOOLKIT_OVERVIEW.md](UI_TOOLKIT_OVERVIEW.md) · [UI_TOOLKIT_COMPONENTS.md](UI_TOOLKIT_COMPONENTS.md).

USS (Unity Style Sheets) is *like* CSS but not CSS — about 80% overlap, 20% bites. This doc lays out the supported subset, Pawchinko-specific conventions (BEM, folder layering, design tokens), all the animation patterns the toolkit gives you, and the most common ways AI agents hallucinate features that don't exist.

---

## How to read this file

- **First read §3** — the unsupported-features list. If you skip it you'll write CSS that silently does nothing.
- §4–§7 are project conventions (folders, BEM, tokens).
- §8–§10 are animation/state.
- §11–§14 are the cookbook (sprites, cursors, theming, layout recipes).
- §16 is the hallucination guard.

---

## 1. Conventions

- File paths: `Assets/UI/Uss/...` per Overview doc §3.
- Class naming: BEM — `.block`, `.block__element`, `.block--modifier`. Mandatory for any reusable style.
- Class names are kebab-case. Custom USS variables are also kebab-case prefixed with `--`.

---

## 2. What USS is

A subset of CSS with Unity-specific extensions (the `-unity-*` prefix). Files end in `.uss`. Attach to a UXML via `<Style src="..." />` or to a `VisualElement` at runtime with `element.styleSheets.Add(stylesheet);`.

Selectors supported:

- Type: `Button { ... }`, `Label { ... }`
- Class: `.button--primary { ... }`
- ID (rare): `#unique-id { ... }`
- Descendant: `.parent .child { ... }`
- Direct child: `.parent > .child { ... }`
- Pseudo-classes: `:hover`, `:active`, `:focus`, `:checked`, `:disabled`, `:selected`, `:root`
- Multiple classes on one element: `.a.b { ... }`

The `:root` selector applies to the root of each panel — use it for design tokens (variables shared everywhere).

---

## 3. USS vs CSS — the "what bites you" list

### Supported but quirky

- `display` — only `flex` and `none`. **No `block`, `inline`, `grid`, `inline-block`, `table`, `flow-root`.**
- `position` — only `relative` and `absolute`. **No `fixed`, `sticky`, `static`.** (The "fixed-equivalent" is `absolute` on a top-level element.)
- Units — `px`, `%`, `auto`. **No `em`, `rem`, `vh`, `vw`, `ch`, `pt`.**
- Colors — `#rrggbb`, `#rrggbbaa`, `rgb(r, g, b)`, `rgba(r, g, b, a)`, named colors (`red`, `transparent`...). **No `hsl()`, no CSS Color Level 4 syntax, no `currentColor`.**
- Pseudo-classes — `:hover`, `:active`, `:focus`, `:checked`, `:disabled`, `:selected`, `:root`. **No `:not()`, `:has()`, `:first-child`, `:last-child`, `:nth-child`, `:empty`.**
- Animations — only via `transition-*` properties. **No `@keyframes` rule.** For multi-step sequences use C# `schedule.Execute` with class swaps (see §10).
- Variables — `--name: value;` and `var(--name)` work. Redeclaring on a child element scope-overrides for that subtree.
- Border colors — separate per side (`border-top-color`, `border-right-color`, `border-bottom-color`, `border-left-color`). The shorthand `border-color: red;` works in modern UI Toolkit but per-side is safer.
- Background image — `background-image: url('project://database/Assets/UI/Sprites/foo.png');`
- Background sizing — `-unity-background-scale-mode: stretch-to-fill | scale-and-crop | scale-to-fit;`. There's also limited `background-size: <px> <px>;` for tile sizes (see §11).
- Tinting a background image — `-unity-background-image-tint-color: rgb(...)`. **`color` does not tint backgrounds**; it only tints text.
- 9-slice — `-unity-slice-left/right/top/bottom: <px>;` and `-unity-slice-scale: 1;` for stretchable panels.
- Text font — `-unity-font-definition: url('...SDF.asset');` for TextMeshPro SDF fonts (preferred) or `-unity-font: url('...ttf');` for legacy TTF.
- Text alignment — `-unity-text-align: middle-center;` (`upper-left`, `upper-center`, `upper-right`, `middle-left`, `middle-center`, `middle-right`, `lower-left`, `lower-center`, `lower-right`).

### Unsupported — explicit DO NOT list

| CSS feature | Why it's not in USS | What to use instead |
|---|---|---|
| `@media` | No media-query engine | Swap stylesheets at runtime (see §13) |
| `@import` | No import directive | Attach `<Style>` tags or `styleSheets.Add` |
| `@keyframes` | No keyframe animations | `transition-*` + class swaps + `schedule.Execute` |
| `grid-*` | No grid layout | Flexbox |
| `box-shadow` | Not implemented | Layered `VisualElement`s, or fake with a 9-slice sprite |
| `text-decoration` | Not implemented | Pre-decorate the text via TMP markup |
| `text-transform` | Not implemented | Set the string already-transformed in C# |
| `letter-spacing` | Use `-unity-letter-spacing` | `-unity-letter-spacing: 5px;` |
| `vertical-align` | Use `-unity-text-align` | See alignment table above |
| `float`, `clear` | No float layout | Flexbox |
| `overflow-x`, `overflow-y` | Only `overflow: hidden` / `visible` | One-axis overflow control via padded child |
| `transform: translate(...)` shorthand | Not supported | `translate: 10px 0;` directly |
| `transform: scale(...)` shorthand | Not supported | `scale: 1.1 1.1;` directly |
| `transform: rotate(...)` shorthand | Not supported | `rotate: 45deg;` directly |
| `filter`, `backdrop-filter` | Not implemented | Use a URP `Volume` for blur on the camera |
| `calc()` | Not implemented | Compute in C# and assign |
| `:not()`, `:has()`, `:nth-child` | Not implemented | Explicit BEM modifier classes |
| `currentColor` | Not implemented | Use a USS variable (`var(--color-text)`) |

If you find yourself reaching for any of the right column, restructure your USS to use the alternative.

---

## 4. Folder & ordering convention

Three layers, three folders:

```
Assets/UI/Uss/
├── Base/         design tokens + primitives (load first)
│   ├── Tokens.uss
│   ├── Colors.uss
│   ├── Text.uss
│   ├── Buttons.uss
│   └── Common.uss
├── Screens/      one file per screen
│   ├── Hud.uss
│   ├── PauseMenu.uss
│   └── MainMenu.uss
└── Components/   one file per custom control
    ├── HealthBar.uss
    ├── SlideToggle.uss
    └── RadialProgress.uss
```

**Order matters.** In a UXML, list `<Style>` tags in this order:

```xml
<Style src="project://database/Assets/UI/Uss/Base/Tokens.uss" />
<Style src="project://database/Assets/UI/Uss/Base/Common.uss" />
<Style src="project://database/Assets/UI/Uss/Base/Buttons.uss" />
<Style src="project://database/Assets/UI/Uss/Components/HealthBar.uss" />
<Style src="project://database/Assets/UI/Uss/Screens/Hud.uss" />
```

The last-loaded sheet wins on equal selector specificity. Tokens first (so variables resolve), screen-specific last (so screens can override component defaults).

---

## 5. Naming — BEM (mandatory)

Every reusable style follows Block / Element / Modifier:

| Concept | Example | Notes |
|---|---|---|
| Block | `.health-bar` | The component or screen as a whole. |
| Element | `.health-bar__background`, `.health-bar__progress` | A part of the block. |
| Modifier | `.health-bar--boss`, `.health-bar__progress--low` | A variant of a block or element. |

Reasoning:
- Cheap class queries: `root.Q(className: "health-bar__progress")`.
- No collision with Unity built-ins like `unity-button`, `unity-toggle__input`.
- Prevents the cascade nightmare of nested selectors.

```css
/* good */
.health-bar { /* ... */ }
.health-bar__background { /* ... */ }
.health-bar__progress { /* ... */ }
.health-bar__progress--low { background-color: var(--color-danger); }
.health-bar--boss { max-width: 40%; }

/* bad — too specific, fragile */
.hud .row .health > .bar > .progress { /* ... */ }
```

---

## 6. Design tokens — `Tokens.uss`

Centralize colors, spacing, radii, font sizes as USS variables. Define them on `:root` so every panel inherits.

```css
:root {
    --color-bg:          rgb(20, 22, 30);
    --color-surface:     rgb(36, 40, 56);
    --color-surface-hi:  rgb(48, 54, 74);
    --color-text:        rgb(240, 240, 240);
    --color-text-muted:  rgb(170, 170, 180);
    --color-accent:      rgb(245, 150, 20);
    --color-accent-hot:  rgb(255, 226, 124);
    --color-success:     rgb(120, 200, 90);
    --color-danger:      rgb(220, 90, 5);
    --color-warning:     rgb(245, 200, 60);

    --space-1: 4px;
    --space-2: 8px;
    --space-3: 16px;
    --space-4: 24px;
    --space-5: 32px;
    --space-6: 48px;

    --radius-sm: 4px;
    --radius-md: 8px;
    --radius-lg: 16px;
    --radius-pill: 999px;

    --font-size-xs: 14px;
    --font-size-sm: 18px;
    --font-size-md: 28px;
    --font-size-lg: 48px;
    --font-size-xl: 72px;

    -unity-font-definition: url("project://database/Assets/UI/Fonts/Pawchinko-Regular_SDF.asset");
}

.text--muted  { color: var(--color-text-muted); }
.text--accent { color: var(--color-accent); }
.text--danger { color: var(--color-danger); }
.text__size--xs { font-size: var(--font-size-xs); }
.text__size--sm { font-size: var(--font-size-sm); }
.text__size--md { font-size: var(--font-size-md); }
.text__size--lg { font-size: var(--font-size-lg); }
.text__size--xl { font-size: var(--font-size-xl); }
.text__shadow--small { text-shadow: 2px 2px 1px rgba(0, 0, 0, 0.4); }
.text__shadow--large { text-shadow: 5px 5px 2px rgba(0, 0, 0, 0.4); }
```

Variables can be **scope-overridden** on any element:

```css
.theme--halloween {
    --color-accent: rgb(220, 110, 30);
    --color-accent-hot: rgb(255, 160, 70);
}
```

Adding the `theme--halloween` class to a parent retints every accent inside.

---

## 7. Buttons — full pattern

A complete `Buttons.uss`. Base style + three modifier variants + states + transitions.

```css
Button {
    background-color: rgba(0, 0, 0, 0);
    border-width: 0;
    color: var(--color-text);
    -unity-font-style: normal;
    text-shadow: 5px 5px 2px rgba(0, 0, 0, 0.4);
    padding: var(--space-2) var(--space-3);
    transition-duration: 0.25s;
    transition-property: scale, -unity-background-image-tint-color, background-color, color;
    transition-timing-function: ease-out;
}

Button:hover    { scale: 1.05 1.05; }
Button:active   { scale: 0.96 0.96; }
Button:disabled { opacity: 0.5; }
Button:focus    { /* gamepad/keyboard focus ring */
    border-width: 2px;
    border-color: var(--color-accent-hot);
}

.button--primary {
    background-image: url('project://database/Assets/UI/Sprites/UI_atlas.psd#bt_orange');
    -unity-slice-left: 32; -unity-slice-right: 32;
    -unity-slice-top: 24; -unity-slice-bottom: 24;
}
.button--primary:hover  { -unity-background-image-tint-color: var(--color-accent-hot); }
.button--primary:active { -unity-background-image-tint-color: var(--color-danger); }
.button--primary:disabled { -unity-background-image-tint-color: rgb(120, 120, 120); }

.button--secondary {
    background-color: var(--color-surface);
    border-radius: var(--radius-md);
}
.button--secondary:hover { background-color: var(--color-surface-hi); }

.button--ghost {
    background-color: rgba(0, 0, 0, 0);
    color: var(--color-accent);
}
.button--ghost:hover { color: var(--color-accent-hot); }

.button--danger {
    background-color: var(--color-danger);
    border-radius: var(--radius-md);
}
```

Usage:

```xml
<ui:Button name="play"     text="PLAY"   class="button--primary" />
<ui:Button name="settings" text="Cog"    class="button--secondary" />
<ui:Button name="cancel"   text="Cancel" class="button--ghost" />
<ui:Button name="quit"     text="Quit"   class="button--danger" />
```

---

## 8. Transitions — the primary animation tool

Mental model: **change a class from C#, USS interpolates over `transition-duration`.**

### Properties

```css
.panel {
    opacity: 1;
    translate: 0 0;
    transition-property: opacity, translate;
    transition-duration: 0.25s, 0.3s;          /* one per property */
    transition-timing-function: ease-out;       /* applies to all */
    transition-delay: 0s, 0.05s;               /* one per property */
}
.panel--hidden {
    opacity: 0;
    translate: 0 -20px;
}
```

C# toggle:

```csharp
panel.AddToClassList("panel--hidden");
// ... USS interpolates opacity 1 -> 0 and translate 0,0 -> 0,-20px over 0.25s/0.3s
panel.RemoveFromClassList("panel--hidden");
// ... and back
```

### Transitionable properties (most-used)

`opacity`, `scale`, `translate`, `rotate`, `background-color`, `color`, `width`, `height`, `min-width`, `max-width`, `top`, `left`, `right`, `bottom`, `padding-*`, `margin-*`, `border-*-color`, `border-*-width`, `border-radius`, `-unity-background-image-tint-color`, custom `--*` variables.

### Easings

Available timing functions: `ease`, `ease-in`, `ease-out`, `ease-in-out`, `linear`, plus a family of named curves: `ease-in-sine`, `ease-out-sine`, `ease-in-out-sine`, `ease-in-quad`, `ease-out-quad`, `ease-in-out-quad`, `ease-in-cubic`, `ease-out-cubic`, `ease-in-out-cubic`, `ease-in-circ`, `ease-out-circ`, `ease-in-back`, `ease-out-back`, `ease-in-out-back`, `ease-in-bounce`, `ease-out-bounce`, `ease-in-out-bounce`, `ease-in-elastic`, `ease-out-elastic`, `ease-in-out-elastic`.

When in doubt, `ease-out` for entrances, `ease-in` for exits.

### Animatable but not via transition

`display` is **discrete** — it can't transition. Use `opacity` + `pointer-events`-style tricks (`pickingMode = Ignore`) or class-swap with a `schedule.Execute` delay before flipping `display`.

```csharp
// Fade out then hide:
panel.AddToClassList("panel--hidden");
panel.schedule.Execute(() => panel.style.display = DisplayStyle.None).StartingIn(250);
```

---

## 9. Hover / Active / Focus / Checked / Disabled

State pseudo-classes with concrete examples.

```css
/* Slide toggle pattern */
.slide-toggle__input {
    width: 64px; height: 28px; border-radius: 14px;
    background-color: var(--color-surface);
    transition-property: background-color;
    transition-duration: 0.25s;
}
.slide-toggle__input--checked {
    background-color: var(--color-accent);
}
.slide-toggle__input-knob {
    width: 24px; height: 24px; border-radius: 12px;
    background-color: white;
    translate: 2px 0;
    transition-property: translate;
    transition-duration: 0.25s;
}
.slide-toggle__input--checked > .slide-toggle__input-knob {
    translate: 38px 0;
}
.slide-toggle:focus .slide-toggle__input-knob {
    border-width: 1px;
    border-color: var(--color-accent-hot);
}

/* Disabled state on any control */
.is-disabled {
    opacity: 0.4;
}

/* Selected list item */
.inventory-row {
    background-color: rgba(0, 0, 0, 0);
}
.inventory-row:hover {
    background-color: var(--color-surface);
}
.unity-list-view__item--selected .inventory-row {
    background-color: var(--color-accent);
}
```

Note: ListView's selection class is `unity-list-view__item--selected` on the row container. Style the inner row by descendant.

---

## 10. Multi-step animations via C# + class swaps

USS has no `@keyframes`. Build sequences in C# with `schedule.Execute`.

```csharp
// "Damage flash" — tint red, then back, then again:
private void FlashDamage(VisualElement target)
{
    target.AddToClassList("flash-damage");
    target.schedule.Execute(() => target.RemoveFromClassList("flash-damage")).StartingIn(120);
}
```

```css
.flash-damage {
    -unity-background-image-tint-color: rgb(255, 80, 80);
    transition-property: -unity-background-image-tint-color;
    transition-duration: 0.05s;
}
```

A multi-step "celebrate" sequence:

```csharp
private void Celebrate(VisualElement target)
{
    target.AddToClassList("celebrate--pop");
    target.schedule.Execute(() =>
    {
        target.RemoveFromClassList("celebrate--pop");
        target.AddToClassList("celebrate--settle");
    }).StartingIn(250);
    target.schedule.Execute(() =>
    {
        target.RemoveFromClassList("celebrate--settle");
    }).StartingIn(750);
}
```

```css
.celebrate--pop    { scale: 1.4 1.4; transition-duration: 0.25s; transition-timing-function: ease-out-back; }
.celebrate--settle { scale: 1 1;     transition-duration: 0.5s;  transition-timing-function: ease-in-out-sine; }
```

`schedule.Execute` returns an `IVisualElementScheduledItem`; you can `.Pause()` to cancel.

For continuous animation (e.g. a pulsing target), use `.Every(ms)`:

```csharp
var pulse = target.schedule.Execute(() => target.ToggleInClassList("pulse--on")).Every(500);
// later: pulse.Pause();
```

```css
.pulse--on { scale: 1.1 1.1; transition-duration: 0.5s; }
```

---

## 11. Sprites, atlases, 9-slice

### URL anatomy

Texture URLs in USS look like:

```
url('project://database/Assets/UI/Sprites/<file>.<ext>?fileID=<id>&guid=<guid>&type=3#<spriteName>')
```

The `#spriteName` suffix selects a sub-sprite from a `.psb` / `.psd` atlas. Unity manages `fileID` and `guid` automatically — **don't write these by hand**. Pick the sprite via UI Builder's image field or copy-paste from an existing USS file.

A simpler form for non-atlas assets:

```css
background-image: url('project://database/Assets/UI/Sprites/panel.png');
```

### `-unity-background-scale-mode`

| Value | Behavior |
|---|---|
| `stretch-to-fill` | Fills the element box, distorting if aspect changes. |
| `scale-and-crop` | Fills the box maintaining aspect; crops overflow. |
| `scale-to-fit` | Fits inside the box maintaining aspect; letterboxes. |

### 9-slice for stretchable panels

```css
.panel--bordered {
    background-image: url('project://database/Assets/UI/Sprites/panel_bg.png');
    -unity-slice-left: 24;
    -unity-slice-right: 24;
    -unity-slice-top: 24;
    -unity-slice-bottom: 24;
    -unity-slice-scale: 1;
}
```

The four `-unity-slice-*` values are the corner sizes (in px). The center stretches; corners stay fixed-size; edges stretch on one axis.

### Tileable backgrounds

```css
.tileable--128px {
    background-image: url('project://database/Assets/UI/Sprites/PatternTile.png');
    background-size: 128px 128px;
    background-repeat: repeat;
}
```

---

## 12. Cursors

```css
:root {
    cursor: url('project://database/Assets/UI/Sprites/Cursor_A.png') 24 17;
}
.button--primary {
    cursor: url('project://database/Assets/UI/Sprites/Cursor_B.png') 10 11;
}
.is-disabled {
    cursor: url('project://database/Assets/UI/Sprites/Cursor_X.png') 12 12;
}
```

The two trailing numbers are the **hotspot** offset (the active click pixel). Default is `0 0` (top-left).

---

## 13. Theme & orientation swapping

USS has no `@media`. Implement responsiveness by **swapping the active stylesheets** at runtime.

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pawchinko.UI
{
    /// <summary>
    /// Swaps a curated set of stylesheets on a UIDocument's root.
    /// Use for orientation flips and seasonal/themed variants.
    /// </summary>
    public class ThemeManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument document;

        [Header("Default Theme")]
        [SerializeField] private List<StyleSheet> defaultTheme;

        [Header("Variants")]
        [SerializeField] private List<StyleSheet> halloweenTheme;
        [SerializeField] private List<StyleSheet> portraitOverrides;
        [SerializeField] private List<StyleSheet> landscapeOverrides;

        public void ApplyTheme(IReadOnlyList<StyleSheet> next)
        {
            if (document == null)
            {
                Debug.LogError("[ThemeManager] document not assigned in Inspector!");
                return;
            }

            VisualElement root = document.rootVisualElement;
            root.styleSheets.Clear();
            foreach (StyleSheet s in next) root.styleSheets.Add(s);
        }
    }
}
```

Usage:

```csharp
themeManager.ApplyTheme(themeManager.HalloweenTheme);
themeManager.ApplyTheme(Screen.width > Screen.height ? landscape : portrait);
```

For seasonal accents *without* a full swap, add/remove a single class on the root:

```csharp
root.EnableInClassList("theme--halloween", isHalloween);
```

```css
.theme--halloween {
    --color-accent: rgb(220, 110, 30);
    --color-accent-hot: rgb(255, 160, 70);
}
.theme--halloween .home-screen__background {
    background-image: url('project://database/Assets/UI/Sprites/halloween_bg.png');
}
```

---

## 14. Custom USS properties read from C#

Custom properties (any `--name`) can be read in C# via `CustomStyleProperty<T>` + a `CustomStyleResolvedEvent` callback. Useful when a `[UxmlElement]` does its own drawing (see Components doc §6) and wants its colors to be USS-author-able.

```csharp
using UnityEngine;
using UnityEngine.UIElements;

namespace Pawchinko.UI
{
    public partial class RadialProgress : VisualElement
    {
        private static readonly CustomStyleProperty<Color> s_TrackColor    = new("--track-color");
        private static readonly CustomStyleProperty<Color> s_ProgressColor = new("--progress-color");

        private Color _trackColor    = Color.black;
        private Color _progressColor = Color.red;

        public RadialProgress()
        {
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStylesResolved);
        }

        private void OnCustomStylesResolved(CustomStyleResolvedEvent evt)
        {
            bool repaint = false;
            if (evt.customStyle.TryGetValue(s_TrackColor,    out _trackColor))    repaint = true;
            if (evt.customStyle.TryGetValue(s_ProgressColor, out _progressColor)) repaint = true;
            if (repaint) MarkDirtyRepaint();
        }
    }
}
```

USS:

```css
.radial-progress {
    --track-color: rgb(60, 60, 60);
    --progress-color: var(--color-accent);
    width: 96px;
    height: 96px;
}
.radial-progress--danger {
    --progress-color: var(--color-danger);
}
```

---

## 15. Layout cookbook (flexbox playbook)

### Centered modal

```css
.modal {
    position: absolute;
    width: 100%;
    height: 100%;
    align-items: center;
    justify-content: center;
    background-color: rgba(0, 0, 0, 0.6);
}
.modal__panel {
    width: 480px;
    padding: var(--space-4);
    background-color: var(--color-surface);
    border-radius: var(--radius-lg);
}
```

### Full-screen background

```css
.background-graphic {
    position: absolute;
    top: 0; left: 0;
    width: 100%; height: 100%;
    -unity-background-scale-mode: scale-and-crop;
}
```

### Top bar (row, space-between)

```css
.top-bar {
    flex-direction: row;
    justify-content: space-between;
    align-items: center;
    padding: var(--space-2) var(--space-3);
    height: 64px;
    background-color: var(--color-surface);
}
```

### Stacked column with stretched children

```css
.settings-column {
    flex-direction: column;
    align-items: stretch;
    padding: var(--space-3);
    /* gap doesn't exist in USS; use margin on children */
}
.settings-column > * {
    margin-bottom: var(--space-2);
}
```

### Anchor to corner

```css
.fps-counter {
    position: absolute;
    bottom: var(--space-2);
    right: var(--space-2);
    font-size: var(--font-size-sm);
}
```

### Spacer (push siblings apart)

```xml
<ui:VisualElement name="left-side" />
<ui:VisualElement name="spacer" style="flex-grow: 1;" />
<ui:VisualElement name="right-side" />
```

### Safe-area wrapper

```xml
<ui:VisualElement name="safe-area" picking-mode="Ignore"
                  style="position: absolute; width: 100%; height: 100%;">
    <!-- screens -->
</ui:VisualElement>
```

### Tileable pattern background

```css
.tileable--128px {
    background-image: url('project://database/Assets/UI/Sprites/PatternTile.png');
    background-size: 128px 128px;
    background-repeat: repeat;
}
```

### Aspect-locked container (hand-rolled)

USS has no `aspect-ratio`. Either:
- Set fixed `width` + `height` in px.
- Use a custom `[UxmlElement]` that responds to `GeometryChangedEvent` and sets one dimension based on the other (see Components doc §10).

### Responsive grid via flex-wrap

```css
.grid {
    flex-direction: row;
    flex-wrap: wrap;
}
.grid > .grid__item {
    width: 33%;       /* 3-column */
    padding: var(--space-2);
}
```

### Common.uss helpers (anchor utilities)

```css
.screen__anchor--top-left {
    position: absolute; width: 100%; height: 100%;
    left: 0; top: 0;
}
.screen__anchor--top-right {
    position: absolute; width: 100%; height: 100%;
    top: 0; right: 0;
}
.screen__anchor--bottom-left {
    position: absolute; width: 100%; height: 100%;
    left: 0; bottom: 0;
}
.screen__anchor--bottom-right {
    position: absolute; width: 100%; height: 100%;
    right: 0; bottom: 0;
}
.screen__anchor--center {
    position: absolute; width: 100%; height: 100%;
    align-items: center; justify-content: center;
}
.alignment--center  { align-items: center; }
.justify--center    { justify-content: center; }
.border__radius--md { border-radius: var(--radius-md); }
.border__radius--lg { border-radius: var(--radius-lg); }
```

---

## 16. Hallucination guard — what AI agents commonly get wrong

| Don't | Do | Why |
|---|---|---|
| `transform: translateX(10px);` | `translate: 10px 0;` | UI Toolkit uses direct USS properties; no `transform` shorthand. |
| `transform: scale(1.1);` | `scale: 1.1 1.1;` | Same. |
| `transform: rotate(45deg);` | `rotate: 45deg;` | Same. |
| `box-shadow: 2px 2px 4px black;` | Layered `VisualElement` or 9-slice sprite | Not implemented in USS. |
| `display: block;` / `display: grid;` | `display: flex;` (or `none`) | Only flex/none are supported. |
| `width: 50vw;` | `width: 50%;` | No viewport units. |
| `@media (max-width: 600px) { ... }` | Swap stylesheets at runtime (§13) | No media queries. |
| `@import "other.uss";` | `<Style>` tags or `styleSheets.Add` | No import directive. |
| `@keyframes spin { ... }` | `transition-*` + class swaps + `schedule.Execute` (§10) | No keyframe support. |
| `.foo:not(:hover) { ... }` | Explicit modifier class | No `:not()`. |
| `:nth-child(2n)` | Style each row from C# in `bindItem` | No structural pseudo-classes. |
| `outline: 1px solid red;` for focus | `border-*-color` on `:focus` | `outline` not supported. |
| `currentColor` | `var(--color-text)` | Not supported. |
| `calc(100% - 24px)` | Compute in C# and assign | No `calc()`. |
| `color: red;` to tint a background image | `-unity-background-image-tint-color: red;` | `color` only tints text. |
| Animate `display` | Animate `opacity`, then schedule `display: none` | `display` is discrete. |
| Set `style.X` and *also* a USS class for the same property | Pick one | Inline style overrides USS until cleared with `style.X = StyleKeyword.Null;`. |
| `transition: all 0.25s ease;` shorthand | `transition-property`, `-duration`, `-timing-function` separately | Shorthand has been buggy historically; explicit form is safest. |
| Multiple `transition-property` values without matching `transition-duration` count | Match counts (or single duration applies to all) | Mismatched counts can fall back unpredictably. |
| Cascade via descendants like `.hud .row .health-bar__progress` | Scope BEM modifiers (`.health-bar--hud .health-bar__progress`) | Cheaper queries, predictable specificity. |

---

## 17. Cheat sheet

### Most-used properties

```css
flex-direction: row | column;
justify-content: flex-start | flex-end | center | space-between | space-around | space-evenly;
align-items: flex-start | flex-end | center | stretch | baseline;
align-self: ...;
flex-grow: 1;
flex-shrink: 0;
flex-basis: auto | <px> | <%>;

position: relative | absolute;
top / right / bottom / left: <px> | <%>;

display: flex | none;
visibility: visible | hidden;
opacity: 0..1;

width / height / min-width / max-width / min-height / max-height: <px> | <%>;
padding / padding-top / padding-right / padding-bottom / padding-left: <px> | <%>;
margin / margin-...: <px> | <%>;

background-color: rgb(...);
background-image: url('...');
-unity-background-scale-mode: stretch-to-fill | scale-and-crop | scale-to-fit;
-unity-background-image-tint-color: rgb(...);

border-width / border-top-width / ...: <px>;
border-color / border-top-color / ...: rgb(...);
border-radius / border-top-left-radius / ...: <px>;

color: rgb(...);
font-size: <px>;
-unity-font-style: normal | bold | italic | bold-and-italic;
-unity-font-definition: url('...SDF.asset');
-unity-text-align: middle-center | upper-left | ...;
text-shadow: <x> <y> <blur> <color>;

scale: <x> <y>;
translate: <x> <y>;
rotate: <deg>deg;
transform-origin: <x> <y>;

transition-property: <a>, <b>;
transition-duration: <a>s, <b>s;
transition-timing-function: ease-out;
transition-delay: 0s, 0.05s;

cursor: url('...') <hot-x> <hot-y>;

-unity-slice-left / -right / -top / -bottom: <px>;
-unity-slice-scale: 1;
```

---

## 18. Further reading

- Unity 6 Manual: [Style UI](https://docs.unity3d.com/6000.4/Documentation/Manual/UIE-USS.html)
- Unity 6 Manual: [USS supported properties](https://docs.unity3d.com/6000.4/Documentation/Manual/UIE-USS-Supported-Properties.html)
- Unity 6 Manual: [USS transitions](https://docs.unity3d.com/6000.4/Documentation/Manual/UIE-Transitions.html)
- Companion: [UI_TOOLKIT_OVERVIEW.md](UI_TOOLKIT_OVERVIEW.md)
- Companion: [UI_TOOLKIT_COMPONENTS.md](UI_TOOLKIT_COMPONENTS.md)

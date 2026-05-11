---
name: unity-ui-uss-pitfalls
description: USS subset rules and silent-failure list for Unity 6 UI Toolkit stylesheets. Use before writing or editing any .uss file, or before setting inline styles on a VisualElement (element.style in C#). Prevents writing CSS that USS quietly ignores: display block/grid, position fixed, em/rem/vh/vw units, @keyframes, box-shadow, :not() selectors, hsl() colors, and the CSS transform shorthand.
---

# UI Toolkit USS - Pitfalls & Silently Ignored Properties

USS is *like* CSS but not CSS - about 80% overlap, 20% bites. Most failures are silent: the property parses, the editor shows no error, and the style simply does not apply. Read this before writing any `.uss` file or before setting inline styles in C# (`element.style.X = ...`).

## Silently ignored CSS

These parse without error and do nothing. Use the right column instead.

| Don't write | What to use |
|---|---|
| `display: block;` / `display: grid;` / `display: inline;` | `display: flex;` (or `display: none;`). Only `flex` and `none` work. |
| `position: fixed;` / `position: sticky;` / `position: static;` | `position: absolute;` on a top-level element (the "fixed-equivalent"). Only `relative` and `absolute` are supported. |
| `width: 50vw;` / `height: 100vh;` / `font-size: 1.5em;` / `2rem` | `px`, `%`, `auto`. No viewport / em / rem / ch / pt units. |
| `@keyframes spin { ... }` / `@media (...)` / `@import "..."` | Class swaps + `transition-*` for animation; runtime stylesheet swap (`element.styleSheets.Add`) for media/import. |
| `box-shadow: 2px 2px 4px black;` | Layered `VisualElement` siblings, or a 9-slice sprite background. |
| `.foo:not(.bar)` / `:nth-child(2n)` / `:first-child` | Explicit BEM modifier classes (`.foo--bar`, `.row--first`). No structural pseudo-classes. |
| `color: hsl(120, 50%, 50%);` / `currentColor` | `rgb()`, `rgba()`, `#rrggbb`, `#rrggbbaa`, named colors. Use `var(--color-text)` instead of `currentColor`. |
| `transform: translateX(10px);` / `transform: scale(1.1);` / `transform: rotate(45deg);` | Direct properties: `translate: 10px 0;` / `scale: 1.1 1.1;` / `rotate: 45deg;`. No `transform` shorthand. |
| `text-decoration: underline;` / `text-transform: uppercase;` / `letter-spacing: 2px;` | Pre-decorate via TMP rich-text markup; uppercase the string in C#; `-unity-letter-spacing: 2px;`. |
| `filter: blur(...)` / `backdrop-filter` / `calc(100% - 24px)` / `outline` | URP `Volume` for blur; compute in C# and assign for `calc`; `border-*-color` on `:focus` for outline. |

`color: red;` does **not** tint a background image. `color` only tints text. To tint an image use `-unity-background-image-tint-color: rgb(...)`.

## Unity-only properties (often forgotten)

Every property below is the only USS spelling that works for that effect. Watch the `-unity-` prefix.

| Property | Use case |
|---|---|
| `-unity-text-align` | Text alignment inside a `Label`/`Button`. Values: `upper-left`, `upper-center`, `upper-right`, `middle-left`, `middle-center`, `middle-right`, `lower-left`, `lower-center`, `lower-right`. |
| `-unity-font-style` | `normal` / `bold` / `italic` / `bold-and-italic`. Replaces CSS `font-weight` + `font-style`. |
| `-unity-font-definition` | `url('project://database/Assets/UI/Fonts/Foo_SDF.asset');` for TMP SDF fonts (preferred). |
| `-unity-background-scale-mode` | `stretch-to-fill` / `scale-and-crop` / `scale-to-fit`. The `background-size` keyword equivalents do not exist. |
| `-unity-background-image-tint-color` | Tint a `background-image` with `rgb()`/`rgba()`. `color` only tints text. |
| `-unity-slice-left` / `-right` / `-top` / `-bottom` / `-unity-slice-scale` | 9-slice corner sizes for stretchable panel sprites. |
| `-unity-letter-spacing` | The spacing-between-glyphs property. CSS `letter-spacing` is ignored. |

## Layout escape hatches

USS has no `position: fixed`, no `aspect-ratio`, no CSS Grid, no `gap`. Use these flexbox-first patterns.

```css
.anchor-top-right {
    position: absolute;
    top: 0;
    right: 0;
    left: auto;
}

.anchor-bottom-left {
    position: absolute;
    bottom: 0;
    left: 0;
    top: auto;
}

.centered-overlay {
    position: absolute;
    width: 100%;
    height: 100%;
    align-items: center;
    justify-content: center;
}

.right-aligned-strip {
    flex-direction: row;
    justify-content: flex-end;
    align-items: center;
    width: 100%;
}

.stacked-column-with-gap > * {
    margin-bottom: 8px;
}
```

Notes:
- `position: absolute` is the only way to overlay a panel on top of siblings without affecting their layout.
- `flex-direction: row` + `justify-content: flex-end` is the right way to right-align children of a strip; do not reach for `text-align: right` on the container.
- USS has no `gap` property; use `margin-bottom` (or `margin-right` for rows) on each child.
- Set the explicit opposite anchor (`left: auto;`, `top: auto;`) when overriding only one side. Browsers default the unset side to `auto` automatically; UI Toolkit is occasionally less forgiving when the element previously had inline style for the opposite side.

## Naming + folder reminder

- Class names are kebab-case BEM: `.block`, `.block__element`, `.block--modifier`. Mandatory for any reusable style. No `id` selectors except for one-offs like `#unique-id` (rare).
- Custom USS variables are kebab-case, prefixed `--`: `--color-accent`, `--space-3`, `--radius-md`. Define on `:root` in `Tokens.uss` so every panel inherits.
- USS folders under `Assets/UI/Uss/`:
  - `Base/` - design tokens + primitives (`Tokens.uss`, `Colors.uss`, `Text.uss`, `Buttons.uss`, `Common.uss`). Load first.
  - `Components/` - one file per custom control or reusable widget (`HealthBar.uss`).
  - `Screens/` - one file per screen (`Hud.uss`, `PauseMenu.uss`). Load last so they can override component defaults.
- In a UXML, `<Style>` tag order is **`Base/` -> `Components/` -> `Screens/`**. The last sheet wins on equal selector specificity.

## Where to look next

- [Docs~/UI-Notes/UI_TOOLKIT_USS.md §3 - USS vs CSS](../../../Docs~/UI-Notes/UI_TOOLKIT_USS.md#3-uss-vs-css--the-what-bites-you-list) for the full supported / unsupported reference (units, colors, selectors, animations).
- [Docs~/UI-Notes/UI_TOOLKIT_USS.md §5 - BEM naming](../../../Docs~/UI-Notes/UI_TOOLKIT_USS.md#5-naming--bem-mandatory) for the full Block / Element / Modifier rationale and examples.
- [Docs~/UI-Notes/UI_TOOLKIT_USS.md §16 - Hallucination guard](../../../Docs~/UI-Notes/UI_TOOLKIT_USS.md#16-hallucination-guard--what-ai-agents-commonly-get-wrong) for the full Don't/Do CSS-vs-USS table.

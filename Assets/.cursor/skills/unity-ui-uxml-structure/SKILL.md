---
name: unity-ui-uxml-structure
description: UXML markup rules for Unity 6 UI Toolkit runtime documents. Use before writing or editing any .uxml file (UXML, Template, Instance, VisualElement, Label, or Button markup). Covers Style tag placement, picking-mode rules, the project://database src format, the GUID query-string trap, editor-extension-mode requirement, and the Template/Instance composition pattern.
---

# UI Toolkit UXML - Structure & Markup Rules

UXML is the structure layer of UI Toolkit (HTML-like). Every runtime UXML follows the skeleton below; most failures come from breaking one of the five markup rules.

## Document skeleton

Minimal valid runtime UXML: root tag with `editor-extension-mode="False"`, top-level `<Style>` tags, then the visual tree.

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" editor-extension-mode="False">
    <Style src="project://database/Assets/UI/Uss/Base/Tokens.uss" />
    <Style src="project://database/Assets/UI/Uss/Screens/MyScreen.uss" />
    <ui:VisualElement name="my-screen__root" class="my-screen">
        <ui:Label name="my-screen__title" text="Title" class="text__size--lg" />
        <ui:Button name="my-screen__play-button" text="Play" class="button--primary" />
    </ui:VisualElement>
</ui:UXML>
```

- `xmlns:ui="UnityEngine.UIElements"` is the runtime namespace. Do **not** add `xmlns:uie="UnityEditor.UIElements"` to runtime UXML; it's editor-only.
- `editor-extension-mode="False"` is the runtime setting. `True` is for editor windows.
- `name` is what `root.Q<T>("name")` queries from C#. Case-sensitive.
- `class` is space-separated USS class list (see Rule 4).

## The 5 markup rules

### 1. Always use `project://database/Assets/...` and never include the `?fileID=...&guid=...` query-string

Correct:

```xml
<Style src="project://database/Assets/UI/Uss/Components/MyComponent.uss" />
```

Wrong - the editor sometimes emits this form; **strip the query string**:

```xml
<Style src="project://database/Assets/UI/Uss/Components/MyComponent.uss?fileID=7433441132597879392&amp;guid=ab12cd34..." />
```

The query-string is brittle: when the file is renamed or moved (even via `AssetDatabase.MoveAsset`, which preserves the GUID) UI Builder may re-serialize without re-checking the query, and the next load silently fails because the cached `fileID` no longer points anywhere. The plain `project://database/<path>` form resolves through the asset database every load and survives moves/renames.

### 2. `<Style>` tags live at the document root, never inside a `<ui:VisualElement>`

Correct:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" editor-extension-mode="False">
    <Style src="project://database/Assets/UI/Uss/Base/Tokens.uss" />
    <ui:VisualElement name="root">
        <!-- ... -->
    </ui:VisualElement>
</ui:UXML>
```

Wrong - the stylesheet attaches to nothing and is silently ignored:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" editor-extension-mode="False">
    <ui:VisualElement name="root">
        <Style src="..." />
    </ui:VisualElement>
</ui:UXML>
```

Stylesheets attach to the **document**, not to elements. For per-element stylesheets at runtime use `element.styleSheets.Add(stylesheetAsset);` from C#. Order matters: `Base/` -> `Components/` -> `Screens/`; the last-loaded sheet wins on equal selector specificity.

### 3. Set `picking-mode="Ignore"` on every layout-only / decorative container

Correct (decorative containers pass clicks through):

```xml
<ui:VisualElement name="background-graphic" picking-mode="Ignore" class="background-graphic" />
<ui:VisualElement name="safe-area" picking-mode="Ignore" style="position: absolute; width: 100%; height: 100%;">
    <!-- interactive children inherit pickingMode="Position" by default -->
</ui:VisualElement>
```

Without `picking-mode="Ignore"`, decorative containers intercept clicks intended for elements behind/under them. The default is `Position` (receives input). Set `Ignore` on every wrapper, background, safe-area, or overlay-vfx layer that has no interaction of its own.

### 4. `class="foo bar"` - space-separated. Comma-separated does not work

Correct:

```xml
<ui:Button text="PLAY" class="button--primary play-card__button" />
```

Wrong - parses as a single class literally named `"button--primary,play-card__button"`:

```xml
<ui:Button text="PLAY" class="button--primary, play-card__button" />
<ui:Button text="PLAY" class="button--primary,play-card__button" />
```

Same convention as HTML/CSS. BEM block / element / modifier classes stack space-separated.

### 5. Reuse via `<ui:Template>` + `<ui:Instance>`

Declare a template once at the top of the document, instance it anywhere by name. The instance materializes at load time and is queryable from C# by its `name` attribute on the `<ui:Instance>` element.

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" editor-extension-mode="False">
    <ui:Template name="HealthBar" src="project://database/Assets/UI/Uxml/Components/HealthBar.uxml" />
    <Style src="project://database/Assets/UI/Uss/Base/Tokens.uss" />
    <ui:VisualElement name="hud-row" style="flex-direction: row;">
        <ui:Instance template="HealthBar" name="PlayerHealth" />
        <ui:Instance template="HealthBar" name="EnemyHealth" />
    </ui:VisualElement>
</ui:UXML>
```

From C#, the `UIManager` queries each instance by its `name` attribute and passes the resulting `VisualElement` to the matching `UIView`:

```csharp
VisualElement playerHealthRoot = root.Q<VisualElement>("PlayerHealth");
_playerHealth = new HealthBarView(playerHealthRoot);
```

This is the composition pattern that lets one master UXML drive a whole scene's UI without one giant flat file.

## Reuse pattern - master document composing screens

A master UXML for a battle scene that composes HUD / Pause / Win / Lose screens, each as a `<ui:Template>` + `<ui:Instance>` pair. Decorative wrappers carry `picking-mode="Ignore"`; `<ui:Instance>` blocks for not-yet-shown screens use inline `display: none;` until the matching `UIView.Show()` flips it.

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" editor-extension-mode="False">
    <ui:Template name="HudScreen"   src="project://database/Assets/UI/Uxml/HudScreen.uxml" />
    <ui:Template name="PauseScreen" src="project://database/Assets/UI/Uxml/PauseScreen.uxml" />
    <ui:Template name="WinScreen"   src="project://database/Assets/UI/Uxml/WinScreen.uxml" />
    <ui:Template name="LoseScreen"  src="project://database/Assets/UI/Uxml/LoseScreen.uxml" />
    <Style src="project://database/Assets/UI/Uss/Base/Tokens.uss" />
    <Style src="project://database/Assets/UI/Uss/Base/Common.uss" />
    <ui:VisualElement name="background-graphic" picking-mode="Ignore" class="background-graphic" />
    <ui:VisualElement name="safe-area" picking-mode="Ignore" style="position: absolute; width: 100%; height: 100%;">
        <ui:Instance template="HudScreen"   name="HudScreen"   picking-mode="Ignore" style="width: 100%; height: 100%;" />
        <ui:Instance template="PauseScreen" name="PauseScreen" picking-mode="Ignore" style="position: absolute; width: 100%; height: 100%; display: none;" />
        <ui:Instance template="WinScreen"   name="WinScreen"   picking-mode="Ignore" style="position: absolute; width: 100%; height: 100%; display: none;" />
        <ui:Instance template="LoseScreen"  name="LoseScreen"  picking-mode="Ignore" style="position: absolute; width: 100%; height: 100%; display: none;" />
    </ui:VisualElement>
</ui:UXML>
```

## Where to look next

- [Docs~/UI-Notes/UI_TOOLKIT_OVERVIEW.md §4 - UXML full anatomy](../../../Docs~/UI-Notes/UI_TOOLKIT_OVERVIEW.md#4-uxml--full-anatomy) for the complete element/attribute reference, full single-screen example, and master-UXML composition example.
- [Docs~/UI-Notes/UI_TOOLKIT_OVERVIEW.md §13 - Hallucination guard](../../../Docs~/UI-Notes/UI_TOOLKIT_OVERVIEW.md#13-hallucination-guard--what-ai-agents-commonly-get-wrong) for the full uGUI-vs-UI-Toolkit do/don't list (Canvas, RectTransform, factory/traits API, etc.).

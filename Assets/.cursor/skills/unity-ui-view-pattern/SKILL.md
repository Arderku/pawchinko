---
name: unity-ui-view-pattern
description: Skeleton and lifecycle rules for writing a UIView subclass or scene-level UIManager that owns a UIDocument in Unity 6 UI Toolkit. Use whenever writing a class that subclasses UIView, owns a UIDocument, queries rootVisualElement (Q for Label, Button, or VisualElement), registers ClickEvent or ChangeEvent callbacks, or wires UI to the project's EventSystem. Prevents Awake-vs-OnEnable null refs and missing Dispose unregisters.
---

# UI Toolkit View + UIManager - Skeleton & Lifecycle

Use these skeletons whenever you write a class that owns a `UIDocument` (a scene-level `UIManager`) or a class that wraps one logical screen/panel/sub-region of that document (a `UIView`). One `UIView` subclass per `<ui:Instance>` in the master UXML. One `UIManager` per scene that has UI.

## UIView base class skeleton

A `UIView` wraps one logical screen, panel, or sub-region. It owns the references to its visual elements and the callbacks for interactions. Subclasses override `SetVisualElements` to query elements (`root.Q<Button>("foo")`), `RegisterButtonCallbacks` to wire events, and `Dispose` to unregister callbacks.

```csharp
using System;
using UnityEngine.UIElements;

namespace Pawchinko.UI
{
    public abstract class UIView : IDisposable
    {
        protected readonly VisualElement root;
        protected bool hideOnAwake = true;

        public bool IsHidden => root.style.display == DisplayStyle.None;
        public VisualElement Root => root;

        protected UIView(VisualElement root)
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            if (hideOnAwake) Hide();
            SetVisualElements();
            RegisterButtonCallbacks();
        }

        protected virtual void SetVisualElements() { }
        protected virtual void RegisterButtonCallbacks() { }

        public virtual void Show() { root.style.display = DisplayStyle.Flex; }
        public virtual void Hide() { root.style.display = DisplayStyle.None; }
        public virtual void Dispose() { }
    }
}
```

## Scene UIManager skeleton (UIDocument owner)

The `UIManager` owns the scene's `UIDocument`, constructs each `UIView`, and routes UI <-> game communication through the project's `EventSystem` (see code guide §9). Pass each `UIView` only the materialized `<ui:Instance>` element from the master UXML, never the whole document root.

```csharp
using UnityEngine;
using UnityEngine.UIElements;

namespace Pawchinko.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class UIManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EventSystem eventSystem;

        private UIDocument _document;
        private HudView _hud;
        private PauseView _pause;

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;
            _document = GetComponent<UIDocument>();

            VisualElement root = _document.rootVisualElement;
            _hud   = new HudView(root.Q<VisualElement>("HudScreen"));
            _pause = new PauseView(root.Q<VisualElement>("PauseScreen"));

            _hud.Show();

            this.eventSystem.Subscribe<BattlePausedEvent>(OnBattlePaused);

            Debug.Log("[UIManager] Initialized");
        }

        private void OnBattlePaused(BattlePausedEvent _) { _pause.Show(); }

        private void OnDestroy()
        {
            if (eventSystem != null)
            {
                eventSystem.Unsubscribe<BattlePausedEvent>(OnBattlePaused);
            }
            _hud?.Dispose();
            _pause?.Dispose();
        }
    }
}
```

> Class names like `HudView`, `PauseView`, `UIManager` are placeholders. Use the screen/scene name in real code; one `UIManager` per scene that has UI, one `UIView` subclass per `<ui:Instance>`.

## The 6 hard rules

1. **Query in `OnEnable` or `Initialize(EventSystem)`, never `Awake`.** `UIDocument.rootVisualElement` is not guaranteed to be ready in `Awake`. The `UIManager` skeleton uses `Initialize` (per code guide §7); a `MonoBehaviour` that owns a `UIDocument` outside the manager pattern should query in `OnEnable`.
2. **`Q<T>("name")` returns `null` silently.** Names are case-sensitive. Always log + null-check during development:
   ```csharp
   _resumeButton = root.Q<Button>("pause__resume-button");
   if (_resumeButton == null) Debug.LogError("[PauseView] pause__resume-button not found in UXML");
   ```
3. **Pass each `UIView` only its own subtree root** - the materialized `<ui:Instance>` element queried from the master UXML by its `name` attribute. Never pass `_document.rootVisualElement` to a view; it forces deeper queries and breaks isolation between screens.
4. **Unregister every callback in `Dispose`.** The `UIManager` calls `Dispose` on every `UIView` from `OnDestroy` (or `OnDisable` for short-lived managers). Pair every `RegisterCallback<ClickEvent>` / `RegisterValueChangedCallback` in `RegisterButtonCallbacks` with the matching `Unregister...` in `Dispose`.
5. **UI -> game communication uses `eventSystem.Publish<TEvent>`, never direct manager-to-manager calls** (project `EventSystem` pattern, code guide §9). A `UIView` never calls `BattleManager.Pause()`; it publishes `BattlePausedEvent` and the manager subscribes.
6. **Log with `[ClassName]` prefix** per project code style: `Debug.Log("[UIManager] Initialized");`, `Debug.LogError("[PauseView] pause__resume-button not found in UXML");`. Makes the source unambiguous in the Console.

## Where to look next

- [Docs~/UI-Notes/UI_TOOLKIT_OVERVIEW.md §5 - UIView base class](../../../Docs~/UI-Notes/UI_TOOLKIT_OVERVIEW.md#5-c--uiview-base-class) for the full base-class rationale.
- [Docs~/UI-Notes/UI_TOOLKIT_OVERVIEW.md §6 - UIManager pattern](../../../Docs~/UI-Notes/UI_TOOLKIT_OVERVIEW.md#6-c--uimanager-pawchinko-manager-pattern) for the manager/`Initialize(EventSystem)` integration with the project bus.
- [Docs~/UI-Notes/UI_TOOLKIT_OVERVIEW.md §7 - Concrete view example](../../../Docs~/UI-Notes/UI_TOOLKIT_OVERVIEW.md#7-c--concrete-view-example-pauseview) for the full Query -> Register -> Publish -> Dispose loop.
- [Docs~/UI-Notes/UI_TOOLKIT_OVERVIEW.md §13 - Hallucination guard](../../../Docs~/UI-Notes/UI_TOOLKIT_OVERVIEW.md#13-hallucination-guard--what-ai-agents-commonly-get-wrong) for the full uGUI-vs-UI-Toolkit do/don't list.

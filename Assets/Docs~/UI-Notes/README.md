# UI Toolkit Helper Docs

Three self-contained guides for building runtime UI in Pawchinko with Unity's UI Toolkit. Read them in order on your first pass; reference any of them as needed afterwards.

| File | Topic |
|---|---|
| [UI_TOOLKIT_OVERVIEW.md](UI_TOOLKIT_OVERVIEW.md) | Concepts, project layout, master UXML, `UIView` / `UIManager` wiring, lifecycle, scene wiring, hallucination guard |
| [UI_TOOLKIT_USS.md](UI_TOOLKIT_USS.md) | Styling, transitions, animations, BEM, tokens, theme/orientation swap, layout cookbook |
| [UI_TOOLKIT_COMPONENTS.md](UI_TOOLKIT_COMPONENTS.md) | Custom controls (`[UxmlElement]`), `BaseField<T>`, manipulators, custom drawing, runtime data binding, `ListView`, world-space anchoring, `EventSystem` glue |

> All code blocks in these docs are inline and runnable as-is. Any path that begins with `Assets/UI/...` refers to the proposed Pawchinko folder layout (see Overview §3). These docs are standalone — treat the inline code as the canonical reference for the patterns shown.

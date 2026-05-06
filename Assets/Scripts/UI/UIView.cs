using System;
using UnityEngine.UIElements;

namespace Pawchinko.UI
{
    /// <summary>
    /// Base class for one logical screen, panel, or sub-region of a UIDocument.
    /// One UIView subclass per &lt;ui:Instance&gt; in the master UXML.
    /// </summary>
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

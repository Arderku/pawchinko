using System.Collections.Generic;
using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// World-space analogue of uGUI's <c>HorizontalLayoutGroup</c>. Arranges this transform's
    /// direct children evenly along a local axis (X by default = "horizontal") with spacing,
    /// padding, and alignment - the 3D equivalent of laying out UI in a row.
    ///
    /// Everything is computed in <b>local space</b> and written to each child's
    /// <see cref="Transform.localPosition"/>, so the whole row inherits the parent's rotation
    /// and scale. That is exactly what a tilted board / angled creature stage needs: set this
    /// on the stage root, drop the creature meshes in as children, and they line up along the
    /// board no matter how it is rotated.
    ///
    /// Runs in edit mode (<see cref="ExecuteAlways"/>) so you can dial spacing in live. With
    /// <see cref="continuousUpdate"/> off it only lays out on enable / on demand
    /// (<see cref="Rebuild"/>) - cheaper for static rows.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class HorizontalLayoutGroup3D : MonoBehaviour
    {
        public enum Axis { X, Y, Z }
        public enum Alignment { Start, Center, End }

        [Header("Layout")]
        [Tooltip("Local axis the children are laid out along. X = horizontal (default), Y = vertical, Z = depth.")]
        [SerializeField] private Axis axis = Axis.X;

        [Tooltip("Gap between items. In 'center-to-center' mode this is the distance between item centers; with Use Renderer Bounds on it is the empty gap between adjacent bounding boxes.")]
        [SerializeField] private float spacing = 1f;

        [Tooltip("Where the row sits relative to this transform's origin along the layout axis. Start = grows in +axis, Center = centered on origin, End = grows in -axis.")]
        [SerializeField] private Alignment alignment = Alignment.Center;

        [Tooltip("Empty space before the first item (in local units along the axis).")]
        [SerializeField] private float paddingStart = 0f;

        [Tooltip("Empty space after the last item (in local units along the axis).")]
        [SerializeField] private float paddingEnd = 0f;

        [Tooltip("Lay the children out in reverse sibling order.")]
        [SerializeField] private bool reverseArrangement = false;

        [Tooltip("Include inactive children in the layout. Off mirrors uGUI (inactive children are skipped).")]
        [SerializeField] private bool includeInactive = false;

        [Header("Sizing")]
        [Tooltip("Off (default): treat each child as a point and place centers 'Spacing' apart - perfect for uniform items. On: measure each child's renderer bounds and pack them edge-to-edge with 'Spacing' as the gap - use for variable-width items. Assumes roughly axis-aligned children / uniform scale on this transform.")]
        [SerializeField] private bool useRendererBounds = false;

        [Header("Cross Axis")]
        [Tooltip("On: leave each child's position on the other two axes untouched. Off: snap the other two axes to 0 so every item sits exactly on the layout line.")]
        [SerializeField] private bool preserveCrossAxis = false;

        [Header("Update")]
        [Tooltip("Re-run the layout every frame (in editor and play mode). Leave on while authoring or for dynamic rows; turn off and call Rebuild() for static content.")]
        [SerializeField] private bool continuousUpdate = true;

        private readonly List<Transform> _children = new();

        public float Spacing
        {
            get => spacing;
            set { spacing = value; Rebuild(); }
        }

        private void OnEnable() => Rebuild();

        private void Update()
        {
            if (continuousUpdate) Rebuild();
        }

        /// <summary>
        /// Recomputes and applies the layout immediately. Safe to call from other systems
        /// after adding / removing / reordering children at runtime.
        /// </summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            CollectChildren();
            int n = _children.Count;
            if (n == 0) return;

            int a = (int)axis;
            float[] sizes = new float[n];
            float contentLength = 0f;
            for (int i = 0; i < n; i++)
            {
                sizes[i] = useRendererBounds ? GetChildSizeAlongAxis(_children[i], a) : 0f;
                contentLength += sizes[i];
            }
            contentLength += spacing * (n - 1);

            float total = paddingStart + contentLength + paddingEnd;
            float blockStart = alignment switch
            {
                Alignment.Center => -total * 0.5f,
                Alignment.End => -total,
                _ => 0f,
            };

            float cursor = blockStart + paddingStart;
            for (int i = 0; i < n; i++)
            {
                float center = cursor + sizes[i] * 0.5f;

                Transform child = _children[i];
                Vector3 local = preserveCrossAxis ? child.localPosition : Vector3.zero;
                local[a] = center;
                child.localPosition = local;

                cursor += sizes[i] + spacing;
            }
        }

        private void CollectChildren()
        {
            _children.Clear();
            int count = transform.childCount;
            for (int i = 0; i < count; i++)
            {
                Transform child = transform.GetChild(i);
                if (!includeInactive && !child.gameObject.activeSelf) continue;
                _children.Add(child);
            }

            if (reverseArrangement) _children.Reverse();
        }

        /// <summary>
        /// Size of a child along the layout axis in this transform's local units, derived from
        /// the combined world bounds of its renderers. Returns 0 when the child has no
        /// renderers (it is then treated as a point).
        /// </summary>
        private float GetChildSizeAlongAxis(Transform child, int axisIndex)
        {
            var renderers = child.GetComponentsInChildren<Renderer>(includeInactive);
            if (renderers.Length == 0) return 0f;

            Bounds world = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) world.Encapsulate(renderers[i].bounds);

            // World extent projected onto the world-space direction of our local layout axis.
            Vector3 axisUnit = Vector3.zero;
            axisUnit[axisIndex] = 1f;
            Vector3 worldDir = transform.TransformDirection(axisUnit);
            float worldHalf = Mathf.Abs(worldDir.x) * world.extents.x
                            + Mathf.Abs(worldDir.y) * world.extents.y
                            + Mathf.Abs(worldDir.z) * world.extents.z;
            float worldSize = worldHalf * 2f;

            // Convert the world size back to local units along the axis.
            float scale = Mathf.Abs(transform.lossyScale[axisIndex]);
            return scale > 1e-5f ? worldSize / scale : worldSize;
        }
    }
}

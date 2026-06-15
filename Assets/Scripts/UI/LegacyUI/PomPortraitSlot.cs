using UnityEngine;
using UnityEngine.UI;

namespace Pawchinko
{
    /// <summary>
    /// One Pom portrait. Owns the <see cref="spawnAnchor"/> where the species prefab is
    /// instantiated and the <see cref="cameraAnchor"/> that tells
    /// <see cref="PomPortraitStage"/> where the shared portrait camera should sit when it
    /// renders this slot. The matching UGUI <see cref="RawImage"/> in the
    /// <see cref="BattlePomCardView"/> is wired by the stage to a single shared atlas
    /// RenderTexture with a per-slot <see cref="RawImage.uvRect"/> slice. Pure view: never
    /// mutates Pom data and never looks at input.
    ///
    /// Lifecycle: <see cref="Awake"/> hides the target image until a Pom is bound.
    /// <see cref="PomPortraitStage.Awake"/> calls <see cref="ConfigureFromStage"/> to wire
    /// the shared atlas texture + UV slice. <see cref="SetPom"/> instantiates the prefab
    /// under <see cref="spawnAnchor"/> and forces the whole subtree onto the PomPortrait
    /// layer so the portrait camera (and only the portrait camera) sees it.
    /// <see cref="Clear"/> destroys the instance and hides the target image.
    /// </summary>
    public class PomPortraitSlot : MonoBehaviour
    {
        public const string PortraitLayerName = "PomPortrait";

        [Header("Stage Bindings")]
        [Tooltip("Empty Transform under which the Pom prefab is spawned. Pose this to frame the Pom inside the portrait.")]
        [SerializeField] private Transform spawnAnchor;
        [Tooltip("Empty Transform whose pose the shared portrait camera teleports to before rendering this slot. The camera looks down its -Z, so place this offset from the spawnAnchor along -Z and rotate to taste.")]
        [SerializeField] private Transform cameraAnchor;
        [Tooltip("RawImage in the card that displays this slot's atlas slice.")]
        [SerializeField] private RawImage targetImage;

        private GameObject _spawned;
        private int _portraitLayer = -1;

        public PomInstance CurrentInstance { get; private set; }
        public Transform CameraAnchor => cameraAnchor;
        public RawImage TargetImage => targetImage;

        private void Awake()
        {
            _portraitLayer = LayerMask.NameToLayer(PortraitLayerName);
            if (_portraitLayer < 0)
            {
                Debug.LogError($"[PomPortraitSlot] Layer '{PortraitLayerName}' is missing. Add it in Project Settings > Tags and Layers.");
            }
            HideImage();
        }

        /// <summary>
        /// Wires this slot's <see cref="RawImage"/> to the shared atlas texture and tells it
        /// which slice of that atlas belongs to this slot. Called once by
        /// <see cref="PomPortraitStage"/> during its own Awake.
        /// </summary>
        public void ConfigureFromStage(Texture atlasTexture, Rect uvRect)
        {
            if (targetImage == null) return;
            targetImage.texture = atlasTexture;
            targetImage.uvRect = uvRect;
        }

        /// <summary>
        /// Spawn (or replace) the Pom prefab for this slot. Pass null to clear.
        /// </summary>
        public void SetPom(PomInstance instance)
        {
            DestroySpawned();

            CurrentInstance = instance;
            var prefab = instance?.data?.PortraitPrefab;
            if (prefab == null)
            {
                HideImage();
                return;
            }

            _spawned = Instantiate(prefab, spawnAnchor);
            _spawned.transform.localPosition = Vector3.zero;
            _spawned.transform.localRotation = Quaternion.identity;
            _spawned.name = $"Portrait_{instance.data.Id}";
            if (_portraitLayer >= 0) SetLayerRecursively(_spawned, _portraitLayer);

            ShowImage();
        }

        /// <summary>Clears the slot (no Pom shown).</summary>
        public void Clear()
        {
            DestroySpawned();
            CurrentInstance = null;
            HideImage();
        }

        private void DestroySpawned()
        {
            if (_spawned == null) return;
            if (Application.isPlaying) Destroy(_spawned);
            else DestroyImmediate(_spawned);
            _spawned = null;
        }

        private void ShowImage()
        {
            if (targetImage != null) targetImage.enabled = true;
        }

        private void HideImage()
        {
            if (targetImage != null) targetImage.enabled = false;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            var t = root.transform;
            for (int i = 0; i < t.childCount; i++) SetLayerRecursively(t.GetChild(i).gameObject, layer);
        }

        private void OnDestroy()
        {
            DestroySpawned();
        }
    }
}

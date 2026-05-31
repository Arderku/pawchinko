using UnityEngine;
using UnityEngine.UI;

namespace Pawchinko
{
    /// <summary>
    /// One live 3D Pom portrait. Owns its <see cref="Camera"/> + <see cref="RenderTexture"/>
    /// and the spawned <see cref="PomData.PortraitPrefab"/> instance; pipes the render texture
    /// into the matching UGUI <see cref="RawImage"/> on a <see cref="BattlePomCardView"/>.
    /// Pure view: never mutates Pom data and never looks at input.
    ///
    /// Lifecycle: <see cref="Awake"/> creates the render texture and wires it to the camera
    /// and the target image. <see cref="SetPom"/> instantiates the prefab under
    /// <see cref="spawnAnchor"/> and forces the whole subtree onto the PomPortrait layer so the
    /// portrait camera (and only the portrait camera) sees it. <see cref="Clear"/> destroys
    /// the instance and hides the target image. <see cref="OnDestroy"/> releases the RT.
    /// </summary>
    public class PomPortraitSlot : MonoBehaviour
    {
        public const string PortraitLayerName = "PomPortrait";

        [Header("Rendering")]
        [Tooltip("Camera dedicated to this slot. Culling mask must be PomPortrait only.")]
        [SerializeField] private Camera portraitCamera;
        [Tooltip("Empty Transform under which the Pom prefab is spawned. The camera should be framed on this anchor.")]
        [SerializeField] private Transform spawnAnchor;
        [Tooltip("RawImage in the card that displays this slot's render texture.")]
        [SerializeField] private RawImage targetImage;
        [Tooltip("Resolution of the render texture. 128x128 is plenty for a 58px card slot.")]
        [SerializeField] private Vector2Int renderSize = new(128, 128);

        private RenderTexture _renderTexture;
        private GameObject _spawned;
        private int _portraitLayer = -1;

        public PomInstance CurrentInstance { get; private set; }

        private void Awake()
        {
            _portraitLayer = LayerMask.NameToLayer(PortraitLayerName);
            if (_portraitLayer < 0)
            {
                Debug.LogError($"[PomPortraitSlot] Layer '{PortraitLayerName}' is missing. Add it in Project Settings > Tags and Layers.");
            }

            CreateRenderTexture();
            HideImage();
        }

        private void CreateRenderTexture()
        {
            if (renderSize.x <= 0 || renderSize.y <= 0) renderSize = new Vector2Int(128, 128);
            _renderTexture = new RenderTexture(renderSize.x, renderSize.y, 24)
            {
                name = $"RT_{name}",
                antiAliasing = 1,
                useMipMap = false,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            _renderTexture.Create();

            if (portraitCamera != null) portraitCamera.targetTexture = _renderTexture;
            if (targetImage != null) targetImage.texture = _renderTexture;
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
            if (_renderTexture != null)
            {
                if (portraitCamera != null && portraitCamera.targetTexture == _renderTexture) portraitCamera.targetTexture = null;
                if (targetImage != null && targetImage.texture == _renderTexture) targetImage.texture = null;
                _renderTexture.Release();
                Destroy(_renderTexture);
                _renderTexture = null;
            }
        }
    }
}

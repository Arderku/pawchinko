using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Overworld trigger volume that publishes EncounterTriggeredEvent when something on
    /// the player layer freshly enters it. After firing, the zone disarms; the actor must
    /// leave the volume before it can fire again. Identification is layer-based for zero
    /// per-collision string lookup; configure the Inspector LayerMask to the Player layer.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EncounterZone : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Layers that count as the player. Defaults to the 'Player' layer.")]
        [SerializeField] private LayerMask playerLayers = 1 << 12;
        [Tooltip("If true, this zone fires only once per scene load and never re-arms.")]
        [SerializeField] private bool oneShot = false;

        private EventSystem _eventSystem;
        private Collider _collider;
        private bool _armed = true;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            if (!_collider.isTrigger)
            {
                Debug.LogWarning($"[EncounterZone] Collider on {name} is not a trigger; enabling isTrigger.");
                _collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_armed) return;
            if (!IsPlayerLayer(other)) return;

            TryResolveBus();
            if (_eventSystem == null)
            {
                Debug.LogError("[EncounterZone] EventSystem unavailable; start from Boot.unity.");
                return;
            }

            _eventSystem.Publish(new EncounterTriggeredEvent());
            _armed = false;
        }

        private void OnTriggerExit(Collider other)
        {
            if (oneShot) return;
            if (!IsPlayerLayer(other)) return;
            _armed = true;
        }

        private bool IsPlayerLayer(Collider other)
        {
            if (other == null) return false;
            return (playerLayers.value & (1 << other.gameObject.layer)) != 0;
        }

        private void TryResolveBus()
        {
            if (_eventSystem != null) return;
            if (GameManager.Instance == null) return;
            _eventSystem = GameManager.Instance.EventSystem;
        }
    }
}

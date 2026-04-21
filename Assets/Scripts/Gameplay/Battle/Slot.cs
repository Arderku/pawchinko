using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Trigger volume at the bottom of a board. Reports the first ball that enters via
    /// Ball.HandleSlotEntered, which forwards to BallManager via the Ball.Settled event.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Slot : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private int slotIndex;

        public int SlotIndex => slotIndex;

        /// <summary>
        /// Sets the slot's ordinal. Called once during board construction.
        /// </summary>
        public void SetSlotIndex(int index)
        {
            slotIndex = index;
        }

        private void OnTriggerEnter(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (rb == null) return;
            var ball = rb.GetComponent<Ball>();
            if (ball != null) ball.HandleSlotEntered(this);
        }
    }
}

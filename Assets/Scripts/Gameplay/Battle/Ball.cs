using System;
using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Physics-driven ball. Rigidbody + SphereCollider live alongside this script. The ball
    /// raises Settled when it enters a slot trigger, after which BallManager despawns it.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class Ball : MonoBehaviour
    {
        [Header("Runtime State")]
        [SerializeField] private int id;
        [SerializeField] private Side side;

        public int Id => id;
        public Side Side => side;
        public Rigidbody Body { get; private set; }

        private bool _hasSettled;

        public event Action<Ball, Slot> Settled;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// Initialises the ball with its id and the side that spawned it. Called by BallSpawner.
        /// </summary>
        public void Init(int id, Side side)
        {
            this.id = id;
            this.side = side;
            _hasSettled = false;
        }

        /// <summary>
        /// Called by Slot.OnTriggerEnter the first time the ball enters any slot.
        /// </summary>
        public void HandleSlotEntered(Slot slot)
        {
            if (_hasSettled) return;
            _hasSettled = true;
            Settled?.Invoke(this, slot);
            Destroy(gameObject, 0.5f);
        }
    }
}

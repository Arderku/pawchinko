using System;
using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Physics-driven ball. Rigidbody + SphereCollider live alongside this script. The ball
    /// raises Settled when it enters a slot trigger, after which BallManager despawns it.
    ///
    /// A ball has no behaviour - only visuals and a <see cref="Type"/> inherited from its
    /// source Pom's primary type (Section 11). All scoring / ability logic stays on the Pom
    /// and the systems that read events.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class Ball : MonoBehaviour
    {
        [Header("Runtime State")]
        [SerializeField] private int id;
        [SerializeField] private Side side;
        [SerializeField] private PomType type;

        public int Id => id;
        public Side Side => side;
        public PomType Type => type;
        public PomInstance SourcePom { get; private set; }
        public Rigidbody Body { get; private set; }

        private bool _hasSettled;

        public event Action<Ball, Slot> Settled;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// Initialises the ball with its id, the side that spawned it, and the Pom that owns
        /// it. The source Pom carries through to BallSettledEvent so scoring can apply its
        /// stats (Power, etc.) when this ball lands. <see cref="Type"/> is set from the
        /// source Pom's primary type so visuals can read it without poking into the Pom.
        /// </summary>
        public void Init(int id, Side side, PomInstance sourcePom)
        {
            this.id = id;
            this.side = side;
            this.type = sourcePom != null && sourcePom.data != null ? sourcePom.data.PrimaryType : default;
            SourcePom = sourcePom;
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

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
    ///
    /// The ball is a plain Unity physics body: gravity pulls it down, peg colliders bounce it
    /// around, and the board's physical geometry keeps it in the play plane. The board has a
    /// back panel and an invisible front glass (the Wall layer), so the ball stays between
    /// them naturally - no Rigidbody position constraint and no scripted corrective forces are
    /// applied. The tilted board means "down the slope" is a real 3D motion the physics solves
    /// on its own, so freezing a world axis or pushing the ball around in code would only fight
    /// the geometry.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class Ball : MonoBehaviour
    {
        [Header("Runtime State")]
        [SerializeField] private int id;
        [SerializeField] private Side side;
        [SerializeField] private PomType type;

        [Header("Labels")]
        [Tooltip("Optional 3D power label baked under the ball prefab. Updated once at Init from the source Pom's Power stat.")]
        [SerializeField] private BallPowerLabel powerLabel;

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
        /// Initialises the ball with its id, the side that spawned it, the resolved ball
        /// <paramref name="type"/>, and the Pom that owns it. The source Pom carries through to
        /// BallSettledEvent so scoring can apply its stats (Power, etc.) when this ball lands.
        /// <paramref name="type"/> is rolled by the spawner (single-type Pom -> its type;
        /// dual-type Pom -> 50/50) and selects which per-type prefab was instantiated, so it
        /// always matches this ball's visuals / PhysicsMaterial.
        /// </summary>
        public void Init(int id, Side side, PomType type, PomInstance sourcePom)
        {
            this.id = id;
            this.side = side;
            this.type = type;
            SourcePom = sourcePom;
            _hasSettled = false;

            // Power readout floats above the ball. Hidden automatically for 1× balls.
            if (powerLabel != null)
            {
                float power = sourcePom != null && sourcePom.data != null && sourcePom.data.BaseStats != null
                    ? sourcePom.data.BaseStats.power
                    : 1f;
                powerLabel.SetPower(power);
            }
        }

        /// <summary>
        /// Called by Slot.OnTriggerEnter the first time the ball enters any slot. Immediately
        /// stops the ball (kinematic + collider off + renderer off) so it visually disappears
        /// the instant it enters the bucket, instead of floating past during a despawn delay.
        /// One physics frame is reserved before destruction so listeners on the Settled event
        /// can still read the ball's transform / Rigidbody state if needed.
        /// </summary>
        public void HandleSlotEntered(Slot slot)
        {
            if (_hasSettled) return;
            _hasSettled = true;

            if (Body != null)
            {
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
                Body.isKinematic = true;
            }
            foreach (var col in GetComponents<Collider>()) col.enabled = false;
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = false;

            Settled?.Invoke(this, slot);
            Destroy(gameObject);
        }
    }
}

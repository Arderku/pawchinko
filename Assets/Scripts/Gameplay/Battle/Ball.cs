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
    /// Movement is locked to a single X-Y plane via Rigidbody Z-position constraint on the
    /// prefab. This mirrors a real plinko board where the ball lives between front glass
    /// and back felt, and guarantees the ball cannot drift in/out of the playable plane.
    ///
    /// Stuck-prevention strategy:
    /// 1. <b>Peg imperfection.</b> Each peg is jittered in position (±few mm) and radius
    ///    (±2.5%) by the ApplyBoardImperfection editor menu. Real pachinko boards work
    ///    the same way - imperfect pins make "balanced on top" geometrically impossible.
    /// 2. <b>Optional lateral micro-gravity.</b> A tiny X-axis acceleration can be added
    ///    every tick to bias the ball off perfect peg tops; off by default since the peg
    ///    jitter alone is enough.
    /// 3. <b>Watchdog (safety net).</b> Position-based stall detector that nudges + (after
    ///    repeated failures) hard-offsets a still-stuck ball.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class Ball : MonoBehaviour
    {
        [Header("Runtime State")]
        [SerializeField] private int id;
        [SerializeField] private Side side;
        [SerializeField] private PomType type;

        [Header("Board Feel")]
        [Tooltip("Constant horizontal acceleration (m/s^2) added every tick to break peg-top equilibria. 0 disables. Sign alternates per ball ID so net X drift on the board averages to zero.")]
        [SerializeField] private float lateralGravity = 0f;

        [Header("Anti-Stuck (Safety Net)")]
        [Tooltip("Distance (m) the ball must travel during the watchdog window or it is considered stalled.")]
        [SerializeField] private float stuckDistanceThreshold = 0.02f;
        [Tooltip("How long the ball must fail to move stuckDistanceThreshold before we nudge it.")]
        [SerializeField] private float stuckTimeBeforeNudge = 0.35f;
        [Tooltip("Base horizontal impulse (impulse units) applied to dislodge a stuck ball.")]
        [SerializeField] private float nudgeImpulse = 0.18f;
        [Tooltip("Hard ceiling on the nudge impulse so the ball never teleports off the board.")]
        [SerializeField] private float maxNudgeImpulse = 0.8f;
        [Tooltip("After this many consecutive failed nudges, also offset the transform sideways to physically dislodge the ball.")]
        [SerializeField] private int hardDislodgeAfterNudges = 3;
        [Tooltip("Sideways offset (m) applied during hard dislodge.")]
        [SerializeField] private float hardDislodgeOffset = 0.04f;

        [Header("Labels")]
        [Tooltip("Optional 3D power label baked under the ball prefab. Updated once at Init from the source Pom's Power stat.")]
        [SerializeField] private BallPowerLabel powerLabel;

        public int Id => id;
        public Side Side => side;
        public PomType Type => type;
        public PomInstance SourcePom { get; private set; }
        public Rigidbody Body { get; private set; }

        private bool _hasSettled;
        private float _stallTime;
        private int _nudgeCount;
        private Vector3 _lastSampledPos;
        private bool _hasSampledPos;

        public event Action<Ball, Slot> Settled;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            // Disable Rigidbody auto-sleep so the watchdog keeps seeing a real velocity even
            // when the ball lands perfectly on top of a peg. Without this, Unity's solver puts
            // the body to sleep and the stall is invisible to us.
            if (Body != null) Body.sleepThreshold = 0f;
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
            _stallTime = 0f;
            _nudgeCount = 0;
            _hasSampledPos = false;

            // Power readout floats above the ball. Hidden automatically for 1× balls.
            if (powerLabel != null)
            {
                float power = sourcePom != null && sourcePom.data != null && sourcePom.data.BaseStats != null
                    ? sourcePom.data.BaseStats.power
                    : 1f;
                powerLabel.SetPower(power);
            }
        }

        private void FixedUpdate()
        {
            if (_hasSettled || Body == null) return;

            // Step 1: lateral micro-gravity. Sign is deterministic per ball ID so the bias
            // is reproducible (same ball, same drift) and balanced across many balls (even
            // ids drift one way, odd the other) so neither side wins a free centre push.
            if (lateralGravity > 0f)
            {
                float sign = (id & 1) == 0 ? 1f : -1f;
                Body.AddForce(new Vector3(sign * lateralGravity, 0f, 0f), ForceMode.Acceleration);
            }

            Vector3 pos = transform.position;
            if (!_hasSampledPos)
            {
                _lastSampledPos = pos;
                _hasSampledPos = true;
                return;
            }

            // Position-based stall detection. Velocity-based misses balls that oscillate in
            // a wedge between two pegs - they have non-zero velocity averaging to zero net
            // motion. Measuring actual displacement catches both perfect rests AND wedges.
            float moved = (pos - _lastSampledPos).magnitude;
            if (moved < stuckDistanceThreshold)
            {
                _stallTime += Time.fixedDeltaTime;
                if (_stallTime >= stuckTimeBeforeNudge)
                {
                    ApplyAntiStuckNudge();
                    _stallTime = 0f;
                    _lastSampledPos = transform.position;
                }
            }
            else
            {
                _stallTime = 0f;
                _nudgeCount = 0;
                _lastSampledPos = pos;
            }
        }

        /// <summary>
        /// Pushes the ball sideways with an impulse + spin so it falls off whichever peg or
        /// wedge it has come to rest on. Strength ramps per consecutive nudge to clear deep
        /// equilibria, capped at <see cref="maxNudgeImpulse"/>. After
        /// <see cref="hardDislodgeAfterNudges"/> failed nudges in a row the transform is
        /// also offset sideways so the ball physically clears the geometry, since impulses
        /// alone can't dislodge a perfectly wedged ball where contact normals fully cancel.
        /// </summary>
        private void ApplyAntiStuckNudge()
        {
            _nudgeCount++;
            float strength = Mathf.Min(nudgeImpulse * (1f + 0.5f * (_nudgeCount - 1)), maxNudgeImpulse);
            float dirX = UnityEngine.Random.value < 0.5f ? -1f : 1f;

            // Reset velocity so successive nudges don't fight a tiny residual oscillation.
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;

            // Sideways shove + small downward bias so the ball resumes falling toward the slots.
            Body.AddForce(new Vector3(dirX * strength, -strength * 0.3f, 0f), ForceMode.Impulse);
            // Spin around the depth axis (Z, board normal) so the ball rolls off rather than re-balancing.
            Body.AddTorque(new Vector3(0f, 0f, dirX * strength * 8f), ForceMode.Impulse);

            if (_nudgeCount >= hardDislodgeAfterNudges)
            {
                // Last resort: physically shift the ball off its perch. Tiny offset avoids
                // visible teleport but guarantees the contact configuration changes so the
                // next physics step can resolve into a real fall.
                var p = transform.position;
                p.x += dirX * hardDislodgeOffset;
                p.y += hardDislodgeOffset * 0.5f;
                transform.position = p;
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

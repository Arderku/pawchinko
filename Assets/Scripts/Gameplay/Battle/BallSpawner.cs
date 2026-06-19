using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Wave-based ball spawner. Instead of dumping every requested ball at a single point
    /// (which causes overlapping spawns and immediate ball-vs-ball collisions), this spawner
    /// owns a row of <see cref="zoneCount"/> invisible spawn zones across the top of a board
    /// and drains a request queue across them: each zone spawns at most one ball at a time
    /// and only releases its next ball once the previous one has dropped clear of the zone.
    /// The wave pattern the player sees ("first six fall, next six follow") is a natural
    /// emergent property; no central wave scheduler is needed.
    ///
    /// Callers (BallManager) enqueue with <see cref="Enqueue"/> and listen to
    /// <see cref="BallSpawned"/> to wire each new ball into per-ball systems (settle event,
    /// scoring, etc.).
    /// </summary>
    public class BallSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [Tooltip("Shared type -> ball-prefab map. Both sides reference the same asset; the ball's rolled PomType picks the prefab (and therefore its visuals + PhysicsMaterial).")]
        [SerializeField] private BallLibrary ballLibrary;

        [Header("References")]
        [Tooltip("Center of the spawn row. The N zones are spread evenly to the left and right of this transform.")]
        [SerializeField] private Transform spawnPoint;
        [Tooltip("Parent transform that all spawned balls are nested under, so the scene stays tidy.")]
        [SerializeField] private Transform ballContainer;

        [Header("Spawn Row")]
        [Tooltip("How many invisible spawn zones the row contains. Each zone spawns one ball at a time.")]
        [Min(1)]
        [SerializeField] private int zoneCount = 6;
        [Tooltip("Half-width of the spawn row, in metres. Zones are evenly distributed across [-half, +half] along the X axis around spawnPoint.")]
        [Min(0f)]
        [SerializeField] private float zoneSpreadHalfWidth = 1.2f;
        [Tooltip("How far (m) below a zone's centre the ball must drop before that zone is considered clear and may spawn its next ball.")]
        [Min(0f)]
        [SerializeField] private float zoneClearMargin = 0.55f;
        [Tooltip("Random X jitter (m) applied inside each zone so balls in the same wave don't look mechanically aligned.")]
        [Min(0f)]
        [SerializeField] private float zoneXJitter = 0.04f;
        [Tooltip("Random Z jitter (m) per ball. Combined with the Ball prefab's now-unfrozen Z, this breaks the 'all balls in the same plane' symmetry that creates stuck equilibria on peg tops.")]
        [Min(0f)]
        [SerializeField] private float zoneZJitter = 0.03f;
        [Tooltip("Minimum interval (s) between any two spawns from this spawner. Keeps balls from spawning on the same physics tick.")]
        [Min(0f)]
        [SerializeField] private float minIntervalBetweenSpawns = 0.04f;

        [Header("Per-Ball")]
        [Tooltip("Small random torque applied to each spawned ball so identical drops don't look cloned.")]
        [SerializeField] private Vector3 spawnTorqueJitter = new(0.5f, 0f, 0.5f);

        private struct PendingSpawn
        {
            public int Id;
            public Side Side;
            public PomType Type;
            public PomInstance SourcePom;
        }

        private readonly Queue<PendingSpawn> _pending = new();
        private Ball[] _zoneLastBall;
        private int _nextZoneIndex;
        private float _nextSpawnEarliest;

        /// <summary>Raised once for each ball this spawner actually instantiates.</summary>
        public event Action<Ball> BallSpawned;

        /// <summary>Total balls currently waiting in the queue, not yet instantiated.</summary>
        public int PendingCount => _pending.Count;

        private void Awake()
        {
            _zoneLastBall = new Ball[Mathf.Max(1, zoneCount)];
        }

        /// <summary>
        /// Queues a ball for spawning. The actual instantiation happens later from
        /// <see cref="Update"/> as soon as a zone is free.
        /// </summary>
        public void Enqueue(int id, Side side, PomInstance sourcePom)
        {
            if (ballLibrary == null)
            {
                Debug.LogError("[BallSpawner] ballLibrary not assigned!");
                return;
            }
            // Roll the ball's type now so it is fixed when queued: single-type Pom -> its type,
            // dual-type Pom -> a fresh 50/50 per ball.
            PomType type = PomBallType.Roll(sourcePom);
            _pending.Enqueue(new PendingSpawn { Id = id, Side = side, Type = type, SourcePom = sourcePom });
        }

        private void Update()
        {
            if (_pending.Count == 0) return;
            if (Time.time < _nextSpawnEarliest) return;
            EnsureZoneBuffer();

            int zones = _zoneLastBall.Length;
            // One pass round-robin through zones; spawn into every free one we encounter.
            // We do not loop "forever" within a single frame even if many zones are free
            // because minIntervalBetweenSpawns gates successive spawns to keep them from
            // landing on the same physics tick.
            for (int step = 0; step < zones && _pending.Count > 0; step++)
            {
                int zone = (_nextZoneIndex + step) % zones;
                if (!IsZoneClear(zone)) continue;

                var req = _pending.Dequeue();
                var ball = SpawnAtZone(zone, req);
                if (ball != null)
                {
                    _zoneLastBall[zone] = ball;
                    BallSpawned?.Invoke(ball);
                    _nextSpawnEarliest = Time.time + minIntervalBetweenSpawns;
                    _nextZoneIndex = (zone + 1) % zones;
                    // Only spawn one per frame to respect the interval; rest happens next frame.
                    return;
                }
            }
        }

        private void EnsureZoneBuffer()
        {
            int n = Mathf.Max(1, zoneCount);
            if (_zoneLastBall == null || _zoneLastBall.Length != n)
            {
                _zoneLastBall = new Ball[n];
            }
        }

        private bool IsZoneClear(int zoneIndex)
        {
            var last = _zoneLastBall[zoneIndex];
            if (last == null) return true; // never spawned or destroyed
            // Ball must have dropped clear of its zone's vertical band.
            float zoneCenterY = GetZoneCenter(zoneIndex).y;
            return last.transform.position.y < zoneCenterY - zoneClearMargin;
        }

        private Vector3 GetZoneCenter(int zoneIndex)
        {
            Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position;
            int zones = Mathf.Max(1, zoneCount);
            if (zones == 1) return origin;
            float t = (zones == 1) ? 0.5f : (float)zoneIndex / (zones - 1);
            float x = origin.x - zoneSpreadHalfWidth + 2f * zoneSpreadHalfWidth * t;
            return new Vector3(x, origin.y, origin.z);
        }

        private Ball SpawnAtZone(int zoneIndex, PendingSpawn req)
        {
            Ball prefab = ballLibrary != null ? ballLibrary.GetPrefab(req.Type) : null;
            if (prefab == null)
            {
                Debug.LogError($"[BallSpawner] No ball prefab for type {req.Type} in ballLibrary (and no fallback).");
                return null;
            }

            Vector3 pos = GetZoneCenter(zoneIndex);
            pos.x += UnityEngine.Random.Range(-zoneXJitter, zoneXJitter);
            pos.z += UnityEngine.Random.Range(-zoneZJitter, zoneZJitter);

            Ball ball = Instantiate(prefab, pos, Quaternion.identity, ballContainer);
            ball.Init(req.Id, req.Side, req.Type, req.SourcePom);

            if (ball.Body != null)
            {
                ball.Body.maxAngularVelocity = 50f;
                ball.Body.AddTorque(new Vector3(
                    UnityEngine.Random.Range(-spawnTorqueJitter.x, spawnTorqueJitter.x),
                    UnityEngine.Random.Range(-spawnTorqueJitter.y, spawnTorqueJitter.y),
                    UnityEngine.Random.Range(-spawnTorqueJitter.z, spawnTorqueJitter.z)
                ), ForceMode.Impulse);
            }

            return ball;
        }

        private void OnDrawGizmos()
        {
            // Visualise the zone row in the editor without polluting the scene with GameObjects.
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.55f);
            int zones = Mathf.Max(1, zoneCount);
            Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position;
            for (int i = 0; i < zones; i++)
            {
                float t = (zones == 1) ? 0.5f : (float)i / (zones - 1);
                float x = origin.x - zoneSpreadHalfWidth + 2f * zoneSpreadHalfWidth * t;
                var p = new Vector3(x, origin.y, origin.z);
                Gizmos.DrawWireSphere(p, 0.12f);
                Gizmos.DrawLine(p, p + new Vector3(0f, -zoneClearMargin, 0f));
            }
        }
    }
}

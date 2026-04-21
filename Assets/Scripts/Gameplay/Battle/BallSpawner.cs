using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Instantiates Ball prefabs at a configured spawn point with a small random horizontal
    /// jitter and a tiny random torque so successive drops don't look cloned.
    /// </summary>
    public class BallSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private Ball ballPrefab;

        [Header("References")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform ballContainer;
        [SerializeField] private Material ballMaterialOverride;

        [Header("Settings")]
        [SerializeField] private float spawnXJitter = 0.01f;
        [SerializeField] private Vector3 spawnTorqueJitter = new(0.5f, 0f, 0.5f);

        /// <summary>
        /// Spawns a single ball at the spawn point, applies jitter, and assigns id/side.
        /// </summary>
        public Ball Spawn(int id, Side side)
        {
            if (ballPrefab == null)
            {
                Debug.LogError("[BallSpawner] ballPrefab not assigned!");
                return null;
            }

            Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position;
            Vector3 pos = origin + new Vector3(Random.Range(-spawnXJitter, spawnXJitter), 0f, 0f);

            Ball ball = Instantiate(ballPrefab, pos, Quaternion.identity, ballContainer);
            ball.Init(id, side);

            if (ballMaterialOverride != null)
            {
                var renderer = ball.GetComponent<Renderer>();
                if (renderer != null) renderer.sharedMaterial = ballMaterialOverride;
            }

            if (ball.Body != null)
            {
                ball.Body.maxAngularVelocity = 50f;
                ball.Body.AddTorque(new Vector3(
                    Random.Range(-spawnTorqueJitter.x, spawnTorqueJitter.x),
                    Random.Range(-spawnTorqueJitter.y, spawnTorqueJitter.y),
                    Random.Range(-spawnTorqueJitter.z, spawnTorqueJitter.z)
                ), ForceMode.Impulse);
            }

            return ball;
        }
    }
}

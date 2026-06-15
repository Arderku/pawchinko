using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// World-space label helper. Two independent jobs, each opt-in:
    /// <list type="bullet">
    /// <item><b>Face the camera.</b> Every <see cref="LateUpdate"/> the transform's rotation is
    /// snapped to the camera's rotation, so the text always reads upright.</item>
    /// <item><b>Float above a target.</b> If <see cref="followTarget"/> is set, the transform
    /// teleports to <c>followTarget.position + Vector3.up * worldYOffset</c> in world space.
    /// World-up (not parent-up) is used on purpose: it decouples the label from any rotation on
    /// the followed transform, which is exactly what a spinning Pachinko ball needs.</item>
    /// </list>
    /// Used by <see cref="BallPowerLabel"/> so the "2x" floats steadily above the ball even as
    /// the ball spins and bounces.
    /// </summary>
    [ExecuteAlways]
    public class CameraFacingBillboard : MonoBehaviour
    {
        [Tooltip("Camera to face. If null, uses Camera.main.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("If assigned, the label parks itself in world space directly above this transform - decoupled from any rotation it has.")]
        [SerializeField] private Transform followTarget;

        [Tooltip("World-space Y offset above followTarget. Y is world Y, not parent Y, so the label never tilts with a spinning ball.")]
        [SerializeField] private float worldYOffset = 0.6f;

        private void LateUpdate()
        {
            if (followTarget != null)
            {
                transform.position = followTarget.position + Vector3.up * worldYOffset;
            }

            var cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null) return;
            transform.rotation = cam.transform.rotation;
        }
    }
}

using UnityEngine;

namespace Vortex.CameraSystem
{
    public class TopDownCameraFollow : MonoBehaviour
    {
        [Header("Target & Offsets")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 16f, -9f);
        [SerializeField] private Vector3 fixedRotation = new Vector3(60f, 0f, 0f);

        [Header("Smoothing")]
        [SerializeField] private float smoothTime = 0.12f;

        private Vector3 _currentVelocity = Vector3.zero;

        private void Start()
        {
            transform.rotation = Quaternion.Euler(fixedRotation);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 targetPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _currentVelocity, smoothTime);
            transform.rotation = Quaternion.Euler(fixedRotation);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                transform.position = target.position + offset;
                transform.rotation = Quaternion.Euler(fixedRotation);
            }
        }
    }
}

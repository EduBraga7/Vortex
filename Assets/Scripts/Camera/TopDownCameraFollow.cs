using UnityEngine;

namespace Vortex.CameraSystem
{
    public class TopDownCameraFollow : MonoBehaviour
    {
        public static TopDownCameraFollow Instance { get; private set; }

        [Header("Target & Offsets")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 16f, -9f);
        [SerializeField] private Vector3 fixedRotation = new Vector3(60f, 0f, 0f);

        [Header("Smoothing")]
        [SerializeField] private float smoothTime = 0.12f;

        private Vector3 _currentVelocity = Vector3.zero;
        private float _shakeTimer;
        private float _shakeMagnitude;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            transform.rotation = Quaternion.Euler(fixedRotation);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 targetPosition = target.position + offset;

            if (_shakeTimer > 0f)
            {
                Vector3 shakeOffset = Random.insideUnitSphere * _shakeMagnitude;
                shakeOffset.y *= 0.25f; // Reduz componente vertical para evitar artefatos de clipping
                targetPosition += shakeOffset;

                _shakeTimer -= Time.deltaTime;
                if (_shakeTimer <= 0f)
                {
                    _shakeMagnitude = 0f;
                }
            }

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _currentVelocity, smoothTime);
            transform.rotation = Quaternion.Euler(fixedRotation);
        }

        /// <summary>
        /// Aciona screen shake procedural suavizado.
        /// </summary>
        public void TriggerShake(float duration, float magnitude)
        {
            _shakeTimer = Mathf.Max(_shakeTimer, duration);
            _shakeMagnitude = Mathf.Max(_shakeMagnitude, magnitude);
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


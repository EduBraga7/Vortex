using UnityEngine;
using Vortex.Player;

namespace Vortex.Enemies
{
    /// <summary>
    /// IA básica para o Swarmer: persegue o jogador no plano XZ com altura Y fixa,
    /// aplica dano por contato e executa rotação contínua procedural.
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        [Header("Movimentação")]
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float fixedHeight = 0.8f;

        [Header("Ataque por Contato")]
        [SerializeField] private float contactDamage = 5f;
        [SerializeField] private float contactCheckDistance = 1.0f;

        [Header("Resistência a Empurrão (Knockback)")]
        [Range(0f, 1f)]
        [SerializeField] private float knockbackResistance = 0f;
        [SerializeField] private float knockbackDecayRate = 8f;

        [Header("Visual Procedural")]
        [SerializeField] private Vector3 tumbleAxis = new Vector3(0.5f, 1f, 0.3f);
        [SerializeField] private float tumbleSpeed = 120f;

        private Transform _playerTransform;
        private PlayerHealth _playerHealth;
        private Vector3 _knockbackVelocity = Vector3.zero;

        public float MoveSpeed => moveSpeed;
        public float ContactDamage => contactDamage;
        public float KnockbackResistance => knockbackResistance;

        public void SetStats(float speed, float damage, float resistance, float height = 0.8f)
        {
            moveSpeed = speed;
            contactDamage = damage;
            knockbackResistance = Mathf.Clamp01(resistance);
            fixedHeight = height;
        }

        public void ApplyKnockback(Vector3 direction, float force)
        {
            if (knockbackResistance >= 1f || force <= 0f) return;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = -transform.forward;
            }
            direction.Normalize();

            float effectiveForce = force * (1f - Mathf.Clamp01(knockbackResistance));
            _knockbackVelocity += direction * effectiveForce;
        }

        private void OnEnable()
        {
            _knockbackVelocity = Vector3.zero;
            FindPlayerReference();
        }

        private void Start()
        {
            FindPlayerReference();
        }

        private void Update()
        {
            if (_playerTransform == null)
            {
                FindPlayerReference();
                if (_playerTransform == null) return;
            }

            // Aplica decaimento do knockback se houver velocidade residual
            if (_knockbackVelocity.sqrMagnitude > 0.01f)
            {
                transform.position += _knockbackVelocity * Time.deltaTime;
                _knockbackVelocity = Vector3.Lerp(_knockbackVelocity, Vector3.zero, knockbackDecayRate * Time.deltaTime);
            }
            else
            {
                _knockbackVelocity = Vector3.zero;
            }

            Vector3 currentPos = transform.position;
            Vector3 targetPos = _playerTransform.position;
            Vector3 direction = targetPos - currentPos;
            direction.y = 0f;

            float sqrDistance = direction.sqrMagnitude;

            // Movimento em direção ao jogador no plano XZ
            if (sqrDistance > 0.01f)
            {
                Vector3 moveDir = direction.normalized;
                transform.position += moveDir * (moveSpeed * Time.deltaTime);
            }

            // Garante que o Y permanece fixo
            transform.position = new Vector3(transform.position.x, fixedHeight, transform.position.z);

            // Rotação procedural contínua para conferir dinamismo à geometria
            transform.Rotate(tumbleAxis * (tumbleSpeed * Time.deltaTime), Space.Self);

            // Verificação de proximidade direta como garantia de acerto mesmo com física dinâmica
            if (sqrDistance <= contactCheckDistance * contactCheckDistance)
            {
                ApplyDamage();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            CheckAndDamage(other);
        }

        private void OnTriggerStay(Collider other)
        {
            CheckAndDamage(other);
        }

        private void CheckAndDamage(Collider other)
        {
            if (other.CompareTag("Player") || other.TryGetComponent<PlayerHealth>(out _) || 
                (other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent<PlayerHealth>(out _)))
            {
                ApplyDamage();
            }
        }

        private void ApplyDamage()
        {
            if (_playerHealth == null)
            {
                FindPlayerReference();
            }

            if (_playerHealth != null)
            {
                _playerHealth.TakeDamage(contactDamage);
            }
        }

        private void FindPlayerReference()
        {
            if (_playerTransform != null && _playerHealth != null) return;

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                var health = Object.FindAnyObjectByType<PlayerHealth>();
                if (health != null) playerObj = health.gameObject;
            }

            if (playerObj != null)
            {
                _playerTransform = playerObj.transform;
                _playerHealth = playerObj.GetComponent<PlayerHealth>();
            }
        }
    }
}

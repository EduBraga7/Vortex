using System.Collections.Generic;
using UnityEngine;
using Vortex.Core;
using Vortex.Enemies;

namespace Vortex.Combat
{
    /// <summary>
    /// Projétil linear de alta velocidade com detecção de colisão contínua,
    /// suporte a perfuração de alvos (Pierce) e retorno para o ObjectPooler.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [Header("Cinemática")]
        [SerializeField] private float speed = 18f;
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private string poolTag = "PulseProjectile";

        private float _damage = 15f;
        private int _pierceCount = 0;
        private float _timer;
        private bool _hasHit;
        private readonly HashSet<EnemyHealth> _damagedEnemies = new HashSet<EnemyHealth>();
        private static readonly Collider[] HitBuffer = new Collider[8];
        private TrailRenderer _trailRenderer;

        public void Initialize(float damageAmount, int pierce = 0, string targetPoolTag = "PulseProjectile")
        {
            _damage = damageAmount;
            _pierceCount = pierce;
            poolTag = targetPoolTag;
            _timer = 0f;
            _hasHit = false;
            _damagedEnemies.Clear();
        }

        private void Awake()
        {
            _trailRenderer = GetComponentInChildren<TrailRenderer>();
        }

        private void OnEnable()
        {
            _timer = 0f;
            _hasHit = false;
            _damagedEnemies.Clear();

            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
            }
        }

        private void Update()
        {
            if (_hasHit) return;

            float travelStep = speed * Time.deltaTime;
            Vector3 startPos = transform.position;

            // Varredura de esfera para evitar penetração/tunneling em altas velocidades
            int hits = Physics.OverlapSphereNonAlloc(startPos, 0.35f, HitBuffer);
            for (int i = 0; i < hits; i++)
            {
                Collider col = HitBuffer[i];
                if (col != null && CheckAndDamage(col))
                {
                    if (_hasHit) return;
                }
            }

            transform.position += transform.forward * travelStep;

            _timer += Time.deltaTime;
            if (_timer >= lifetime)
            {
                ReturnToPool();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasHit) return;
            CheckAndDamage(other);
        }

        private bool CheckAndDamage(Collider other)
        {
            if (_hasHit) return false;

            // Ignora o jogador ou outros projéteis
            if (other.CompareTag("Player") || other.TryGetComponent<Projectile>(out _))
            {
                return false;
            }

            EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>() ?? other.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null && enemyHealth.gameObject.activeInHierarchy)
            {
                // Evita aplicar dano repetido ao mesmo inimigo no caso de perfuração
                if (_damagedEnemies.Contains(enemyHealth))
                {
                    return false;
                }

                _damagedEnemies.Add(enemyHealth);
                enemyHealth.TakeDamage(_damage);

                if (_pierceCount > 0)
                {
                    _pierceCount--;
                    return true;
                }

                _hasHit = true;
                ReturnToPool();
                return true;
            }

            return false;
        }

        private void ReturnToPool()
        {
            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.ReturnToPool(poolTag, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}

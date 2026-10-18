using System.Collections;
using UnityEngine;
using Vortex.Core;
using Vortex.Enemies;

namespace Vortex.Combat
{
    /// <summary>
    /// Pulse Blaster: Arma inicial automática de Vortex.
    /// Varre inimigos em um raio de 12m com OverlapSphereNonAlloc e dispara na direção do mais próximo.
    /// Implementa progressão exata dos 5 níveis definidos no GDD.
    /// </summary>
    public class PulseBlaster : WeaponBase
    {
        [Header("Configuração de Projéteis")]
        [SerializeField] private string projectilePoolTag = "PulseProjectile";
        [SerializeField] private float projectileSpawnHeight = 0.5f;

        [Header("Parâmetros de Rajada e Perfuração")]
        [SerializeField] private int burstCount = 1;
        [SerializeField] private float burstInterval = 0.1f;
        [SerializeField] private int pierceCount = 0;

        private readonly Collider[] _targetBuffer = new Collider[64];

        private void Reset()
        {
            damage = 15f;
            cooldown = 1.0f;
            range = 12f;
            currentLevel = 1;
            maxLevel = 5;
            burstCount = 1;
            pierceCount = 0;
        }

        protected override void Awake()
        {
            base.Awake();
            ApplyLevelStats();
        }

        protected override bool TryAttack()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, range, _targetBuffer);
            if (hitCount == 0) return false;

            Transform closestEnemy = null;
            float closestSqrDistance = float.MaxValue;
            Vector3 myPos = transform.position;

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = _targetBuffer[i];
                if (col == null) continue;

                EnemyHealth enemyHealth = col.GetComponent<EnemyHealth>() ?? col.GetComponentInParent<EnemyHealth>();
                if (enemyHealth != null && enemyHealth.gameObject.activeInHierarchy)
                {
                    Vector3 toEnemy = enemyHealth.transform.position - myPos;
                    toEnemy.y = 0f;
                    float sqrDist = toEnemy.sqrMagnitude;

                    if (sqrDist < closestSqrDistance)
                    {
                        closestSqrDistance = sqrDist;
                        closestEnemy = enemyHealth.transform;
                    }
                }
            }

            if (closestEnemy != null)
            {
                FireAtTarget(closestEnemy.position);
                return true;
            }

            return false;
        }

        private void FireAtTarget(Vector3 targetPosition)
        {
            if (burstCount > 1 && gameObject.activeInHierarchy)
            {
                StartCoroutine(BurstFireRoutine(targetPosition));
            }
            else
            {
                SpawnSingleProjectile(targetPosition);
            }
        }

        private IEnumerator BurstFireRoutine(Vector3 targetPosition)
        {
            for (int i = 0; i < burstCount; i++)
            {
                SpawnSingleProjectile(targetPosition);
                if (i < burstCount - 1)
                {
                    yield return new WaitForSeconds(burstInterval);
                }
            }
        }

        private void SpawnSingleProjectile(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
            {
                direction = transform.forward;
            }
            direction.Normalize();

            Vector3 spawnPosition = transform.position + (Vector3.up * projectileSpawnHeight) + (direction * 0.5f);
            Quaternion spawnRotation = Quaternion.LookRotation(direction, Vector3.up);

            if (ObjectPooler.Instance != null)
            {
                GameObject projObj = ObjectPooler.Instance.SpawnFromPool(projectilePoolTag, spawnPosition, spawnRotation);
                if (projObj != null && projObj.TryGetComponent<Projectile>(out var projectile))
                {
                    projectile.Initialize(damage, pierceCount, projectilePoolTag);
                }
            }
        }

        public override void UpgradeWeapon()
        {
            if (!CanUpgrade) return;

            currentLevel++;
            ApplyLevelStats();
            Debug.Log($"<color=#00FF99>[PulseBlaster]</color> Evoluído para Nível {currentLevel}!");
        }

        private void ApplyLevelStats()
        {
            switch (currentLevel)
            {
                case 1:
                    damage = 15f;
                    cooldown = 1.0f;
                    burstCount = 1;
                    pierceCount = 0;
                    break;
                case 2: // Nv 2: +25% velocidade de disparo (recarga 0.75s)
                    damage = 15f;
                    cooldown = 0.75f;
                    burstCount = 1;
                    pierceCount = 0;
                    break;
                case 3: // Nv 3: +1 projétil adicional em sequência
                    damage = 15f;
                    cooldown = 0.75f;
                    burstCount = 2;
                    pierceCount = 0;
                    break;
                case 4: // Nv 4: Projéteis atravessam +1 inimigo (Pierce 1)
                    damage = 15f;
                    cooldown = 0.75f;
                    burstCount = 2;
                    pierceCount = 1;
                    break;
                case 5: // Nv 5 (Evolução): Cadência dobrada (0.38s) e dano 30
                    damage = 30f;
                    cooldown = 0.38f;
                    burstCount = 2;
                    pierceCount = 1;
                    break;
            }
        }

        public override string GetNextUpgradeDescription()
        {
            return currentLevel switch
            {
                1 => "Nv 2: +25% Cadência de disparo (recarga 0.75s).",
                2 => "Nv 3: Dispara +1 projétil adicional em sequência.",
                3 => "Nv 4: Projéteis perfuram +1 inimigo adicional.",
                4 => "Nv 5 (Evolução): Cadência dobrada (0.38s) e 30 de Dano.",
                _ => "Nível Máximo Alcançado."
            };
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}

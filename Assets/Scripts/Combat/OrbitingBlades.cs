using System.Collections.Generic;
using UnityEngine;
using Vortex.Enemies;

namespace Vortex.Combat
{
    /// <summary>
    /// Orbiting Blades: Arma defensiva orbital de curto alcance.
    /// Mantém lâminas/esferas girando ao redor do jogador em raio fixo de 2.5m.
    /// Começa desativada (currentLevel = 0) até ser selecionada no Level Up.
    /// </summary>
    public class OrbitingBlades : WeaponBase
    {
        [Header("Configuração Orbital")]
        [SerializeField] private float orbitRadius = 2.5f;
        [SerializeField] private float baseRotationSpeed = 180f; // Graus por segundo
        [SerializeField] private float orbitHeight = 0.8f;
        [SerializeField] private float hitCooldownPerEnemy = 0.35f;

        [Header("Configurações das Lâminas")]
        [SerializeField] private int bladeCount = 2;
        [SerializeField] private float bladeScaleMultiplier = 1.0f;
        [SerializeField] private bool hasKnockback = false;
        [SerializeField] private float knockbackForce = 12f;

        private float _currentAngle;
        private readonly List<GameObject> _bladeVisuals = new List<GameObject>();
        private readonly Dictionary<EnemyHealth, float> _enemyHitTimers = new Dictionary<EnemyHealth, float>();
        private readonly Collider[] _hitBuffer = new Collider[32];
        private Material _bladeMaterial;

        private void Reset()
        {
            damage = 10f;
            cooldown = 1.0f;
            range = 2.5f;
            currentLevel = 0;
            maxLevel = 5;
            orbitRadius = 2.5f;
            baseRotationSpeed = 180f;
            bladeCount = 2;
            bladeScaleMultiplier = 1.0f;
            hasKnockback = false;
        }

        protected override void Awake()
        {
            base.Awake();
            currentLevel = 0; // Começa desativada no início da partida
            InitializeMaterial();
            ApplyLevelStats();
        }

        private void InitializeMaterial()
        {
            if (_bladeMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") 
                             ?? Shader.Find("Standard") 
                             ?? Shader.Find("Unlit/Color");
                _bladeMaterial = new Material(shader) { name = "M_OrbitBlade_Cyan" };
                Color neonCyan = new Color(0f, 0.95f, 1f);
                if (_bladeMaterial.HasProperty("_BaseColor")) _bladeMaterial.SetColor("_BaseColor", neonCyan);
                if (_bladeMaterial.HasProperty("_Color")) _bladeMaterial.SetColor("_Color", neonCyan);
                if (_bladeMaterial.HasProperty("_EmissionColor"))
                {
                    _bladeMaterial.EnableKeyword("_EMISSION");
                    _bladeMaterial.SetColor("_EmissionColor", neonCyan * 2.0f);
                }
            }
        }

        protected override void Update()
        {
            if (currentLevel <= 0)
            {
                SetBladesActive(false);
                return;
            }

            SetBladesActive(true);
            UpdateOrbitalPositions();
            CheckBladeCollisions();
        }

        protected override bool TryAttack()
        {
            // O ataque das lâminas é contínuo por colisão orbital em Update()
            return true;
        }

        private void UpdateOrbitalPositions()
        {
            _currentAngle += baseRotationSpeed * Time.deltaTime;
            if (_currentAngle >= 360f) _currentAngle -= 360f;

            Vector3 center = transform.position;
            center.y = orbitHeight;

            EnsureBladeVisuals();

            float angleStep = 360f / bladeCount;
            for (int i = 0; i < bladeCount; i++)
            {
                float angleDeg = _currentAngle + (i * angleStep);
                float angleRad = angleDeg * Mathf.Deg2Rad;

                Vector3 localOffset = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad)) * orbitRadius;
                Vector3 bladePos = center + localOffset;

                GameObject blade = _bladeVisuals[i];
                blade.transform.position = bladePos;
                blade.transform.rotation = Quaternion.Euler(0f, -angleDeg, 45f);

                float size = 0.45f * bladeScaleMultiplier;
                blade.transform.localScale = new Vector3(size, size, size);
            }
        }

        private void CheckBladeCollisions()
        {
            float hitRadius = 0.5f * bladeScaleMultiplier;
            float currentTime = Time.time;

            for (int i = 0; i < bladeCount; i++)
            {
                Vector3 bladePos = _bladeVisuals[i].transform.position;
                int count = Physics.OverlapSphereNonAlloc(bladePos, hitRadius, _hitBuffer);

                for (int c = 0; c < count; c++)
                {
                    Collider col = _hitBuffer[c];
                    if (col == null) continue;

                    EnemyHealth enemyHealth = col.GetComponent<EnemyHealth>() ?? col.GetComponentInParent<EnemyHealth>();
                    if (enemyHealth != null && enemyHealth.gameObject.activeInHierarchy)
                    {
                        if (_enemyHitTimers.TryGetValue(enemyHealth, out float nextHitTime))
                        {
                            if (currentTime < nextHitTime) continue;
                        }

                        _enemyHitTimers[enemyHealth] = currentTime + hitCooldownPerEnemy;
                        enemyHealth.TakeDamage(damage);

                        if (hasKnockback)
                        {
                            EnemyAI ai = enemyHealth.GetComponent<EnemyAI>() ?? enemyHealth.GetComponentInParent<EnemyAI>();
                            if (ai != null)
                            {
                                Vector3 knockDir = (enemyHealth.transform.position - transform.position).normalized;
                                ai.ApplyKnockback(knockDir, knockbackForce);
                            }
                        }
                    }
                }
            }
        }

        private void EnsureBladeVisuals()
        {
            while (_bladeVisuals.Count < bladeCount)
            {
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                blade.name = $"OrbitBlade_{_bladeVisuals.Count + 1}";
                blade.transform.SetParent(transform, true);

                if (blade.TryGetComponent<Collider>(out var col))
                {
                    col.enabled = false; // Usamos OverlapSphere manual para máxima performance e controle
                }

                if (blade.TryGetComponent<Renderer>(out var rend) && _bladeMaterial != null)
                {
                    rend.sharedMaterial = _bladeMaterial;
                }

                _bladeVisuals.Add(blade);
            }

            for (int i = 0; i < _bladeVisuals.Count; i++)
            {
                _bladeVisuals[i].SetActive(i < bladeCount);
            }
        }

        private void SetBladesActive(bool active)
        {
            for (int i = 0; i < _bladeVisuals.Count; i++)
            {
                if (_bladeVisuals[i] != null)
                {
                    _bladeVisuals[i].SetActive(active && i < bladeCount);
                }
            }
        }

        public override void UpgradeWeapon()
        {
            if (!CanUpgrade) return;

            currentLevel++;
            ApplyLevelStats();
            Debug.Log($"<color=#00FFFF>[OrbitingBlades]</color> Evoluído para Nível {currentLevel}!");
        }

        private void ApplyLevelStats()
        {
            switch (currentLevel)
            {
                case 1:
                    bladeCount = 2;
                    damage = 10f;
                    baseRotationSpeed = 180f;
                    bladeScaleMultiplier = 1.0f;
                    hasKnockback = false;
                    break;
                case 2: // Nv 2: +30% velocidade de rotação
                    bladeCount = 2;
                    damage = 10f;
                    baseRotationSpeed = 234f;
                    bladeScaleMultiplier = 1.0f;
                    hasKnockback = false;
                    break;
                case 3: // Nv 3: +1 lâmina orbital (total 3)
                    bladeCount = 3;
                    damage = 10f;
                    baseRotationSpeed = 234f;
                    bladeScaleMultiplier = 1.0f;
                    hasKnockback = false;
                    break;
                case 4: // Nv 4: +25% tamanho
                    bladeCount = 3;
                    damage = 10f;
                    baseRotationSpeed = 234f;
                    bladeScaleMultiplier = 1.25f;
                    hasKnockback = false;
                    break;
                case 5: // Nv 5 (Evolução): 4 lâminas, rotação hiper-rápida e Knockback
                    bladeCount = 4;
                    damage = 15f;
                    baseRotationSpeed = 420f;
                    bladeScaleMultiplier = 1.35f;
                    hasKnockback = true;
                    knockbackForce = 14f;
                    break;
            }

            if (currentLevel > 0)
            {
                EnsureBladeVisuals();
            }
        }

        public override string GetNextUpgradeDescription()
        {
            return currentLevel switch
            {
                0 => "Nv 1: 2 Lâminas orbitais (raio 2.5m) que causam 10 de dano por contato.",
                1 => "Nv 2: +30% velocidade de rotação orbital.",
                2 => "Nv 3: +1 lâmina orbital adicional (total 3).",
                3 => "Nv 4: Aumenta o tamanho das lâminas em 25%.",
                4 => "Nv 5 (Evolução): 4 lâminas, rotação hiper-rápida e repulsão (Knockback).",
                _ => "Nível Máximo Alcançado."
            };
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _bladeVisuals.Count; i++)
            {
                if (_bladeVisuals[i] != null)
                {
                    Destroy(_bladeVisuals[i]);
                }
            }
            _bladeVisuals.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, orbitRadius);
        }
    }
}

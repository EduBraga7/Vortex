using System.Collections;
using UnityEngine;
using Vortex.CameraSystem;
using Vortex.Core;
using Vortex.Enemies;

namespace Vortex.Combat
{
    /// <summary>
    /// Vortex Shockwave: Arma de controle de grupo e repulsão em área circular.
    /// Emite pulsos a partir do jogador usando Physics.OverlapSphereNonAlloc e efeito visual de anel expansivo.
    /// Começa desativada (currentLevel = 0) até ser selecionada no Level Up.
    /// </summary>
    public class VortexShockwave : WeaponBase
    {
        [Header("Efeito Visual do Pulso")]
        [SerializeField] private Color shockwaveColor = new Color(0.85f, 0.2f, 1f, 0.8f); // Magenta elétrico
        [SerializeField] private float expandDuration = 0.28f;

        [Header("Atributos de Knockback")]
        [SerializeField] private float knockbackForce = 10f;
        [SerializeField] private bool isElectrified = false;
        [SerializeField] private float electrifiedDuration = 2.0f;
        [SerializeField] private float electrifiedTickDamage = 6f;

        private readonly Collider[] _hitBuffer = new Collider[64];
        private GameObject _ringVisualObj;
        private LineRenderer _ringLineRenderer;
        private Material _ringMaterial;

        private void Reset()
        {
            damage = 20f;
            cooldown = 3.5f;
            range = 4.0f;
            currentLevel = 0;
            maxLevel = 5;
            knockbackForce = 10f;
            isElectrified = false;
        }

        protected override void Awake()
        {
            base.Awake();
            currentLevel = 0; // Começa desativada no início da partida
            InitializeRingVisual();
            ApplyLevelStats();
        }

        private void InitializeRingVisual()
        {
            if (_ringVisualObj == null)
            {
                _ringVisualObj = new GameObject("ShockwaveRingVisual");
                _ringVisualObj.transform.SetParent(transform, false);
                _ringVisualObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);

                _ringLineRenderer = _ringVisualObj.AddComponent<LineRenderer>();
                _ringLineRenderer.useWorldSpace = false;
                _ringLineRenderer.loop = true;
                _ringLineRenderer.positionCount = 36;
                _ringLineRenderer.startWidth = 0.25f;
                _ringLineRenderer.endWidth = 0.25f;

                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") 
                             ?? Shader.Find("Sprites/Default") 
                             ?? Shader.Find("Standard");
                _ringMaterial = new Material(shader) { name = "M_ShockwaveRing" };
                if (_ringMaterial.HasProperty("_BaseColor")) _ringMaterial.SetColor("_BaseColor", shockwaveColor);
                if (_ringMaterial.HasProperty("_Color")) _ringMaterial.SetColor("_Color", shockwaveColor);

                _ringLineRenderer.sharedMaterial = _ringMaterial;
                _ringVisualObj.SetActive(false);
            }
        }

        protected override bool TryAttack()
        {
            if (currentLevel <= 0) return false;

            if (currentLevel >= 5)
            {
                StartCoroutine(DoublePulseRoutine());
            }
            else
            {
                TriggerPulse(range, damage, knockbackForce);
                StartCoroutine(ExpandRingVisual(range, expandDuration, shockwaveColor));
            }

            return true;
        }

        private IEnumerator DoublePulseRoutine()
        {
            // Primeiro pulso
            TriggerPulse(range, damage, knockbackForce);
            StartCoroutine(ExpandRingVisual(range, expandDuration, shockwaveColor));

            yield return new WaitForSeconds(0.25f);

            // Segundo pulso (Nv 5)
            TriggerPulse(range, damage, knockbackForce);
            StartCoroutine(ExpandRingVisual(range, expandDuration, new Color(1f, 0.4f, 0.1f, 0.9f))); // Laranja elétrico

            // Eletrifica a área por 2 segundos
            if (isElectrified)
            {
                StartCoroutine(ElectrifiedFieldRoutine(transform.position, range, electrifiedDuration));
            }
        }

        private void TriggerPulse(float pulseRadius, float pulseDamage, float force)
        {
            // Shake leve de câmera e som de pulso sci-fi expansivo
            TopDownCameraFollow.Instance?.TriggerShake(0.18f, 0.25f);
            SynthAudioManager.Instance?.PlayShockwaveSFX();

            Vector3 center = transform.position;
            int count = Physics.OverlapSphereNonAlloc(center, pulseRadius, _hitBuffer);

            for (int i = 0; i < count; i++)
            {
                Collider col = _hitBuffer[i];
                if (col == null) continue;

                EnemyHealth health = col.GetComponent<EnemyHealth>() ?? col.GetComponentInParent<EnemyHealth>();
                if (health != null && health.gameObject.activeInHierarchy)
                {
                    health.TakeDamage(pulseDamage);

                    EnemyAI ai = health.GetComponent<EnemyAI>() ?? health.GetComponentInParent<EnemyAI>();
                    if (ai != null)
                    {
                        Vector3 dir = health.transform.position - center;
                        dir.y = 0f;
                        if (dir.sqrMagnitude < 0.001f) dir = -health.transform.forward;
                        ai.ApplyKnockback(dir.normalized, force);
                    }
                }
            }
        }

        private IEnumerator ElectrifiedFieldRoutine(Vector3 fixedCenter, float pulseRadius, float duration)
        {
            float elapsed = 0f;
            float tickInterval = 0.4f;

            while (elapsed < duration)
            {
                yield return new WaitForSeconds(tickInterval);
                elapsed += tickInterval;

                int count = Physics.OverlapSphereNonAlloc(fixedCenter, pulseRadius, _hitBuffer);
                for (int i = 0; i < count; i++)
                {
                    Collider col = _hitBuffer[i];
                    if (col == null) continue;

                    EnemyHealth health = col.GetComponent<EnemyHealth>() ?? col.GetComponentInParent<EnemyHealth>();
                    if (health != null && health.gameObject.activeInHierarchy)
                    {
                        health.TakeDamage(electrifiedTickDamage);
                    }
                }
            }
        }

        private IEnumerator ExpandRingVisual(float targetRadius, float duration, Color ringCol)
        {
            if (_ringVisualObj == null || _ringLineRenderer == null) yield break;

            _ringVisualObj.SetActive(true);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float currentRad = Mathf.Lerp(0.5f, targetRadius, t);

                UpdateCircleVertices(currentRad);

                Color col = ringCol;
                col.a = Mathf.Lerp(ringCol.a, 0f, t);
                _ringLineRenderer.startColor = col;
                _ringLineRenderer.endColor = col;

                yield return null;
            }

            _ringVisualObj.SetActive(false);
        }

        private void UpdateCircleVertices(float radius)
        {
            int segments = 36;
            float step = 360f / segments;
            for (int i = 0; i < segments; i++)
            {
                float rad = i * step * Mathf.Deg2Rad;
                Vector3 pt = new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
                _ringLineRenderer.SetPosition(i, pt);
            }
        }

        public override void UpgradeWeapon()
        {
            if (!CanUpgrade) return;

            currentLevel++;
            ApplyLevelStats();
            Debug.Log($"<color=#FF00FF>[VortexShockwave]</color> Evoluído para Nível {currentLevel}!");
        }

        private void ApplyLevelStats()
        {
            switch (currentLevel)
            {
                case 1:
                    range = 4.0f;
                    damage = 20f;
                    cooldown = 3.5f;
                    knockbackForce = 10f;
                    isElectrified = false;
                    break;
                case 2: // Nv 2: Raio 5.5m
                    range = 5.5f;
                    damage = 20f;
                    cooldown = 3.5f;
                    knockbackForce = 10f;
                    isElectrified = false;
                    break;
                case 3: // Nv 3: Recarga reduzida para 2.8s
                    range = 5.5f;
                    damage = 20f;
                    cooldown = 2.8f;
                    knockbackForce = 10f;
                    isElectrified = false;
                    break;
                case 4: // Nv 4: +50% Knockback (força 15)
                    range = 5.5f;
                    damage = 20f;
                    cooldown = 2.8f;
                    knockbackForce = 15f;
                    isElectrified = false;
                    break;
                case 5: // Nv 5 (Evolução): Pulso duplo que eletrifica a área por 2s
                    range = 6.0f;
                    damage = 25f;
                    cooldown = 2.5f;
                    knockbackForce = 18f;
                    isElectrified = true;
                    electrifiedDuration = 2.0f;
                    electrifiedTickDamage = 8f;
                    break;
            }
        }

        public override string GetNextUpgradeDescription()
        {
            return currentLevel switch
            {
                0 => "Nv 1: Pulso circular em área (Raio 4m, 20 dano, 3.5s recarga) com Knockback.",
                1 => "Nv 2: Raio de impacto expandido para 5.5m.",
                2 => "Nv 3: Tempo de recarga reduzido para 2.8s.",
                3 => "Nv 4: +50% de força de repulsão (Knockback).",
                4 => "Nv 5 (Evolução): Pulso duplo que eletrifica a área por 2 segundos.",
                _ => "Nível Máximo Alcançado."
            };
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}

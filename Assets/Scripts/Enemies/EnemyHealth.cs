using System.Collections;
using UnityEngine;
using Vortex.Combat;
using Vortex.Core;
using Vortex.CameraSystem;

namespace Vortex.Enemies
{
    /// <summary>
    /// Controla a vida do inimigo, feedback visual procedural de dano (squash & stretch, hit flash)
    /// e reciclagem de volta para o ObjectPooler ao morrer com spawn de partículas DeathVFX.
    /// </summary>
    public class EnemyHealth : MonoBehaviour
    {
        [Header("Configurações de Vida")]
        [SerializeField] private float maxHP = 15f;
        [SerializeField] private string poolTag = "Swarmer";

        [Header("Feedback Visual")]
        [SerializeField] private float hitRecoverySpeed = 16f;
        [SerializeField] private Color deathColor = new Color(1.0f, 0.4f, 0.1f);

        private float _currentHP;
        private Vector3 _baseScale;
        private bool _isDying;

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private Coroutine _hitFlashCoroutine;
        private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");

        public float CurrentHP => _currentHP;
        public float MaxHP => maxHP;
        public string PoolTag => poolTag;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _renderer = GetComponentInChildren<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            _currentHP = maxHP;
            _isDying = false;

            if (_baseScale != Vector3.zero)
            {
                transform.localScale = _baseScale;
            }

            if (_renderer != null && _propBlock != null)
            {
                _propBlock.Clear();
                _renderer.SetPropertyBlock(_propBlock);
            }
        }

        private void Update()
        {
            // Recuperação suave do efeito de squash & stretch ao receber dano
            if (transform.localScale != _baseScale)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, hitRecoverySpeed * Time.deltaTime);
            }
        }

        public void TakeDamage(float amount)
        {
            if (_isDying) return;

            _currentHP -= amount;

            // Efeito procedural de squash & stretch: expande no plano XZ e achata em Y
            transform.localScale = new Vector3(_baseScale.x * 1.35f, _baseScale.y * 0.65f, _baseScale.z * 1.35f);

            // Audio SFX de impacto
            SynthAudioManager.Instance?.PlayHitSFX();

            // Popup de dano flutuante no espaço 3D
            DamagePopupManager.Instance?.ShowDamage(transform.position, amount);

            // Hit Flash em branco brilhante via MaterialPropertyBlock por 0.08s
            if (_renderer != null && gameObject.activeInHierarchy)
            {
                if (_hitFlashCoroutine != null) StopCoroutine(_hitFlashCoroutine);
                _hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
            }

            if (_currentHP <= 0f)
            {
                Die();
            }
        }

        private IEnumerator HitFlashRoutine()
        {
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(EmissionColorProp, Color.white * 3.5f);
            _renderer.SetPropertyBlock(_propBlock);

            yield return new WaitForSeconds(0.08f);

            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.Clear();
            _renderer.SetPropertyBlock(_propBlock);
            _hitFlashCoroutine = null;
        }

        private void Die()
        {
            _isDying = true;

            if (_hitFlashCoroutine != null)
            {
                StopCoroutine(_hitFlashCoroutine);
                _hitFlashCoroutine = null;
                if (_renderer != null && _propBlock != null)
                {
                    _propBlock.Clear();
                    _renderer.SetPropertyBlock(_propBlock);
                }
            }

            // Notifica o sistema que um inimigo foi eliminado (contador de kills)
            EventBus.RaiseEnemyKilled();

            // Micro screen shake responsivo de acordo com o porte do inimigo abatido
            if (poolTag == "CoreBoss")
            {
                TopDownCameraFollow.Instance?.TriggerShake(0.35f, 0.55f);
            }
            else if (poolTag == "Bruiser")
            {
                TopDownCameraFollow.Instance?.TriggerShake(0.12f, 0.20f);
            }
            else
            {
                TopDownCameraFollow.Instance?.TriggerShake(0.05f, 0.08f);
            }

            // Spawna partículas de morte e Cristal de Energia (XP)
            if (ObjectPooler.Instance != null)
            {
                GameObject vfx = ObjectPooler.Instance.SpawnFromPool("DeathVFX", transform.position, Quaternion.identity);
                if (vfx != null && vfx.TryGetComponent<DeathParticleEffect>(out var deathEffect))
                {
                    deathEffect.Play(GetDeathColor());
                }

                ObjectPooler.Instance.SpawnFromPool("XPGem", transform.position, Quaternion.identity);
                ObjectPooler.Instance.ReturnToPool(poolTag, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public Color GetDeathColor()
        {
            if (poolTag == "Swarmer") return new Color(1.0f, 0.45f, 0.05f) * 2.0f;
            if (poolTag == "Bruiser") return new Color(0.9f, 0.1f, 0.85f) * 2.0f;
            if (poolTag == "CoreBoss") return new Color(1.0f, 0.1f, 0.2f) * 2.5f;
            return deathColor;
        }

        public void SetBaseScale(Vector3 scale)
        {
            _baseScale = scale;
            transform.localScale = scale;
        }

        public void Configure(float hp, string tag)
        {
            maxHP = hp;
            _currentHP = hp;
            poolTag = tag;
        }
    }
}

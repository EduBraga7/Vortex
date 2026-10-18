using UnityEngine;
using Vortex.Core;

namespace Vortex.Enemies
{
    /// <summary>
    /// Controla a vida do inimigo, feedback visual procedural de dano (squash & stretch)
    /// e reciclagem de volta para o ObjectPooler ao morrer.
    /// </summary>
    public class EnemyHealth : MonoBehaviour
    {
        [Header("Configurações de Vida")]
        [SerializeField] private float maxHP = 15f;
        [SerializeField] private string poolTag = "Swarmer";

        [Header("Feedback Visual")]
        [SerializeField] private float hitRecoverySpeed = 16f;

        private float _currentHP;
        private Vector3 _baseScale;
        private bool _isDying;

        public float CurrentHP => _currentHP;
        public float MaxHP => maxHP;
        public string PoolTag => poolTag;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            _currentHP = maxHP;
            _isDying = false;

            if (_baseScale != Vector3.zero)
            {
                transform.localScale = _baseScale;
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

            if (_currentHP <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            _isDying = true;

            // Notifica o sistema que um inimigo foi eliminado (contador de kills)
            EventBus.RaiseEnemyKilled();

            // Spawna Cristal de Energia (XP) na posição do inimigo
            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.SpawnFromPool("XPGem", transform.position, Quaternion.identity);
                ObjectPooler.Instance.ReturnToPool(poolTag, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
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

using System;
using UnityEngine;
using Vortex.Core;

namespace Vortex.Player
{
    /// <summary>
    /// Gerencia os pontos de vida do jogador, invulnerabilidade temporária (I-Frames) e evento de derrota.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Configurações de Vida")]
        [SerializeField] private float maxHP = 100f;
        [SerializeField] private float invulnerabilityDuration = 0.5f;

        public event Action<float, float> OnHealthChanged;
        public event Action OnPlayerDeath;

        private float _currentHP;
        private float _lastDamageTime = -999f;
        private bool _isDead;

        public float CurrentHP => _currentHP;
        public float MaxHP => maxHP;
        public bool IsDead => _isDead;

        private void Awake()
        {
            _currentHP = maxHP;
            _isDead = false;
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(_currentHP, maxHP);
            EventBus.RaisePlayerHealthChanged(_currentHP, maxHP);
        }

        /// <summary>
        /// Aplica dano respeitando o período de invulnerabilidade (I-Frames).
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (_isDead) return;

            if (Time.time < _lastDamageTime + invulnerabilityDuration)
            {
                return;
            }

            _lastDamageTime = Time.time;
            _currentHP = Mathf.Max(0f, _currentHP - amount);

            OnHealthChanged?.Invoke(_currentHP, maxHP);
            EventBus.RaisePlayerHealthChanged(_currentHP, maxHP);

            if (_currentHP <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// Restaura vida do jogador até o limite máximo.
        /// </summary>
        public void Heal(float amount)
        {
            if (_isDead) return;

            _currentHP = Mathf.Min(maxHP, _currentHP + amount);
            OnHealthChanged?.Invoke(_currentHP, maxHP);
            EventBus.RaisePlayerHealthChanged(_currentHP, maxHP);
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            Debug.Log("<color=#FF3366><b>[Game Over]</b></color> A integridade do núcleo do jogador chegou a zero.");
            OnPlayerDeath?.Invoke();
            EventBus.RaisePlayerDeath();

            Time.timeScale = 0f;
        }

        public void ResetHealth()
        {
            _isDead = false;
            _currentHP = maxHP;
            _lastDamageTime = -999f;
            OnHealthChanged?.Invoke(_currentHP, maxHP);
            EventBus.RaisePlayerHealthChanged(_currentHP, maxHP);
        }
    }
}

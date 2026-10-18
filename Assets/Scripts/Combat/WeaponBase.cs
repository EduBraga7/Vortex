using UnityEngine;
using Vortex.Player;

namespace Vortex.Combat
{
    /// <summary>
    /// Classe base abstrata para o arsenal de Vortex.
    /// Define atributos de Nível 1 ao 5, suporte a Upgrades e ciclo de disparo automático com Cooldown Reduction.
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        [Header("Atributos da Arma")]
        [SerializeField] protected float damage = 15f;
        [SerializeField] protected float cooldown = 1.0f;
        [SerializeField] protected float range = 12f;

        [Header("Nível e Progressão")]
        public int currentLevel = 1;
        public int maxLevel = 5;

        protected float nextFireTime;
        protected PlayerStats playerStats;

        public float Damage => damage;
        public float Cooldown => cooldown;
        public float Range => range;
        public virtual bool CanUpgrade => currentLevel < maxLevel;

        protected virtual void Awake()
        {
            playerStats = GetComponentInParent<PlayerStats>();
        }

        protected virtual void Update()
        {
            if (currentLevel <= 0) return;

            if (playerStats == null)
            {
                playerStats = GetComponentInParent<PlayerStats>();
            }

            if (Time.time >= nextFireTime)
            {
                if (TryAttack())
                {
                    float cdr = playerStats != null ? playerStats.CooldownReduction : 0f;
                    float effectiveCooldown = Mathf.Max(0.1f, cooldown * (1f - cdr));
                    nextFireTime = Time.time + effectiveCooldown;
                }
            }
        }

        /// <summary>
        /// Executa a rotina de ataque da arma. Retorna true se um ataque foi realizado com sucesso.
        /// </summary>
        protected abstract bool TryAttack();

        /// <summary>
        /// Aplica a melhoria para o próximo nível da arma.
        /// </summary>
        public abstract void UpgradeWeapon();

        /// <summary>
        /// Retorna a descrição do próximo nível para ser exibida nos cartões de Level Up.
        /// </summary>
        public abstract string GetNextUpgradeDescription();
    }
}

using UnityEngine;
using Vortex.Combat;
using Vortex.Core;

namespace Vortex.Player
{
    /// <summary>
    /// Gerencia atributos base do jogador (Move Speed, Pickup Radius, Cooldown Reduction),
    /// atração magnética de Cristais de XP e progressão de Níveis com escalonamento de +40%.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerStats : MonoBehaviour
    {
        [Header("Atributos Base (GDD)")]
        [SerializeField] private float moveSpeed = 6.0f;
        [SerializeField] private float pickupRadius = 3.0f;
        [Range(0f, 0.8f)]
        [SerializeField] private float cooldownReduction = 0.0f;

        [Header("Progressão de XP")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private float currentXP = 0f;
        [SerializeField] private float requiredXP = 25f;

        private PlayerMovement _playerMovement;
        private readonly Collider[] _gemBuffer = new Collider[32];

        public float MoveSpeed => moveSpeed;
        public float PickupRadius => pickupRadius;
        public float CooldownReduction => cooldownReduction;
        public int CurrentLevel => currentLevel;
        public float CurrentXP => currentXP;
        public float RequiredXP => requiredXP;

        private void Awake()
        {
            _playerMovement = GetComponent<PlayerMovement>();
            if (_playerMovement != null)
            {
                _playerMovement.SetMoveSpeed(moveSpeed);
            }
        }

        private void Start()
        {
            // Emite estado inicial de XP para atualizar HUD imediatamente
            EventBus.RaiseXPChanged(currentXP, requiredXP, currentLevel);
        }

        private void Update()
        {
            ScanNearbyXPGems();
        }

        /// <summary>
        /// Detecta Cristais de Energia no raio magnético (pickupRadius) e ativa sua atração em direção ao Player.
        /// </summary>
        private void ScanNearbyXPGems()
        {
            int hits = Physics.OverlapSphereNonAlloc(transform.position, pickupRadius, _gemBuffer);
            for (int i = 0; i < hits; i++)
            {
                Collider col = _gemBuffer[i];
                if (col == null) continue;

                if (col.TryGetComponent<XPGem>(out var gem) || (col.attachedRigidbody != null && col.attachedRigidbody.TryGetComponent<XPGem>(out gem)))
                {
                    gem.AttractTo(transform);
                }
            }
        }

        /// <summary>
        /// Adiciona pontos de experiência ao jogador, checando avanço de nível (+40% requiredXP por nível).
        /// </summary>
        public void AddXP(float amount)
        {
            currentXP += amount;

            while (currentXP >= requiredXP)
            {
                currentXP -= requiredXP;
                currentLevel++;
                requiredXP = Mathf.Round(requiredXP * 1.4f);

                EventBus.RaisePlayerLevelUp(currentLevel);
            }

            EventBus.RaiseXPChanged(currentXP, requiredXP, currentLevel);
        }

        // ==================== MÉTODOS DE UPGRADE (CARTAS) ====================

        public void UpgradeMoveSpeed(float bonusPercent)
        {
            moveSpeed *= (1f + bonusPercent);
            if (_playerMovement != null)
            {
                _playerMovement.SetMoveSpeed(moveSpeed);
            }
            Debug.Log($"<color=#00FF99>[PlayerStats]</color> Move Speed aprimorado para: {moveSpeed:F2} m/s (+{bonusPercent * 100}%)");
        }

        public void UpgradePickupRadius(float bonusPercent)
        {
            pickupRadius *= (1f + bonusPercent);
            Debug.Log($"<color=#00FF99>[PlayerStats]</color> Pickup Radius aprimorado para: {pickupRadius:F2} m (+{bonusPercent * 100}%)");
        }

        public void UpgradeCooldownReduction(float bonusPercent)
        {
            cooldownReduction = Mathf.Clamp(cooldownReduction + bonusPercent, 0f, 0.75f);
            Debug.Log($"<color=#00FF99>[PlayerStats]</color> Cooldown Reduction aprimorado para: {cooldownReduction * 100:F1}%");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}

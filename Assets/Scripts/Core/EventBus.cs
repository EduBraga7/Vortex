using System;

namespace Vortex.Core
{
    /// <summary>
    /// Barramento de eventos global e desacoplado para comunicação entre Gameplay, Jogador e UI.
    /// Elimina dependências diretas e mantém o desacoplamento de acordo com o GDD.
    /// </summary>
    public static class EventBus
    {
        public static event Action<float, float, int> OnXPChanged;
        public static event Action<int> OnPlayerLevelUp;
        public static event Action OnEnemyKilled;
        public static event Action<float, float> OnPlayerHealthChanged;
        public static event Action OnPlayerDeath;
        public static event Action<bool, float, int> OnGameEnded; // isVictory, survivalTime, totalKills

        public static void RaiseXPChanged(float currentXP, float requiredXP, int currentLevel)
        {
            OnXPChanged?.Invoke(currentXP, requiredXP, currentLevel);
        }

        public static void RaisePlayerLevelUp(int newLevel)
        {
            OnPlayerLevelUp?.Invoke(newLevel);
        }

        public static void RaiseEnemyKilled()
        {
            OnEnemyKilled?.Invoke();
        }

        public static void RaisePlayerHealthChanged(float currentHP, float maxHP)
        {
            OnPlayerHealthChanged?.Invoke(currentHP, maxHP);
        }

        public static void RaisePlayerDeath()
        {
            OnPlayerDeath?.Invoke();
        }

        public static void RaiseGameEnded(bool isVictory, float survivalTime, int totalKills)
        {
            OnGameEnded?.Invoke(isVictory, survivalTime, totalKills);
        }

        public static void ClearAllSubscribers()
        {
            OnXPChanged = null;
            OnPlayerLevelUp = null;
            OnEnemyKilled = null;
            OnPlayerHealthChanged = null;
            OnPlayerDeath = null;
            OnGameEnded = null;
        }
    }
}

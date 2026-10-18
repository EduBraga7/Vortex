using UnityEngine;
using UnityEngine.UI;
using Vortex.Core;

namespace Vortex.UI
{
    /// <summary>
    /// Controla a exibição em tempo real da HUD de gameplay (XP, Nível, Cronômetro, Kills e Vida).
    /// Totalmente desacoplado através do EventBus.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Elementos de XP e Nível")]
        [SerializeField] private Image xpBarFill;
        [SerializeField] private Text levelText;

        [Header("Elementos de Partida")]
        [SerializeField] private Text timerText;
        [SerializeField] private Text killsText;

        [Header("Vida do Jogador")]
        [SerializeField] private Image healthBarFill;

        private float _matchTimer;
        private int _killCount;

        private void OnEnable()
        {
            EventBus.OnXPChanged += HandleXPChanged;
            EventBus.OnPlayerLevelUp += HandleLevelUp;
            EventBus.OnEnemyKilled += HandleEnemyKilled;
            EventBus.OnPlayerHealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            EventBus.OnXPChanged -= HandleXPChanged;
            EventBus.OnPlayerLevelUp -= HandleLevelUp;
            EventBus.OnEnemyKilled -= HandleEnemyKilled;
            EventBus.OnPlayerHealthChanged -= HandleHealthChanged;
        }

        private void Start()
        {
            _matchTimer = 0f;
            _killCount = 0;
            UpdateKillsDisplay();
            UpdateTimerDisplay();
        }

        private void Update()
        {
            if (Time.timeScale > 0f)
            {
                _matchTimer += Time.deltaTime;
                UpdateTimerDisplay();
            }
        }

        private void HandleXPChanged(float currentXP, float requiredXP, int currentLevel)
        {
            if (xpBarFill != null)
            {
                xpBarFill.fillAmount = requiredXP > 0 ? Mathf.Clamp01(currentXP / requiredXP) : 0f;
            }

            if (levelText != null)
            {
                levelText.text = $"LV. {currentLevel}";
            }
        }

        private void HandleLevelUp(int newLevel)
        {
            if (levelText != null)
            {
                levelText.text = $"LV. {newLevel}";
            }
        }

        private void HandleEnemyKilled()
        {
            _killCount++;
            UpdateKillsDisplay();
        }

        private void HandleHealthChanged(float currentHP, float maxHP)
        {
            if (healthBarFill != null)
            {
                healthBarFill.fillAmount = maxHP > 0 ? Mathf.Clamp01(currentHP / maxHP) : 0f;
            }
        }

        private void UpdateTimerDisplay()
        {
            if (timerText == null) return;

            int minutes = Mathf.FloorToInt(_matchTimer / 60f);
            int seconds = Mathf.FloorToInt(_matchTimer % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        private void UpdateKillsDisplay()
        {
            if (killsText != null)
            {
                killsText.text = $"KILLS: {_killCount}";
            }
        }

        public void SetupReferences(Image xpFill, Text lvlTxt, Text tmrTxt, Text kllTxt, Image hpFill = null)
        {
            xpBarFill = xpFill;
            levelText = lvlTxt;
            timerText = tmrTxt;
            killsText = kllTxt;
            healthBarFill = hpFill;
        }
    }
}

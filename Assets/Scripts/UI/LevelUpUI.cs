using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Combat;
using Vortex.Core;
using Vortex.Player;

namespace Vortex.UI
{
    /// <summary>
    /// Gerencia a janela de Level Up, congelamento do jogo (Time.timeScale = 0),
    /// exibição de 3 cartas aleatórias de upgrade e aplicação das melhorias selecionadas.
    /// </summary>
    public class LevelUpUI : MonoBehaviour
    {
        [System.Serializable]
        public class UpgradeCardUI
        {
            public GameObject cardRoot;
            public Text titleText;
            public Text descriptionText;
            public Button selectButton;
        }

        private class UpgradeOption
        {
            public string Title;
            public string Description;
            public Action ApplyAction;
        }

        [Header("Painel Principal")]
        [SerializeField] private GameObject levelUpPanel;

        [Header("Cartões de Upgrade")]
        [SerializeField] private UpgradeCardUI[] upgradeCards = new UpgradeCardUI[3];

        private PlayerStats _playerStats;
        private PulseBlaster _pulseBlaster;
        private OrbitingBlades _orbitingBlades;
        private VortexShockwave _vortexShockwave;

        private void OnEnable()
        {
            EventBus.OnPlayerLevelUp += ShowLevelUpSelection;
        }

        private void OnDisable()
        {
            EventBus.OnPlayerLevelUp -= ShowLevelUpSelection;
        }

        private void Awake()
        {
            LocatePlayerReferences();
            if (levelUpPanel != null)
            {
                levelUpPanel.SetActive(false);
            }
        }

        private void LocatePlayerReferences()
        {
            if (_playerStats != null && _pulseBlaster != null && _orbitingBlades != null && _vortexShockwave != null) return;

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                if (_playerStats == null) _playerStats = player.GetComponent<PlayerStats>();
                if (_pulseBlaster == null) _pulseBlaster = player.GetComponent<PulseBlaster>();
                if (_orbitingBlades == null) _orbitingBlades = player.GetComponent<OrbitingBlades>();
                if (_vortexShockwave == null) _vortexShockwave = player.GetComponent<VortexShockwave>();
            }
        }

        public void SetupCards(GameObject panel, UpgradeCardUI[] cards)
        {
            levelUpPanel = panel;
            upgradeCards = cards;
        }

        private void ShowLevelUpSelection(int newLevel)
        {
            LocatePlayerReferences();

            // Pausa o jogo para a seleção de melhoria
            Time.timeScale = 0f;

            if (levelUpPanel != null)
            {
                levelUpPanel.SetActive(true);
            }

            // Constrói o conjunto de opções disponíveis
            List<UpgradeOption> availableOptions = new List<UpgradeOption>();

            // 1. Evolução da Arma Pulse Blaster (se não estiver no nível máximo)
            if (_pulseBlaster != null && _pulseBlaster.CanUpgrade)
            {
                int nextLvl = _pulseBlaster.currentLevel + 1;
                string title = _pulseBlaster.currentLevel == 0 ? "Nova Arma: Pulse Blaster" : $"Pulse Blaster (Nv {nextLvl})";
                availableOptions.Add(new UpgradeOption
                {
                    Title = title,
                    Description = _pulseBlaster.GetNextUpgradeDescription(),
                    ApplyAction = () => _pulseBlaster.UpgradeWeapon()
                });
            }

            // 2. Evolução da Arma Orbiting Blades (se não estiver no nível máximo)
            if (_orbitingBlades != null && _orbitingBlades.CanUpgrade)
            {
                int nextLvl = _orbitingBlades.currentLevel + 1;
                string title = _orbitingBlades.currentLevel == 0 ? "Nova Arma: Orbiting Blades" : $"Orbiting Blades (Nv {nextLvl})";
                availableOptions.Add(new UpgradeOption
                {
                    Title = title,
                    Description = _orbitingBlades.GetNextUpgradeDescription(),
                    ApplyAction = () => _orbitingBlades.UpgradeWeapon()
                });
            }

            // 3. Evolução da Arma Vortex Shockwave (se não estiver no nível máximo)
            if (_vortexShockwave != null && _vortexShockwave.CanUpgrade)
            {
                int nextLvl = _vortexShockwave.currentLevel + 1;
                string title = _vortexShockwave.currentLevel == 0 ? "Nova Arma: Vortex Shockwave" : $"Vortex Shockwave (Nv {nextLvl})";
                availableOptions.Add(new UpgradeOption
                {
                    Title = title,
                    Description = _vortexShockwave.GetNextUpgradeDescription(),
                    ApplyAction = () => _vortexShockwave.UpgradeWeapon()
                });
            }

            // 2. Propulsores de Núcleo (+15% Move Speed)
            availableOptions.Add(new UpgradeOption
            {
                Title = "Propulsores de Núcleo",
                Description = "+15% Velocidade de Movimentação no plano de batalha.",
                ApplyAction = () =>
                {
                    if (_playerStats != null) _playerStats.UpgradeMoveSpeed(0.15f);
                }
            });

            // 3. Campo Magnético (+30% Pickup Radius)
            availableOptions.Add(new UpgradeOption
            {
                Title = "Campo Magnético",
                Description = "+30% Raio magnético para atração de Cristais de XP.",
                ApplyAction = () =>
                {
                    if (_playerStats != null) _playerStats.UpgradePickupRadius(0.30f);
                }
            });

            // 4. Overclock de Sistema (-10% Cooldown)
            availableOptions.Add(new UpgradeOption
            {
                Title = "Overclock de Sistema",
                Description = "-10% Tempo de recarga global de todas as armas.",
                ApplyAction = () =>
                {
                    if (_playerStats != null) _playerStats.UpgradeCooldownReduction(0.10f);
                }
            });

            // Embaralha as opções para gerar seleção aleatória
            for (int i = 0; i < availableOptions.Count; i++)
            {
                int rnd = UnityEngine.Random.Range(i, availableOptions.Count);
                var temp = availableOptions[i];
                availableOptions[i] = availableOptions[rnd];
                availableOptions[rnd] = temp;
            }

            // Popula os 3 cartões
            int cardCount = Mathf.Min(upgradeCards.Length, availableOptions.Count);
            for (int i = 0; i < upgradeCards.Length; i++)
            {
                if (i < cardCount)
                {
                    var card = upgradeCards[i];
                    var option = availableOptions[i];

                    if (card.cardRoot != null) card.cardRoot.SetActive(true);
                    if (card.titleText != null) card.titleText.text = option.Title;
                    if (card.descriptionText != null) card.descriptionText.text = option.Description;

                    if (card.selectButton != null)
                    {
                        card.selectButton.onClick.RemoveAllListeners();
                        card.selectButton.onClick.AddListener(() => SelectUpgrade(option));
                    }
                }
                else
                {
                    if (upgradeCards[i].cardRoot != null)
                    {
                        upgradeCards[i].cardRoot.SetActive(false);
                    }
                }
            }
        }

        private void SelectUpgrade(UpgradeOption option)
        {
            option.ApplyAction?.Invoke();

            if (levelUpPanel != null)
            {
                levelUpPanel.SetActive(false);
            }

            // Retoma a execução do jogo
            Time.timeScale = 1f;
        }
    }
}

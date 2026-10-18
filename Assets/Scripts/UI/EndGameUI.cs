using UnityEngine;
using UnityEngine.UI;
using Vortex.Core;

namespace Vortex.UI
{
    /// <summary>
    /// Gerencia a exibição da tela de Fim de Jogo (Vitória ou Game Over),
    /// apresentando o resultado, o tempo sobrevivido, total de inimigos eliminados
    /// e botão de reinício da partida.
    /// </summary>
    public class EndGameUI : MonoBehaviour
    {
        [Header("Painel Principal")]
        [SerializeField] private GameObject endGamePanel;

        [Header("Elementos de Texto")]
        [SerializeField] private Text resultHeaderText;
        [SerializeField] private Text resultSubtitleText;
        [SerializeField] private Text survivalTimeText;
        [SerializeField] private Text totalKillsText;

        [Header("Botão de Reinício")]
        [SerializeField] private Button restartButton;

        private void Awake()
        {
            if (endGamePanel != null)
            {
                endGamePanel.SetActive(false);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(OnRestartClicked);
            }
        }

        private void OnEnable()
        {
            EventBus.OnGameEnded += HandleGameEnded;
        }

        private void OnDisable()
        {
            EventBus.OnGameEnded -= HandleGameEnded;
        }

        public void SetupReferences(
            GameObject panel, 
            Text headerTxt, 
            Text subTxt, 
            Text timeTxt, 
            Text killsTxt, 
            Button btn)
        {
            endGamePanel = panel;
            resultHeaderText = headerTxt;
            resultSubtitleText = subTxt;
            survivalTimeText = timeTxt;
            totalKillsText = killsTxt;
            restartButton = btn;

            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (endGamePanel != null)
            {
                endGamePanel.SetActive(false);
            }
        }

        private void HandleGameEnded(bool isVictory, float survivalTime, int totalKills)
        {
            if (endGamePanel != null)
            {
                endGamePanel.SetActive(true);
            }

            if (resultHeaderText != null)
            {
                if (isVictory)
                {
                    resultHeaderText.text = "VITÓRIA";
                    resultHeaderText.color = new Color(0.0f, 1.0f, 0.85f); // Neon Cyan
                }
                else
                {
                    resultHeaderText.text = "GAME OVER";
                    resultHeaderText.color = new Color(1.0f, 0.2f, 0.35f); // Neon Crimson
                }
            }

            if (resultSubtitleText != null)
            {
                resultSubtitleText.text = isVictory 
                    ? "A anomalia espacial foi contida e o núcleo quântico estabilizou!" 
                    : "A integridade estrutural do núcleo chegou ao limite crítico.";
            }

            if (survivalTimeText != null)
            {
                int minutes = Mathf.FloorToInt(survivalTime / 60f);
                int seconds = Mathf.FloorToInt(survivalTime % 60f);
                survivalTimeText.text = $"Tempo Sobrevivido: {minutes:00}:{seconds:00}";
            }

            if (totalKillsText != null)
            {
                totalKillsText.text = $"Inimigos Derrotados: {totalKills}";
            }
        }

        private void OnRestartClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
            else
            {
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}

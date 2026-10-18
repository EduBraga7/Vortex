using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Vortex.Core;

namespace Vortex.UI
{
    /// <summary>
    /// Gerencia a Tela Inicial (Menu Principal) de Vortex:
    /// - Exibe o painel de boas-vindas com título, instrução e botões de ação.
    /// - Botão "Jogar": Inicia a partida (ou carrega a cena de gameplay / reinicia o fluxo).
    /// - Botão "Sair": Fecha a aplicação (Application.Quit) ou para o Play Mode no Editor.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        public static MainMenuUI Instance { get; private set; }

        /// <summary>
        /// Flag estática para pular a exibição do Menu Principal quando a cena é recarregada
        /// após uma reinicialização de partida (Pause ou Fim de Jogo).
        /// </summary>
        public static bool SkipMenuOnNextLoad { get; set; } = false;

        [Header("Painel Principal")]
        [SerializeField] private GameObject mainMenuPanel;

        [Header("Botões de Ação")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button quitButton;

        [Header("Configurações de Cena")]
        [Tooltip("Nome da cena de gameplay caso utilize cenas separadas. Deixe vazio para fluxo em cena única.")]
        [SerializeField] private string gameplaySceneName = "";

        [Tooltip("Se verdadeiro e o painel estiver ativo no Awake, pausa o jogo em Time.timeScale = 0 até o jogador clicar em Jogar.")]
        [SerializeField] private bool pauseTimeWhileActive = true;

        public bool IsMenuOpen => mainMenuPanel != null && mainMenuPanel.activeSelf;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            RegisterButtonListeners();
        }

        private void Start()
        {
            if (SkipMenuOnNextLoad)
            {
                SkipMenuOnNextLoad = false;
                if (mainMenuPanel != null)
                {
                    mainMenuPanel.SetActive(false);
                }
                Time.timeScale = 1f;
                return;
            }

            if (mainMenuPanel != null && mainMenuPanel.activeSelf && pauseTimeWhileActive)
            {
                Time.timeScale = 0f;
            }
        }

        private void RegisterButtonListeners()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(PlayGame);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(QuitGame);
            }
        }

        /// <summary>
        /// Vincula as referências da UI de forma procedural através do VortexSceneBuilder.
        /// </summary>
        public void SetupReferences(GameObject panel, Button playBtn, Button quitBtn, string sceneName = "")
        {
            mainMenuPanel = panel;
            playButton = playBtn;
            quitButton = quitBtn;
            gameplaySceneName = sceneName;

            RegisterButtonListeners();
        }

        /// <summary>
        /// Ação do Botão "Jogar":
        /// - Carrega a cena informada ou oculta o painel e destrava o tempo de jogo.
        /// - Reinicia o loop se a partida anterior já havia sido encerrada.
        /// </summary>
        public void PlayGame()
        {
            Time.timeScale = 1f;

            if (!string.IsNullOrEmpty(gameplaySceneName))
            {
                SceneManager.LoadScene(gameplaySceneName);
                return;
            }

            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }

            if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
            {
                GameManager.Instance.RestartGame();
            }

            Debug.Log("<color=#00FFFF><b>[MainMenuUI]</b></color> Partida iniciada!");
        }

        /// <summary>
        /// Ação do Botão "Sair":
        /// Encerra a aplicação ou interrompe o Play Mode se executado no Editor.
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("<color=#FF3366><b>[MainMenuUI]</b></color> Encerrando aplicação...");
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        /// <summary>
        /// Abre manualmente o Menu Principal (congelando o tempo se configurado).
        /// </summary>
        public void OpenMenu()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
            }

            if (pauseTimeWhileActive)
            {
                Time.timeScale = 0f;
            }
        }

        /// <summary>
        /// Fecha manualmente o Menu Principal e retoma o tempo normal.
        /// </summary>
        public void CloseMenu()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }

            Time.timeScale = 1f;
        }
    }
}

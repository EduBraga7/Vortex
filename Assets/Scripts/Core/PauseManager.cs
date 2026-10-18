using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Vortex.UI;

namespace Vortex.Core
{
    /// <summary>
    /// Gerencia o estado de pausa e a janela de Menu de Pausa de Vortex:
    /// - Escuta a tecla ESC (KeyCode.Escape ou Input System) durante o gameplay.
    /// - Alterna Time.timeScale entre 0f e 1f, exibindo ou ocultando o painel de pause.
    /// - Métodos públicos para os botões da UI: ResumeGame(), RestartGame() e QuitToDesktop().
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        public static PauseManager Instance { get; private set; }

        [Header("Painel de Pause")]
        [SerializeField] private GameObject pausePanel;

        [Header("Botões de Ação")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;

        private bool _isPaused;

        public bool IsPaused => _isPaused;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            RegisterButtonListeners();

            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (IsEscapePressed())
            {
                // Não pausar se a tela de vitória/derrota já estiver ativa
                if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
                {
                    return;
                }

                // Não pausar se a tela inicial estiver aberta
                if (MainMenuUI.Instance != null && MainMenuUI.Instance.IsMenuOpen)
                {
                    return;
                }

                TogglePause();
            }
        }

        private bool IsEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return true;
            }
#endif
            try
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    return true;
                }
            }
            catch
            {
                // Captura exceção quando o Legacy Input Manager está desativado no PlayerSettings
            }

            return false;
        }

        private void RegisterButtonListeners()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(ResumeGame);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(RestartGame);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(QuitToDesktop);
            }
        }

        /// <summary>
        /// Configura as referências de forma procedural através do VortexSceneBuilder.
        /// </summary>
        public void SetupReferences(GameObject panel, Button resumeBtn, Button restartBtn, Button quitBtn)
        {
            pausePanel = panel;
            resumeButton = resumeBtn;
            restartButton = restartBtn;
            quitButton = quitBtn;

            RegisterButtonListeners();

            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
        }

        /// <summary>
        /// Alterna o estado de pausa durante o gameplay.
        /// </summary>
        public void TogglePause()
        {
            if (_isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        /// <summary>
        /// Pausa o gameplay, define Time.timeScale = 0f e exibe o painel de pause.
        /// </summary>
        public void PauseGame()
        {
            _isPaused = true;
            Time.timeScale = 0f;

            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }

            Debug.Log("<color=#00FFFF><b>[PauseManager]</b></color> Jogo pausado.");
        }

        /// <summary>
        /// Retoma o gameplay, define Time.timeScale = 1f e oculta o painel de pause.
        /// </summary>
        public void ResumeGame()
        {
            _isPaused = false;
            Time.timeScale = 1f;

            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            Debug.Log("<color=#00FF99><b>[PauseManager]</b></color> Jogo retomado.");
        }

        /// <summary>
        /// Reinicia a partida imediatamente a partir do menu de pausa.
        /// </summary>
        public void RestartGame()
        {
            _isPaused = false;
            Time.timeScale = 1f;

            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            MainMenuUI.SkipMenuOnNextLoad = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
            else
            {
                EventBus.ClearAllSubscribers();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            Debug.Log("<color=#FFA500><b>[PauseManager]</b></color> Partida reiniciada.");
        }

        /// <summary>
        /// Fecha a aplicação ou encerra o Play Mode no Editor.
        /// </summary>
        public void QuitToDesktop()
        {
            Time.timeScale = 1f;
            Debug.Log("<color=#FF3366><b>[PauseManager]</b></color> Encerrando aplicação para o desktop...");
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}

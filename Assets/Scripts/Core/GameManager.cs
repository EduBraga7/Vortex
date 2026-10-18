using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vortex.Core
{
    /// <summary>
    /// Gerencia o ciclo de vida da partida de Vortex:
    /// - Cronômetro de sobrevivência de 10 minutos (600s).
    /// - Monitoramento de condição de Vitória (10:00) e Derrota (HP do Player = 0).
    /// - Disparo de evento desacoplado OnGameEnded e recarregamento da cena ao jogar novamente.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Regras da Partida")]
        [SerializeField] private float matchDurationSeconds = 600f; // 10 minutos

        private float _survivalTimer;
        private int _totalKills;
        private bool _isGameEnded;

        public float MatchDurationSeconds => matchDurationSeconds;
        public float SurvivalTimer => _survivalTimer;
        public int TotalKills => _totalKills;
        public bool IsGameEnded => _isGameEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.OnEnemyKilled += HandleEnemyKilled;
            EventBus.OnPlayerDeath += HandlePlayerDeath;
        }

        private void OnDisable()
        {
            EventBus.OnEnemyKilled -= HandleEnemyKilled;
            EventBus.OnPlayerDeath -= HandlePlayerDeath;
        }

        private void Start()
        {
            _survivalTimer = 0f;
            _totalKills = 0;
            _isGameEnded = false;
        }

        private void Update()
        {
            if (_isGameEnded) return;
            if (Time.timeScale <= 0f) return;

            _survivalTimer += Time.deltaTime;

            if (_survivalTimer >= matchDurationSeconds)
            {
                TriggerGameEnd(isVictory: true);
            }
        }

        private void HandleEnemyKilled()
        {
            _totalKills++;
        }

        private void HandlePlayerDeath()
        {
            if (_isGameEnded) return;
            TriggerGameEnd(isVictory: false);
        }

        public void TriggerGameEnd(bool isVictory)
        {
            if (_isGameEnded) return;
            _isGameEnded = true;

            Time.timeScale = 0f;

            if (isVictory)
            {
                Debug.Log("<color=#00FFFF><b>[VITÓRIA QUÂNTICA]</b></color> Você sobreviveu aos 10 minutos de anomalia!");
            }
            else
            {
                Debug.Log("<color=#FF3366><b>[GAME OVER]</b></color> O núcleo colapsou antes da estabilização.");
            }

            EventBus.RaiseGameEnded(isVictory, _survivalTimer, _totalKills);
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            EventBus.ClearAllSubscribers();
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.buildIndex);
        }
    }
}

using UnityEngine;
using Vortex.Core;

namespace Vortex.Enemies
{
    /// <summary>
    /// Gerencia o nascimento escalonado de hordas de inimigos seguindo a progressão de 10 minutos do GDD:
    /// - 0:00 a 2:00: Apenas Swarmers (1 a cada 1.5s)
    /// - 2:00 a 5:00: Hordas mistas de Swarmers e Bruisers (3 a cada 1.0s)
    /// - 5:00 a 10:00: Hordas densas contínuas + Spawn do CoreBoss aos 5:00 e 10:00.
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [Header("Configurações de Spawn")]
        [SerializeField] private float spawnRadius = 22f;
        [SerializeField] private float spawnHeight = 0.8f;

        [Header("Tags de Inimigos no Pool")]
        [SerializeField] private string swarmerTag = "Swarmer";
        [SerializeField] private string bruiserTag = "Bruiser";
        [SerializeField] private string coreBossTag = "CoreBoss";

        [Header("Referências")]
        [SerializeField] private Transform playerTransform;

        private float _matchTimer;
        private float _spawnIntervalTimer;
        private bool _boss5MinSpawned;
        private bool _boss10MinSpawned;

        public float MatchTimer => _matchTimer;

        public void SetPlayer(Transform player)
        {
            playerTransform = player;
        }

        private void Start()
        {
            FindPlayerIfMissing();
            _matchTimer = 0f;
            _spawnIntervalTimer = 0f;
            _boss5MinSpawned = false;
            _boss10MinSpawned = false;
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayerIfMissing();
                if (playerTransform == null) return;
            }

            if (Time.timeScale <= 0f) return;

            _matchTimer += Time.deltaTime;
            _spawnIntervalTimer += Time.deltaTime;

            // Verificação de Boss Spawns nos marcos exatos do GDD
            CheckBossSpawns();

            // Progressão das Ondas com base no tempo de partida
            float currentInterval;
            int currentBatchCount;

            if (_matchTimer < 120f) // 0:00 a 2:00
            {
                currentInterval = 1.5f;
                currentBatchCount = 1;
            }
            else if (_matchTimer < 300f) // 2:00 a 5:00
            {
                currentInterval = 1.0f;
                currentBatchCount = 3;
            }
            else // 5:00 a 10:00
            {
                currentInterval = 0.8f;
                currentBatchCount = 5;
            }

            if (_spawnIntervalTimer >= currentInterval)
            {
                _spawnIntervalTimer = 0f;
                SpawnBatch(currentBatchCount);
            }
        }

        private void CheckBossSpawns()
        {
            if (!_boss5MinSpawned && _matchTimer >= 300f)
            {
                _boss5MinSpawned = true;
                SpawnBoss();
                Debug.Log("<color=#FF0055><b>[ALERTA ANOMALIA]</b></color> Core Boss (Marco 5:00) detectado!");
            }

            if (!_boss10MinSpawned && _matchTimer >= 595f)
            {
                _boss10MinSpawned = true;
                SpawnBoss();
                Debug.Log("<color=#FF0055><b>[ALERTA ANOMALIA CRÍTICA]</b></color> Core Boss Final (Marco 10:00) manifestado!");
            }
        }

        private void SpawnBatch(int count)
        {
            if (ObjectPooler.Instance == null) return;

            for (int i = 0; i < count; i++)
            {
                string tagToSpawn = swarmerTag;

                if (_matchTimer >= 120f)
                {
                    // A partir de 2:00, 35% de chance de spawnar Bruiser
                    if (Random.value < 0.35f)
                    {
                        tagToSpawn = bruiserTag;
                    }
                }

                SpawnSingleEnemy(tagToSpawn);
            }
        }

        private void SpawnBoss()
        {
            if (ObjectPooler.Instance == null) return;
            SpawnSingleEnemy(coreBossTag, heightOffset: 1.5f);
        }

        private void SpawnSingleEnemy(string poolTag, float heightOffset = 0f)
        {
            // Posição aleatória em anel ao redor do Player
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnRadius;
            Vector3 spawnPosition = playerTransform.position + offset;
            spawnPosition.y = spawnHeight + heightOffset;

            ObjectPooler.Instance.SpawnFromPool(poolTag, spawnPosition, Quaternion.identity);
        }

        private void FindPlayerIfMissing()
        {
            if (playerTransform != null) return;

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (playerTransform != null)
            {
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
                Gizmos.DrawWireSphere(playerTransform.position, spawnRadius);
            }
        }
    }
}

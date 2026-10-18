using System.Collections.Generic;
using UnityEngine;

namespace Vortex.Core
{
    /// <summary>
    /// Singleton performático de Object Pooling para gerenciar instanciamento e reciclagem de entidades.
    /// Suporta pré-configuração pelo Inspector e registro dinâmico via código.
    /// </summary>
    public class ObjectPooler : MonoBehaviour
    {
        [System.Serializable]
        public class PoolConfig
        {
            public string tag;
            public GameObject prefab;
            public int initialSize = 20;
        }

        public static ObjectPooler Instance { get; private set; }

        [Header("Configurações das Pools")]
        [SerializeField] private List<PoolConfig> pools = new List<PoolConfig>();

        private readonly Dictionary<string, Queue<GameObject>> _poolDictionary = new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<string, GameObject> _prefabLookup = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, Transform> _poolParents = new Dictionary<string, Transform>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeConfiguredPools();
        }

        /// <summary>
        /// Inicializa todas as pools cadastradas na lista serializável.
        /// </summary>
        public void InitializeConfiguredPools()
        {
            if (pools == null) return;

            foreach (var pool in pools)
            {
                if (pool == null || pool.prefab == null || string.IsNullOrEmpty(pool.tag)) continue;
                if (!_poolDictionary.ContainsKey(pool.tag))
                {
                    CreatePool(pool.tag, pool.prefab, pool.initialSize);
                }
            }
        }

        /// <summary>
        /// Cria uma nova pool com a tag, prefab e quantidade especificados.
        /// </summary>
        public void CreatePool(string tag, GameObject prefab, int size)
        {
            if (string.IsNullOrEmpty(tag) || prefab == null) return;

            if (!_poolDictionary.ContainsKey(tag))
            {
                _poolDictionary[tag] = new Queue<GameObject>();
                _prefabLookup[tag] = prefab;

                GameObject parentObj = new GameObject($"Pool_{tag}");
                parentObj.transform.SetParent(transform);
                _poolParents[tag] = parentObj.transform;
            }

            Queue<GameObject> objectQueue = _poolDictionary[tag];
            Transform parentTransform = _poolParents[tag];

            for (int i = 0; i < size; i++)
            {
                GameObject obj = Instantiate(prefab, parentTransform);
                obj.name = $"{prefab.name}_{i:D3}";
                obj.SetActive(false);
                objectQueue.Enqueue(obj);
            }
        }

        /// <summary>
        /// Obtém um objeto da pool, posiciona, rotaciona e ativa.
        /// Se a pool estiver esgotada, expande dinamicamente para não travar o gameplay.
        /// </summary>
        public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
        {
            if (!_poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"[ObjectPooler] A pool '{tag}' não existe ou não foi inicializada.");
                return null;
            }

            Queue<GameObject> queue = _poolDictionary[tag];
            GameObject objToSpawn = null;

            while (queue.Count > 0)
            {
                var candidate = queue.Dequeue();
                if (candidate != null)
                {
                    objToSpawn = candidate;
                    break;
                }
            }

            if (objToSpawn == null)
            {
                // Expansão dinâmica sob demanda
                if (_prefabLookup.TryGetValue(tag, out GameObject prefab) && prefab != null)
                {
                    Transform parentTransform = _poolParents.TryGetValue(tag, out var pt) ? pt : transform;
                    objToSpawn = Instantiate(prefab, parentTransform);
                }
                else
                {
                    Debug.LogWarning($"[ObjectPooler] A pool '{tag}' esgotou e não possui prefab válido para expansão.");
                    return null;
                }
            }

            objToSpawn.transform.SetPositionAndRotation(position, rotation);
            objToSpawn.SetActive(true);

            return objToSpawn;
        }

        /// <summary>
        /// Desativa o objeto e retorna-o à fila da pool.
        /// </summary>
        public void ReturnToPool(string tag, GameObject obj)
        {
            if (obj == null) return;

            obj.SetActive(false);

            if (_poolParents.TryGetValue(tag, out Transform parentTransform) && parentTransform != null)
            {
                obj.transform.SetParent(parentTransform);
            }

            if (_poolDictionary.TryGetValue(tag, out Queue<GameObject> queue))
            {
                queue.Enqueue(obj);
            }
            else
            {
                Debug.LogWarning($"[ObjectPooler] Tentativa de retornar objeto para pool inexistente: '{tag}'. Destruindo objeto.");
                Destroy(obj);
            }
        }

        /// <summary>
        /// Registra ou atualiza uma configuração de pool para uso no Editor/SceneBuilder.
        /// </summary>
        public void RegisterPoolConfig(string tag, GameObject prefab, int size)
        {
            if (pools == null) pools = new List<PoolConfig>();

            var existing = pools.Find(p => p.tag == tag);
            if (existing != null)
            {
                existing.prefab = prefab;
                existing.initialSize = size;
            }
            else
            {
                pools.Add(new PoolConfig { tag = tag, prefab = prefab, initialSize = size });
            }
        }
    }
}

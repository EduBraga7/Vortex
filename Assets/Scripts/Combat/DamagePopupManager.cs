using UnityEngine;
using Vortex.Core;

namespace Vortex.Combat
{
    /// <summary>
    /// Gerencia o spawn de números de dano flutuantes a partir do ObjectPooler.
    /// Reside em _GameplayManagers como um componente singleton direto e leve.
    /// </summary>
    public class DamagePopupManager : MonoBehaviour
    {
        public static DamagePopupManager Instance { get; private set; }

        private const string PoolTag = "DamagePopup";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Spawna um número flutuante de dano na posição do alvo.
        /// </summary>
        public void ShowDamage(Vector3 worldPosition, float damageAmount, Color? customColor = null)
        {
            if (ObjectPooler.Instance == null) return;

            Vector3 spawnPos = worldPosition + new Vector3(
                Random.Range(-0.2f, 0.2f),
                0.8f,
                Random.Range(-0.2f, 0.2f)
            );

            GameObject popupObj = ObjectPooler.Instance.SpawnFromPool(PoolTag, spawnPos, Quaternion.identity);
            if (popupObj != null && popupObj.TryGetComponent<DamagePopup>(out var popup))
            {
                popup.Setup(damageAmount, spawnPos, customColor);
            }
        }
    }
}

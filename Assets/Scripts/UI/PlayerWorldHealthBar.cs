using UnityEngine;
using UnityEngine.UI;
using Vortex.Core;

namespace Vortex.UI
{
    /// <summary>
    /// Barra de vida acoplada embaixo do Player em um pequeno Canvas World Space.
    /// Acompanha a posição do Player no plano XZ mantendo a rotação fixa voltada para a câmera,
    /// sem girar quando a malha do Player rotaciona.
    /// </summary>
    public class PlayerWorldHealthBar : MonoBehaviour
    {
        [Header("Alvo e Posicionamento")]
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0.05f, -1.15f);
        [SerializeField] private Vector3 fixedRotation = new Vector3(60f, 0f, 0f);

        [Header("Elementos de UI")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Image healthBarBackground;

        [Header("Feedback Visual")]
        [SerializeField] private Color fullHealthColor = new Color(0.1f, 0.95f, 0.5f, 1f);
        [SerializeField] private Color midHealthColor = new Color(1.0f, 0.85f, 0.1f, 1f);
        [SerializeField] private Color lowHealthColor = new Color(1.0f, 0.2f, 0.2f, 1f);

        private void OnEnable()
        {
            EventBus.OnPlayerHealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            EventBus.OnPlayerHealthChanged -= HandleHealthChanged;
        }

        private void Start()
        {
            if (playerTarget == null)
            {
                GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
                if (player != null)
                {
                    playerTarget = player.transform;
                }
            }

            transform.rotation = Quaternion.Euler(fixedRotation);
        }

        private void LateUpdate()
        {
            if (playerTarget == null)
            {
                GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
                if (player != null)
                {
                    playerTarget = player.transform;
                }
                else return;
            }

            transform.position = playerTarget.position + offset;
            transform.rotation = Quaternion.Euler(fixedRotation);
        }

        private void HandleHealthChanged(float currentHP, float maxHP)
        {
            if (healthBarFill == null) return;

            float ratio = maxHP > 0f ? Mathf.Clamp01(currentHP / maxHP) : 0f;
            healthBarFill.fillAmount = ratio;

            if (ratio > 0.5f)
            {
                healthBarFill.color = Color.Lerp(midHealthColor, fullHealthColor, (ratio - 0.5f) * 2f);
            }
            else
            {
                healthBarFill.color = Color.Lerp(lowHealthColor, midHealthColor, ratio * 2f);
            }
        }

        public void Setup(Transform target, Image fill, Image background)
        {
            playerTarget = target;
            healthBarFill = fill;
            healthBarBackground = background;
            transform.rotation = Quaternion.Euler(fixedRotation);
        }
    }
}

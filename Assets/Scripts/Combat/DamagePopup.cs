using UnityEngine;
using Vortex.Core;

namespace Vortex.Combat
{
    /// <summary>
    /// Exibe números flutuantes de dano no espaço 3D.
    /// Flutua suavemente para cima com leve dispersão horizontal, desvanece
    /// e retorna à pool "DamagePopup" após 0.6s.
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        [Header("Configurações do Popup")]
        [SerializeField] private TextMesh textMesh;
        [SerializeField] private float lifetime = 0.6f;
        [SerializeField] private float floatSpeed = 2.4f;

        private float _elapsedTime;
        private Color _baseColor;
        private Vector3 _driftVelocity;

        private void Awake()
        {
            if (textMesh == null)
            {
                textMesh = GetComponent<TextMesh>() ?? GetComponentInChildren<TextMesh>();
            }
        }

        /// <summary>
        /// Inicializa o texto, posição e direção do popup de dano.
        /// </summary>
        public void Setup(float damage, Vector3 position, Color? customColor = null)
        {
            transform.position = position;
            _elapsedTime = 0f;

            // Cor padrão vibrante amarela/laranja neon para visibilidade máxima contra o piso escuro
            _baseColor = customColor ?? new Color(1.0f, 0.88f, 0.2f, 1.0f);

            if (textMesh != null)
            {
                textMesh.text = Mathf.RoundToInt(damage).ToString();
                textMesh.color = _baseColor;
            }

            // Dispersão sutil no plano XZ
            float randomX = Random.Range(-0.5f, 0.5f);
            float randomZ = Random.Range(-0.25f, 0.25f);
            _driftVelocity = new Vector3(randomX, floatSpeed, randomZ);

            AlignWithCamera();
        }

        private void Update()
        {
            _elapsedTime += Time.deltaTime;
            transform.position += _driftVelocity * Time.deltaTime;

            AlignWithCamera();

            float progress = _elapsedTime / lifetime;
            if (textMesh != null)
            {
                Color c = _baseColor;
                c.a = Mathf.Clamp01(1f - (progress * progress)); // Curva suave de desaparecimento
                textMesh.color = c;
            }

            if (_elapsedTime >= lifetime)
            {
                if (ObjectPooler.Instance != null)
                {
                    ObjectPooler.Instance.ReturnToPool("DamagePopup", gameObject);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        private void AlignWithCamera()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                transform.forward = cam.transform.forward;
            }
        }
    }
}

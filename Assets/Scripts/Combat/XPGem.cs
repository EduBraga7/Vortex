using UnityEngine;
using Vortex.Core;
using Vortex.Player;

namespace Vortex.Combat
{
    /// <summary>
    /// Cristal de Energia (XP) dropado por inimigos eliminados.
    /// Flutua no solo até ser atraído pelo raio magnético do jogador e adiciona XP ao contato.
    /// </summary>
    public class XPGem : MonoBehaviour
    {
        [Header("Configurações de XP")]
        [SerializeField] private float xpValue = 10f;
        [SerializeField] private string poolTag = "XPGem";

        [Header("Cinemática de Atração")]
        [SerializeField] private float attractSpeed = 16f;
        [SerializeField] private float collectDistance = 0.6f;

        [Header("Animação Procedural (Solo)")]
        [SerializeField] private float spinSpeed = 90f;
        [SerializeField] private float bobSpeed = 3f;
        [SerializeField] private float bobHeight = 0.15f;

        private Transform _target;
        private bool _isAttracted;
        private float _baseHeight;
        private Vector3 _spawnPosition;

        private void OnEnable()
        {
            _isAttracted = false;
            _target = null;
            _spawnPosition = transform.position;
            _baseHeight = transform.position.y;
        }

        private void Update()
        {
            if (_isAttracted && _target != null)
            {
                FlyTowardsTarget();
            }
            else
            {
                IdleFloatAnimation();
            }
        }

        /// <summary>
        /// Animação de rotação e flutuação suave senoidal no chão enquanto não atraído.
        /// </summary>
        private void IdleFloatAnimation()
        {
            transform.Rotate(Vector3.up * (spinSpeed * Time.deltaTime), Space.World);

            float newY = _baseHeight + (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        /// <summary>
        /// Movimenta o cristal rapidamente em direção ao núcleo do Player e efetua a coleta.
        /// </summary>
        private void FlyTowardsTarget()
        {
            Vector3 targetPos = _target.position + Vector3.up * 0.5f;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, attractSpeed * Time.deltaTime);

            // Rotação acelerada durante o voo
            transform.Rotate(Vector3.up * (spinSpeed * 2.5f * Time.deltaTime), Space.World);

            float distance = Vector3.Distance(transform.position, targetPos);
            if (distance < collectDistance)
            {
                Collect();
            }
        }

        /// <summary>
        /// Inicia a atração magnética em direção ao Player.
        /// </summary>
        public void AttractTo(Transform target)
        {
            if (_isAttracted) return;

            _isAttracted = true;
            _target = target;
        }

        private void Collect()
        {
            if (_target != null)
            {
                PlayerStats stats = _target.GetComponent<PlayerStats>() ?? _target.GetComponentInParent<PlayerStats>();
                if (stats != null)
                {
                    stats.AddXP(xpValue);
                }
            }

            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.ReturnToPool(poolTag, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}

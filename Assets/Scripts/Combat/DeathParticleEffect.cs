using System.Collections;
using UnityEngine;
using Vortex.Core;

namespace Vortex.Combat
{
    /// <summary>
    /// Gerencia a explosão de partículas low-poly ao eliminar um inimigo.
    /// Ajusta dinamicamente a cor da emissão para a cor da entidade eliminada
    /// e retorna autonomamente ao ObjectPooler após 1 segundo.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class DeathParticleEffect : MonoBehaviour
    {
        [Header("Configurações do Efeito")]
        [SerializeField] private ParticleSystem particleSys;
        [SerializeField] private float returnDelay = 1.0f;

        private Coroutine _returnCoroutine;

        private void Awake()
        {
            if (particleSys == null)
            {
                particleSys = GetComponent<ParticleSystem>();
            }
        }

        /// <summary>
        /// Configura a cor das partículas e dispara o sistema de partículas, iniciando o retorno à pool.
        /// </summary>
        public void Play(Color burstColor)
        {
            if (particleSys == null)
            {
                particleSys = GetComponent<ParticleSystem>();
            }

            if (particleSys != null)
            {
                var main = particleSys.main;
                main.startColor = new ParticleSystem.MinMaxGradient(burstColor);
                particleSys.Clear();
                particleSys.Play();
            }

            if (_returnCoroutine != null)
            {
                StopCoroutine(_returnCoroutine);
            }
            _returnCoroutine = StartCoroutine(ReturnRoutine());
        }

        private IEnumerator ReturnRoutine()
        {
            yield return new WaitForSeconds(returnDelay);

            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.ReturnToPool("DeathVFX", gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (_returnCoroutine != null)
            {
                StopCoroutine(_returnCoroutine);
                _returnCoroutine = null;
            }
        }
    }
}

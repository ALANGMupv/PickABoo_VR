using PickABoo.AjustesSonido;
using UnityEngine;

namespace PickABoo.Sonido
{
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class SonidoDucha : MonoBehaviour
    {
        ParticleSystem m_Agua;
        AudioSource m_Fuente;

        void Awake()
        {
            m_Agua = GetComponent<ParticleSystem>();
            m_Fuente = AudioDemo.CrearFuente(transform, "Agua de la ducha", CategoriaAudio.Ambiente, 0.60f, 1f, 5.5f);
            m_Fuente.clip = AudioDemo.Clip("sonidoDucha");
            m_Fuente.loop = true;
        }

        void Update()
        {
            bool saleAgua = m_Agua.isPlaying && m_Agua.isEmitting;
            if (saleAgua && !m_Fuente.isPlaying && m_Fuente.clip) m_Fuente.Play();
            if (!saleAgua && m_Fuente.isPlaying) m_Fuente.Stop();
        }

        void OnDisable()
        {
            if (m_Fuente) m_Fuente.Stop();
        }
    }
}

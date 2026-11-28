using System.Collections.Generic;
using PickABoo.AjustesSonido;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PickABoo.Sonido
{
    internal sealed class AudioDemo : MonoBehaviour
    {
        static AudioDemo s_Instancia;
        static readonly Dictionary<string, AudioClip> s_Clips = new();

        AudioSource m_Musica;
        FuenteAudioCategoria m_VolumenMusica;
        AmbienteCasa m_Casa;
        PisadasXR m_Pisadas;
        bool m_Entrada;
        bool m_Explorando;
        bool m_EnVideo;
        bool m_Saliendo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar()
        {
            s_Instancia = null;
            s_Clips.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Iniciar()
        {
            var objeto = new GameObject("Audio de Pick a Boo");
            DontDestroyOnLoad(objeto);
            s_Instancia = objeto.AddComponent<AudioDemo>();
        }

        void Awake()
        {
            m_Musica = CrearFuente(transform, "Música", CategoriaAudio.Ambiente, 0f, 0f, 1f);
            m_Musica.clip = Clip("musicaMenu");
            m_Musica.loop = true;
            m_VolumenMusica = m_Musica.GetComponent<FuenteAudioCategoria>();
            SceneManager.sceneLoaded += EscenaCargada;
        }

        void OnDestroy() => SceneManager.sceneLoaded -= EscenaCargada;

        void EscenaCargada(Scene escena, LoadSceneMode modo)
        {
            m_EnVideo = escena.name == "Video360";
            if (escena.name == "Menu")
            {
                m_Entrada = true;
                m_Explorando = false;
                m_Saliendo = false;
            }

            if (escena.name == "MainDemo")
            {
                m_Explorando = !SceneManager.GetSceneByName("Menu").isLoaded;
                m_Entrada = !m_Explorando;
                BuscarOrigen(escena);
            }

            if (m_EnVideo)
            {
                m_Entrada = m_Explorando = false;
                m_Musica.Stop();
                FijarVolumen(m_VolumenMusica, 0f);
            }
        }

        void BuscarOrigen(Scene escena)
        {
            foreach (var raiz in escena.GetRootGameObjects())
            {
                var origen = raiz.GetComponentInChildren<XROrigin>();
                if (!origen) continue;
                m_Pisadas = origen.gameObject.AddComponent<PisadasXR>();
                m_Casa = origen.gameObject.AddComponent<AmbienteCasa>();
                break;
            }
        }

        void Update()
        {
            float objetivo = m_Saliendo || m_EnVideo ? 0f : m_Entrada ? 0.25f : m_Explorando ? 0.035f : 0.025f;
            if (!m_Musica.isPlaying && objetivo > 0f && m_Musica.clip) m_Musica.Play();
            FijarVolumen(m_VolumenMusica, Mathf.MoveTowards(m_VolumenMusica.volumenBase, objetivo, Time.unscaledDeltaTime * 0.35f));
            if (m_Casa) m_Casa.Explorando = m_Explorando && !m_Saliendo;
            if (m_Pisadas) m_Pisadas.Activas = !m_Saliendo;
        }

        internal static void ComenzarViaje(Vector3 posicion)
        {
            if (!s_Instancia) return;
            s_Instancia.m_Entrada = false;
            Efecto("sonidoTransicion", posicion, 0.53f, mezcla3D: 0.25f);
        }

        internal static void LlegarACasa()
        {
            if (s_Instancia) s_Instancia.m_Explorando = true;
        }

        internal static void AtenuarParaSalir()
        {
            if (s_Instancia) s_Instancia.m_Saliendo = true;
        }

        internal static AudioClip Clip(string nombre)
        {
            if (!s_Clips.TryGetValue(nombre, out var clip))
            {
                clip = Resources.Load<AudioClip>("PickABooAudio/" + nombre);
                s_Clips.Add(nombre, clip);
                if (!clip) Debug.LogWarning($"Falta el clip de audio {nombre}.");
            }
            return clip;
        }

        internal static AudioSource CrearFuente(Transform padre, string nombre, CategoriaAudio categoria,
            float volumen, float mezcla3D, float alcance)
        {
            var objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);
            var fuente = objeto.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.spatialBlend = mezcla3D;
            fuente.dopplerLevel = 0f;
            fuente.rolloffMode = AudioRolloffMode.Linear;
            fuente.minDistance = 0.45f;
            fuente.maxDistance = alcance;
            var control = objeto.AddComponent<FuenteAudioCategoria>();
            control.categoria = categoria;
            FijarVolumen(control, volumen);
            return fuente;
        }

        internal static void FijarVolumen(FuenteAudioCategoria control, float volumen)
        {
            if (control.volumenBase == volumen) return;
            control.volumenBase = volumen;
            control.Aplicar();
        }

        internal static void Efecto(string nombre, Vector3 posicion, float volumen = 0.55f,
            float tono = 1f, float mezcla3D = 1f, Transform seguir = null)
        {
            if (s_Instancia) Efecto(Clip(nombre), posicion, volumen, tono, mezcla3D, seguir);
        }

        internal static void Efecto(AudioClip clip, Vector3 posicion, float volumen, float tono, float mezcla3D, Transform seguir = null)
        {
            if (!s_Instancia || !clip) return;
            var fuente = CrearFuente(seguir ? seguir : s_Instancia.transform, clip.name,
                CategoriaAudio.Efectos, volumen, mezcla3D, 5f);
            fuente.transform.position = posicion;
            fuente.clip = clip;
            fuente.pitch = tono;
            fuente.Play();
            Destroy(fuente.gameObject, clip.length / Mathf.Abs(tono) + 0.1f);
        }
    }
}

using System;
using System.Collections.Generic;
using PickABoo.AjustesSonido;
using StealthGame;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace PickABoo.Sonido
{
    internal sealed class AmbienteCasa : MonoBehaviour
    {
        static readonly string[] s_Acentos = { "sonidoCrujidoMadera", "sonidoClicLampara", "sonidoSusurros", "sonidoGritosLejanos" };

        readonly List<Transform> m_Ventanas = new();
        readonly List<Transform> m_Lamparas = new();
        XROrigin m_Origen;
        AudioSource m_Fondo;
        AudioSource m_Viento;
        AudioSource m_Zumbido;
        AudioSource m_Acento;
        FuenteAudioCategoria m_VolumenFondo;
        FuenteAudioCategoria m_VolumenViento;
        FuenteAudioCategoria m_VolumenZumbido;
        FuenteAudioCategoria m_VolumenAcento;
        float m_Intensidad;
        float m_SiguienteBusqueda;
        float m_SiguienteAcento;
        float m_InicioAcento;
        float m_FinAcento;
        float m_NivelAcento;
        Transform m_Ventana;
        Transform m_Lampara;

        internal bool Explorando { get; set; }

        void Awake()
        {
            m_Origen = GetComponent<XROrigin>();
            foreach (var raiz in gameObject.scene.GetRootGameObjects())
            {
                foreach (var malla in raiz.GetComponentsInChildren<MeshRenderer>())
                    if (malla.name.EndsWith("_Window", StringComparison.OrdinalIgnoreCase))
                        m_Ventanas.Add(malla.transform);

                foreach (var lampara in raiz.GetComponentsInChildren<LightFlicker>())
                    m_Lamparas.Add(lampara.transform);
            }

            m_Fondo = CrearBucle("sonidoVientoPasillos", 0f, 1f, out m_VolumenFondo);
            m_Viento = CrearBucle("sonidoVientoVentanas", 1f, 9f, out m_VolumenViento);
            m_Zumbido = CrearBucle("sonidoZumbidoLampara", 1f, 4.5f, out m_VolumenZumbido);
            m_Acento = AudioDemo.CrearFuente(transform, "Acentos de la casa", CategoriaAudio.Ambiente, 0f, 1f, 9f);
            m_VolumenAcento = m_Acento.GetComponent<FuenteAudioCategoria>();

            // Las fuentes espaciales permanecen en el mundo aunque el jugador se desplace o gire.
            var fuentes = new GameObject("Ambiente de la casa");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fuentes, gameObject.scene);
            m_Fondo.transform.SetParent(fuentes.transform);
            m_Viento.transform.SetParent(fuentes.transform);
            m_Zumbido.transform.SetParent(fuentes.transform);
            m_Acento.transform.SetParent(fuentes.transform);
        }

        AudioSource CrearBucle(string nombre, float espacio, float alcance, out FuenteAudioCategoria control)
        {
            var fuente = AudioDemo.CrearFuente(transform, nombre, CategoriaAudio.Ambiente, 0f, espacio, alcance);
            fuente.clip = AudioDemo.Clip(nombre);
            fuente.loop = true;
            control = fuente.GetComponent<FuenteAudioCategoria>();
            return fuente;
        }

        void Update()
        {
            if (!m_Origen.Camera) return;
            float tiempo = Time.unscaledTime;
            m_Intensidad = Mathf.MoveTowards(m_Intensidad, Explorando ? 1f : 0f, Time.unscaledDeltaTime * 0.65f);
            if (m_Intensidad <= 0f)
            {
                AjustarBucle(m_Fondo, m_VolumenFondo, 0f);
                AjustarBucle(m_Viento, m_VolumenViento, 0f);
                AjustarBucle(m_Zumbido, m_VolumenZumbido, 0f);
                if (m_Acento.isPlaying) m_Acento.Stop();
                m_SiguienteAcento = tiempo + 8f;
                return;
            }

            Vector3 cabeza = m_Origen.Camera.transform.position;
            if (tiempo >= m_SiguienteBusqueda)
            {
                m_SiguienteBusqueda = tiempo + 0.65f;
                m_Ventana = MasCercano(m_Ventanas, cabeza, 9f);
                m_Lampara = MasCercano(m_Lamparas, cabeza, 4.5f);
                if (m_Ventana) m_Viento.transform.position = m_Ventana.position;
                if (m_Lampara) m_Zumbido.transform.position = m_Lampara.position;
            }

            AjustarBucle(m_Fondo, m_VolumenFondo, m_Intensidad * 0.24f);
            AjustarBucle(m_Viento, m_VolumenViento, m_Ventana ? m_Intensidad * 0.30f : 0f);
            AjustarBucle(m_Zumbido, m_VolumenZumbido, m_Lampara ? m_Intensidad * 0.34f : 0f);

            if (m_Acento.isPlaying)
            {
                if (tiempo >= m_FinAcento) m_Acento.Stop();
                else
                {
                    float fundido = Mathf.Clamp01(Mathf.Min(tiempo - m_InicioAcento, m_FinAcento - tiempo) / 0.3f);
                    AudioDemo.FijarVolumen(m_VolumenAcento, fundido * m_NivelAcento * m_Intensidad);
                }
            }
            if (Explorando && tiempo >= m_SiguienteAcento)
            {
                m_SiguienteAcento = tiempo + UnityEngine.Random.Range(13f, 23f);
                ReproducirAcento(cabeza, tiempo);
            }
        }

        static void AjustarBucle(AudioSource fuente, FuenteAudioCategoria control, float objetivo)
        {
            float volumen = Mathf.MoveTowards(control.volumenBase, objetivo, Time.unscaledDeltaTime * 0.4f);
            AudioDemo.FijarVolumen(control, volumen);
            if (volumen > 0f && fuente.clip && !fuente.isPlaying) fuente.Play();
            if (volumen <= 0f && fuente.isPlaying) fuente.Stop();
        }

        static Transform MasCercano(List<Transform> puntos, Vector3 referencia, float alcance)
        {
            Transform mejor = null;
            float distancia = alcance * alcance;
            foreach (var punto in puntos)
            {
                if (!punto || !punto.gameObject.activeInHierarchy) continue;
                float candidata = (punto.position - referencia).sqrMagnitude;
                if (candidata >= distancia) continue;
                distancia = candidata;
                mejor = punto;
            }
            return mejor;
        }

        void ReproducirAcento(Vector3 cabeza, float tiempo)
        {
            int indice = UnityEngine.Random.Range(0, 10);
            indice = indice < 5 ? 0 : indice < 7 ? 1 : indice < 9 ? 2 : 3;
            if (indice == 1 && !m_Lampara) indice = 0;
            var clip = AudioDemo.Clip(s_Acentos[indice]);
            if (!clip) return;

            float inicio = indice == 1 ? 0.82f : indice < 2 ? 0f : UnityEngine.Random.Range(0f, Mathf.Max(0f, clip.length - 9f));
            float duracion = indice == 1 ? 1.05f : indice == 0 ? 10f : 9f;
            m_NivelAcento = indice == 3 ? 0.11f : indice == 2 ? 0.17f : 0.35f;
            m_Acento.transform.position = indice == 1 ? m_Lampara.position : cabeza + transform.right * 3f;
            m_Acento.clip = clip;
            m_Acento.time = inicio;
            m_InicioAcento = tiempo;
            m_FinAcento = tiempo + Mathf.Min(duracion, clip.length - inicio);
            AudioDemo.FijarVolumen(m_VolumenAcento, 0f);
            m_Acento.Play();
        }
    }
}

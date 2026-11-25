using System;
using System.Collections;
using PickABoo.AjustesSonido;
using PickABoo.Transiciones;
using UnityEngine;
using UnityEngine.Video;

namespace PickABoo.Video360
{
    /// <summary>
    /// Tráiler 360 montado como en el PDF de vídeo 360: VideoPlayer -> RenderTexture -> material Skybox/Panoramic.
    /// Expone los controles que usa <see cref="PanelReproductor360"/> y vuelve al menú con fundido.
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    public class ReproductorVideo360 : MonoBehaviour
    {
        public string escenaVuelta = "Menu";
        public AudioSource fuenteAudio;                 // con FuenteAudioCategoria (Música)
        public Material cielo360;                       // Skybox/Panoramic con la RenderTexture
        [Range(0, 360)] public float orientacionInicial = 0f;   // ajusta hacia dónde mira el vídeo al empezar

        VideoPlayer m_Video;
        FuenteAudioCategoria m_Categoria;
        bool m_Saliendo;

        public event Action Terminado;
        public bool Reproduciendo => m_Video.isPlaying;
        public bool Preparado => m_Video.isPrepared;
        public double Duracion => m_Video.length;
        public double Tiempo => m_Video.time;
        public float Volumen => m_Categoria ? m_Categoria.volumenBase : 1f;
        public bool Silenciado { get; private set; }

        void Awake()
        {
            m_Video = GetComponent<VideoPlayer>();
            m_Categoria = fuenteAudio ? fuenteAudio.GetComponent<FuenteAudioCategoria>() : null;
            m_Video.playOnAwake = false;
            m_Video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            m_Video.EnableAudioTrack(0, true);
            m_Video.SetTargetAudioSource(0, fuenteAudio);
            m_Video.loopPointReached += _ => Terminado?.Invoke();
            if (cielo360) { cielo360.SetFloat("_Rotation", orientacionInicial); RenderSettings.skybox = cielo360; }
        }

        IEnumerator Start()
        {
            var f = Fundido.Instancia;
            if (f) f.Fijar(new Color(CargadorEscenas.ColorEntrada.r, CargadorEscenas.ColorEntrada.g, CargadorEscenas.ColorEntrada.b, 1));
            m_Video.Prepare();
            while (!m_Video.isPrepared) yield return null;
            m_Video.Play();
            if (f) yield return f.Aparecer(1.2f);
        }

        public void PlayPausa() { if (m_Video.isPlaying) m_Video.Pause(); else m_Video.Play(); }

        public void Saltar(float segundos) => IrA(m_Video.time + segundos);

        public void IrA(double segundos)
        {
            if (!m_Video.isPrepared) return;
            m_Video.time = Math.Max(0, Math.Min(segundos, m_Video.length - 0.1));
        }

        public void FijarVolumen(float v)
        {
            if (m_Categoria) { m_Categoria.volumenBase = Mathf.Clamp01(v); m_Categoria.Aplicar(); }
            if (v > 0.001f && Silenciado) Silenciar(false);
        }

        public void Silenciar(bool s)
        {
            Silenciado = s;
            fuenteAudio.mute = s;
        }

        public void VerOtraVez() { IrA(0); m_Video.Play(); }

        public void Salir()
        {
            if (m_Saliendo) return;
            m_Saliendo = true;
            CargadorEscenas.Ir(escenaVuelta, Color.white, 0.6f);
        }
    }
}

using PickABoo.AjustesSonido;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PickABoo.UI
{
    /// <summary>
    /// Ponlo en la raíz de un Canvas: todos sus botones, interruptores y deslizadores suenan al usarse
    /// (categoría Efectos del menú de sonido).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SonidosUI : MonoBehaviour
    {
        public AudioClip clic;
        public AudioClip deslizar;
        [Tooltip("Tiempo mínimo entre dos sonidos de deslizador")]
        public float intervaloDeslizar = 0.08f;

        AudioSource m_Fuente;
        float m_UltimoDeslizar;

        void Awake()
        {
            m_Fuente = GetComponent<AudioSource>();
            m_Fuente.playOnAwake = false;
            m_Fuente.spatialBlend = 0.6f;
            if (!GetComponent<FuenteAudioCategoria>()) gameObject.AddComponent<FuenteAudioCategoria>().categoria = CategoriaAudio.Efectos;
            foreach (var b in GetComponentsInChildren<Button>(true)) b.onClick.AddListener(Clic);
            foreach (var t in GetComponentsInChildren<Toggle>(true)) t.onValueChanged.AddListener(_ => Clic());
            foreach (var s in GetComponentsInChildren<Slider>(true))
                if (s.interactable && s.GetComponent<Selectable>()) s.onValueChanged.AddListener(_ => Deslizar());
        }

        public void Clic() { if (clic) m_Fuente.PlayOneShot(clic); }

        void Deslizar()
        {
            // solo cuando lo mueve el jugador (no cuando el vídeo avanza la barra de tiempo)
            if (deslizar == null || EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null) return;
            if (Time.unscaledTime - m_UltimoDeslizar < intervaloDeslizar) return;
            m_UltimoDeslizar = Time.unscaledTime;
            m_Fuente.PlayOneShot(deslizar, 0.6f);
        }
    }
}

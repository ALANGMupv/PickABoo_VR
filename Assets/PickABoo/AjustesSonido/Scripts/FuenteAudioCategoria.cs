using UnityEngine;

namespace PickABoo.AjustesSonido
{
    /// <summary>
    /// Ponlo junto a cualquier AudioSource del juego y elige su categoría (Efectos o Ambiente).
    /// Su volumen seguirá automáticamente al menú de sonido. "Volumen base" es el volumen propio del sonido.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class FuenteAudioCategoria : MonoBehaviour
    {
        public CategoriaAudio categoria = CategoriaAudio.Efectos;
        [Range(0, 1)] public float volumenBase = 1f;
        AudioSource m_Fuente;

        void Awake() { m_Fuente = GetComponent<AudioSource>(); }
        void OnEnable() { GestorVolumen.Cambio += Aplicar; Aplicar(); }
        void OnDisable() { GestorVolumen.Cambio -= Aplicar; }

        public void Aplicar()
        {
            if (m_Fuente == null) m_Fuente = GetComponent<AudioSource>();
            m_Fuente.volume = volumenBase * GestorVolumen.Efectivo(categoria);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace PickABoo.AjustesSonido
{
    /// <summary>
    /// Deslizadores de volumen por categoría + silenciar + restablecer, enlazados con <see cref="GestorVolumen"/>.
    /// Es el mismo bloque en el menú de sonido del menú principal y en el menú de pausa.
    /// </summary>
    public class ControlesSonido : MonoBehaviour
    {
        [System.Serializable]
        public class Fila
        {
            public CategoriaAudio categoria;
            public Slider deslizador;
        }

        public Fila[] filas;
        public Button botonSilencio;
        public Image iconoSilencio;
        public Sprite spriteVolumen, spriteSilencio;
        public Button botonRestablecer; // opcional

        bool m_Actualizando;

        void Awake()
        {
            if (botonRestablecer) botonRestablecer.onClick.AddListener(GestorVolumen.Restablecer);
            if (botonSilencio) botonSilencio.onClick.AddListener(() => GestorVolumen.Silenciado = !GestorVolumen.Silenciado);
            foreach (var f in filas)
            {
                var fila = f;
                fila.deslizador.minValue = 0; fila.deslizador.maxValue = 1;
                fila.deslizador.onValueChanged.AddListener(v => { if (!m_Actualizando) GestorVolumen.Fijar(fila.categoria, v); });
            }
        }

        void OnEnable() { GestorVolumen.Cambio += Refrescar; Refrescar(); }
        void OnDisable() { GestorVolumen.Cambio -= Refrescar; }

        void Refrescar()
        {
            m_Actualizando = true;
            bool s = GestorVolumen.Silenciado;
            foreach (var f in filas)
            {
                f.deslizador.SetValueWithoutNotify(GestorVolumen.Obtener(f.categoria));
                f.deslizador.interactable = !s;
                var barras = f.deslizador.GetComponent<UI.DeslizadorBarras>();
                if (barras) barras.Pintar();
                var grupo = f.deslizador.GetComponent<CanvasGroup>();
                if (grupo) grupo.alpha = s ? 0.4f : 1f;
            }
            if (iconoSilencio) iconoSilencio.sprite = s ? spriteSilencio : spriteVolumen;
            m_Actualizando = false;
        }
    }
}

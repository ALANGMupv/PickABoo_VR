using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PickABoo.UI
{
    /// <summary>
    /// Dibuja un Slider como una fila de barras verticales (llenas hasta el valor) con un globo que muestra el número.
    /// El Slider sigue siendo el que recibe el arrastre del rayo; este componente solo pinta.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class DeslizadorBarras : MonoBehaviour
    {
        public RectTransform contenedorBarras;
        public Sprite spriteBarra;
        public int numeroBarras = 30;
        [Range(0.1f, 0.9f)] public float grosor = 0.45f;
        public Color colorLleno = Color.white;
        public Color colorVacio = new Color(1, 1, 1, 0.35f);
        [Tooltip("Globo con el valor (opcional)")]
        public TMP_Text textoValor;
        [Tooltip("Si se rellena, el globo muestra este texto en vez del porcentaje")]
        public System.Func<float, string> formato;

        Slider m_Slider;
        Image[] m_Barras;

        void Awake()
        {
            m_Slider = GetComponent<Slider>();
            Construir();
            m_Slider.onValueChanged.AddListener(_ => Pintar());
        }

        void OnEnable() { Pintar(); }

        void Construir()
        {
            if (m_Barras != null) return;
            for (int i = contenedorBarras.childCount - 1; i >= 0; i--) Destroy(contenedorBarras.GetChild(i).gameObject);
            m_Barras = new Image[numeroBarras];
            for (int i = 0; i < numeroBarras; i++)
            {
                var go = new GameObject("Barra" + i, typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(contenedorBarras, false);
                float x0 = (float)i / numeroBarras, x1 = (i + 1f) / numeroBarras;
                float margen = (x1 - x0) * (1 - grosor) * 0.5f;
                rt.anchorMin = new Vector2(x0 + margen, 0); rt.anchorMax = new Vector2(x1 - margen, 1);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var img = go.GetComponent<Image>();
                img.sprite = spriteBarra; img.type = Image.Type.Sliced; img.raycastTarget = false;
                m_Barras[i] = img;
            }
        }

        public void Pintar()
        {
            if (m_Slider == null) m_Slider = GetComponent<Slider>();
            if (m_Barras == null) return;
            float n = m_Slider.normalizedValue;
            for (int i = 0; i < m_Barras.Length; i++)
                m_Barras[i].color = (i + 0.5f) / m_Barras.Length <= n ? colorLleno : colorVacio;
            if (textoValor) textoValor.text = formato != null ? formato(m_Slider.value) : Mathf.RoundToInt(n * 100).ToString();
        }
    }
}

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PickABoo.AjustesSonido
{
    /// <summary>
    /// Menú de sonido dentro de un marco de la casa. Cerrado muestra una placa "Sonido";
    /// al pulsarla se despliega el panel con los deslizadores. "Volver" lo pliega de nuevo.
    /// </summary>
    public class PanelSonido : MonoBehaviour
    {
        [System.Serializable]
        public class Fila
        {
            public CategoriaAudio categoria;
            public Slider deslizador;
            public TMP_Text porcentaje;
        }

        [Header("Estados")]
        public RectTransform placa;          // botón que abre el panel
        public RectTransform panel;          // contenido desplegado
        [Header("Controles")]
        public Fila[] filas;
        public Button botonSilencio;
        public TMP_Text textoSilencio;
        public Image iconoSilencio;
        public Sprite spriteVolumen, spriteSilencio;
        public Button botonRestablecer;
        public Button botonVolver;
        public float duracionAnimacion = 0.35f;

        bool m_Abierto, m_Actualizando;
        Coroutine m_Anim;

        void Awake()
        {
            placa.GetComponent<Button>().onClick.AddListener(Abrir);
            botonVolver.onClick.AddListener(Cerrar);
            botonRestablecer.onClick.AddListener(GestorVolumen.Restablecer);
            botonSilencio.onClick.AddListener(() => GestorVolumen.Silenciado = !GestorVolumen.Silenciado);
            foreach (var f in filas)
            {
                var fila = f;
                fila.deslizador.minValue = 0; fila.deslizador.maxValue = 1;
                fila.deslizador.onValueChanged.AddListener(v => { if (!m_Actualizando) GestorVolumen.Fijar(fila.categoria, v); });
            }
            panel.localScale = new Vector3(1, 0, 1);
            panel.gameObject.SetActive(false);
        }

        void OnEnable() { GestorVolumen.Cambio += Refrescar; Refrescar(); }
        void OnDisable() { GestorVolumen.Cambio -= Refrescar; }

        void Refrescar()
        {
            m_Actualizando = true;
            foreach (var f in filas)
            {
                float v = GestorVolumen.Obtener(f.categoria);
                f.deslizador.SetValueWithoutNotify(v);
                f.porcentaje.text = Mathf.RoundToInt(v * 100) + "%";
                f.deslizador.interactable = !GestorVolumen.Silenciado;
            }
            bool s = GestorVolumen.Silenciado;
            textoSilencio.text = s ? "Activar sonido" : "Silenciar todo";
            iconoSilencio.sprite = s ? spriteSilencio : spriteVolumen;
            m_Actualizando = false;
        }

        public void Abrir()
        {
            if (m_Abierto) return;
            m_Abierto = true;
            Animar(true);
        }

        public void Cerrar()
        {
            if (!m_Abierto) return;
            m_Abierto = false;
            Animar(false);
        }

        void Animar(bool abrir)
        {
            if (m_Anim != null) StopCoroutine(m_Anim);
            m_Anim = StartCoroutine(Desplegar(abrir));
        }

        // El panel se desenrolla de arriba abajo como un pergamino; la placa se encoge a la vez.
        IEnumerator Desplegar(bool abrir)
        {
            panel.gameObject.SetActive(true);
            placa.gameObject.SetActive(true);
            float a0 = panel.localScale.y, a1 = abrir ? 1 : 0;
            for (float t = 0; t < duracionAnimacion; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / duracionAnimacion);
                float y = Mathf.Lerp(a0, a1, k);
                panel.localScale = new Vector3(1, y, 1);
                placa.localScale = Vector3.one * (1 - y);
                yield return null;
            }
            panel.localScale = new Vector3(1, a1, 1);
            placa.localScale = Vector3.one * (1 - a1);
            panel.gameObject.SetActive(abrir);
            placa.gameObject.SetActive(!abrir);
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PickABoo.AjustesSonido
{
    /// <summary>
    /// Menú de sonido del cuadro de la entrada. Cerrado muestra una placa "Sonido";
    /// al pulsarla se despliega el panel (con <see cref="ControlesSonido"/>). "Volver" lo pliega de nuevo.
    /// </summary>
    public class PanelSonido : MonoBehaviour
    {
        [Header("Estados")]
        public RectTransform placa;          // botón que abre el panel
        public RectTransform panel;          // contenido desplegado
        public Button botonVolver;
        public float duracionAnimacion = 0.3f;

        bool m_Abierto;
        Coroutine m_Anim;

        void Awake()
        {
            placa.GetComponent<Button>().onClick.AddListener(Abrir);
            botonVolver.onClick.AddListener(Cerrar);
            panel.localScale = new Vector3(1, 0, 1);
            panel.gameObject.SetActive(false);
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

        // El panel se despliega de arriba abajo; la placa se encoge a la vez.
        IEnumerator Desplegar(bool abrir)
        {
            panel.gameObject.SetActive(true);
            placa.gameObject.SetActive(true);
            float a0 = panel.localScale.y, a1 = abrir ? 1 : 0;
            for (float t = 0; t < duracionAnimacion; t += Time.deltaTime)
            {
                float y = Mathf.Lerp(a0, a1, Mathf.SmoothStep(0, 1, t / duracionAnimacion));
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

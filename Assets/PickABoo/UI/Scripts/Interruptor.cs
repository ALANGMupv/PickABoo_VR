using UnityEngine;
using UnityEngine.UI;

namespace PickABoo.UI
{
    /// <summary>Aspecto de interruptor (sí/no) para un Toggle: la bola se desliza y la pista cambia de color.</summary>
    [RequireComponent(typeof(Toggle))]
    public class Interruptor : MonoBehaviour
    {
        public Image pista;
        public RectTransform bola;
        public float recorrido = 48f;
        Toggle m_Toggle;
        float m_Pos;

        void Awake()
        {
            m_Toggle = GetComponent<Toggle>();
            m_Pos = m_Toggle.isOn ? 1 : 0;
            Aplicar();
        }

        void Update()
        {
            float objetivo = m_Toggle.isOn ? 1 : 0;
            if (Mathf.Approximately(m_Pos, objetivo)) return;
            m_Pos = Mathf.MoveTowards(m_Pos, objetivo, Time.unscaledDeltaTime * 6f);
            Aplicar();
        }

        void Aplicar()
        {
            float k = Mathf.SmoothStep(0, 1, m_Pos);
            bola.anchoredPosition = new Vector2(Mathf.Lerp(-recorrido, recorrido, k) * 0.5f, 0);
            pista.color = Color.Lerp(PaletaPickABoo.Apagado, PaletaPickABoo.Mostaza, k);
        }
    }
}

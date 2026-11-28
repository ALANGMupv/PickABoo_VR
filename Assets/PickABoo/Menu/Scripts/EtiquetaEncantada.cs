using TMPro;
using UnityEngine;

namespace PickABoo.Menu
{
    /// <summary>
    /// Rótulo flotante encima de un objeto del menú que dice para qué sirve. Letras crema con contorno granate
    /// y un halo difuminado del color del aura del objeto que tiembla como una vela.
    /// Mira siempre al jugador, flota despacio y se desvanece mientras el objeto está en la mano.
    /// </summary>
    public class EtiquetaEncantada : MonoBehaviour
    {
        public ObjetoEncantado objeto;
        public TMP_Text texto;
        [Tooltip("Altura sobre el punto de reposo del objeto")] public float altura = 0.26f;
        public float amplitudFlotar = 0.01f;
        public float velocidadFlotar = 0.3f;
        [Tooltip("Brillo del halo (más al apuntar al objeto)")] public float halo = 0.9f;

        Transform m_Cabeza;
        Material m_Mat;
        Vector3 m_Ancla;
        Color m_Color;
        float m_Alfa, m_Fase, m_Realce;

        void Start()
        {
            m_Ancla = (objeto ? objeto.PosicionReposo : transform.position) + Vector3.up * altura;
            m_Fase = Random.value * 10f;
            texto.alpha = 0f;
            // material propio de esta etiqueta: contorno granate fino y halo del color del aura del objeto
            m_Mat = texto.fontMaterial;
            Color aura = objeto ? objeto.colorAura : new Color(1f, 0.65f, 0.25f);
            float m = Mathf.Max(aura.r, aura.g, aura.b, 0.001f);
            Color c = new Color(aura.r / m, aura.g / m, aura.b / m, 1f);
            // el shader móvil de TMP (el de las gafas) no tiene "glow": el halo es la sombra difuminada
            // (underlay) centrada detrás de las letras y teñida del color del aura
            m_Color = c;
            m_Mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            m_Mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
            m_Mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
            m_Mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.6f);
            m_Mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.75f);
            m_Mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.29f, 0f, 0.06f, 1f));
            m_Mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.15f);
            m_Mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.15f);
            texto.UpdateMeshPadding();
        }

        void LateUpdate()
        {
            if (m_Cabeza == null && Camera.main) m_Cabeza = Camera.main.transform;

            bool visible = objeto == null || objeto.EnReposo;
            m_Alfa = Mathf.MoveTowards(m_Alfa, visible ? 1f : 0f, Time.deltaTime * (visible ? 1.2f : 4f));
            texto.alpha = m_Alfa;

            float t = Time.time + m_Fase;
            transform.position = m_Ancla + Vector3.up * (Mathf.Sin(t * velocidadFlotar * Mathf.PI * 2f) * amplitudFlotar);
            // de cara al jugador (también inclinada si mira desde arriba), sin volcarse
            if (m_Cabeza)
            {
                Vector3 d = transform.position - m_Cabeza.position;
                if (d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(d, Vector3.up);
            }

            // halo de vela: tiembla un poco y se aviva al apuntar al objeto
            m_Realce = Mathf.MoveTowards(m_Realce, objeto && objeto.Apuntado ? 1f : 0f, Time.deltaTime * 4f);
            float vela = 0.8f + 0.25f * Mathf.PerlinNoise(t * 2.5f, m_Fase);
            var c = m_Color; c.a = Mathf.Clamp01(halo * vela * (1f + 0.5f * m_Realce) * m_Alfa);
            m_Mat.SetColor(ShaderUtilities.ID_UnderlayColor, c);
        }

        void OnDestroy() { if (m_Mat) Destroy(m_Mat); }
    }
}

using System.Collections;
using UnityEngine;

namespace PickABoo.Transiciones
{
    /// <summary>
    /// Fundido a color en VR: una esfera pequeña alrededor de la cámara que se dibuja por encima de todo.
    /// Se crea sola la primera vez que se usa (Fundido.Instancia).
    /// </summary>
    public class Fundido : MonoBehaviour
    {
        static Fundido s_Instancia;
        Material m_Material;
        Color m_Actual = new Color(0, 0, 0, 0);
        Renderer m_Renderer;

        public static Fundido Instancia
        {
            get
            {
                var cam = Camera.main;
                if (s_Instancia != null && cam != null && s_Instancia.transform.parent == cam.transform) return s_Instancia;
                if (cam == null) return null;
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Fundido";
                Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(cam.transform, false);
                go.transform.localScale = Vector3.one * 0.6f;
                s_Instancia = go.AddComponent<Fundido>();
                return s_Instancia;
            }
        }

        void Awake()
        {
            m_Renderer = GetComponent<Renderer>();
            m_Material = new Material(Resources.Load<Material>("Fundido_Material")); // el material vive en Transiciones/Resources para que entre en la build
            m_Renderer.sharedMaterial = m_Material;
            m_Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_Renderer.receiveShadows = false;
            Aplicar(m_Actual);
        }

        void Aplicar(Color c)
        {
            m_Actual = c;
            m_Material.SetColor("_Color", c);
            m_Renderer.enabled = c.a > 0.001f;
        }

        /// <summary>Pone el color de golpe (por ejemplo, empezar en negro).</summary>
        public void Fijar(Color c) => Aplicar(c);

        /// <summary>Lleva el fundido hasta el color y la opacidad indicados.</summary>
        public IEnumerator A(Color destino, float duracion)
        {
            var inicio = m_Actual;
            if (Mathf.Approximately(inicio.a, 0)) inicio = new Color(destino.r, destino.g, destino.b, 0);
            for (float t = 0; t < duracion; t += Time.unscaledDeltaTime)
            {
                Aplicar(Color.Lerp(inicio, destino, Mathf.SmoothStep(0, 1, t / duracion)));
                yield return null;
            }
            Aplicar(destino);
        }

        public IEnumerator Aparecer(float duracion) => A(new Color(m_Actual.r, m_Actual.g, m_Actual.b, 0), duracion);
    }
}

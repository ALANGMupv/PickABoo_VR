using System.Collections;
using System.Collections.Generic;
using PickABoo.Sonido;
using PickABoo.Transiciones;
using PickABoo.UI;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace PickABoo.Salida
{
    /// <summary>
    /// Puerta de la entrada por la que se sale del juego.
    /// Brilla suavemente para que se note que se puede usar; al apuntarla brilla más, vibra el mando y aparece
    /// el cartel "Salir del juego". Con el gatillo o el agarre pregunta si quieres salir; al confirmar se bajan
    /// las manillas, la puerta se ilumina y la vista funde a negro antes de cerrar el juego.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class PuertaSalida : MonoBehaviour
    {
        public Renderer[] puertas;
        public Transform[] manillas;
        [Tooltip("Material de brillo encantado (shader PickABoo/Encantado)")]
        public Material materialAura;
        [ColorUsage(true, true)] public Color colorAura = new Color(1.4f, 1.1f, 0.6f);
        [Range(0, 1)] public float brilloReposo = 0.35f;
        public CanvasGroup cartel;
        public DialogoConfirmacion dialogo;
        public float distanciaDialogo = 0.8f;
        [Tooltip("Ángulo que bajan las manillas al abrir")]
        public float anguloManilla = 40f;

        XRSimpleInteractable m_Interactuable;
        readonly List<Material> m_Auras = new List<Material>();
        float m_Brillo, m_Cartel;
        bool m_Saliendo;
        Quaternion[] m_RotManillas;

        void Awake()
        {
            m_Interactuable = GetComponent<XRSimpleInteractable>();
            m_Interactuable.hoverEntered.AddListener(a => Menu.ObjetoEncantado.Vibrar(a.interactorObject, 0.25f, 0.06f));
            m_Interactuable.selectEntered.AddListener(_ => Preguntar());

            if (materialAura != null)
            {
                foreach (var p in puertas)
                {
                    var mf = p.GetComponent<MeshFilter>();
                    if (mf == null) continue;
                    var capa = new GameObject("Aura_" + p.name);
                    capa.transform.SetParent(p.transform, false);
                    capa.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                    var r = capa.AddComponent<MeshRenderer>();
                    var m = new Material(materialAura);
                    m.SetColor("_Color", colorAura);
                    r.sharedMaterial = m;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                    m_Auras.Add(m);
                }
            }
            m_RotManillas = new Quaternion[manillas.Length];
            for (int i = 0; i < manillas.Length; i++) m_RotManillas[i] = manillas[i].localRotation;
            if (cartel) cartel.alpha = 0;
        }

        void Update()
        {
            bool apuntada = m_Interactuable.isHovered;
            // el gatillo también sirve (además del agarre) mientras se apunta a la puerta
            if (apuntada && !m_Saliendo && (dialogo == null || !dialogo.Abierto))
                foreach (var i in m_Interactuable.interactorsHovering)
                    if (i is XRBaseInputInteractor e && e.activateInput.ReadWasPerformedThisFrame()) { Preguntar(); break; }

            // con la ventana abierta el brillo se apaga para que no se vea a través de ella
            bool ventana = dialogo != null && dialogo.Abierto;
            float objetivo = m_Saliendo ? 1.6f : (ventana ? 0f : (apuntada ? 1f : brilloReposo));
            m_Brillo = Mathf.MoveTowards(m_Brillo, objetivo, Time.deltaTime * 2.5f);
            foreach (var m in m_Auras) { m.SetFloat("_Realce", m_Brillo); m.SetFloat("_Visibilidad", Mathf.Clamp01(m_Brillo * 1.5f)); }

            if (cartel)
            {
                m_Cartel = Mathf.MoveTowards(m_Cartel, apuntada || (dialogo && dialogo.Abierto) ? 1 : 0, Time.deltaTime * 4f);
                cartel.alpha = m_Cartel;
            }
        }

        public void Preguntar()
        {
            if (m_Saliendo || dialogo == null || dialogo.Abierto) return;
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 frente = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                // si estás muy cerca de la puerta, la ventana se queda entre tú y la puerta
                float d = distanciaDialogo;
                var plano = new Plane(-transform.right, transform.position);
                if (plano.Raycast(new Ray(cam.transform.position, frente), out float hasta)) d = Mathf.Clamp(hasta - 0.3f, 0.35f, distanciaDialogo);
                Vector3 pos = cam.transform.position + frente * d - Vector3.up * 0.1f;
                dialogo.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cam.transform.position, Vector3.up));
            }
            dialogo.Preguntar("¿Salir del juego?", "Pick a Boo se cerrará.", "Salir", () => StartCoroutine(Salir()));
        }

        IEnumerator Salir()
        {
            m_Saliendo = true;
            AudioDemo.AtenuarParaSalir();
            Vector3 origen = manillas.Length > 0 ? manillas[0].position : transform.position;
            AudioDemo.Efecto("sonidoPestillo", origen, 0.60f);
            // las manillas bajan
            for (float t = 0; t < 0.3f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / 0.3f);
                for (int i = 0; i < manillas.Length; i++) manillas[i].localRotation = m_RotManillas[i] * Quaternion.Euler(0, 0, -anguloManilla * k);
                yield return null;
            }
            yield return new WaitForSeconds(0.25f);
            var f = Fundido.Instancia;
            if (f) yield return f.A(Color.black, 1.2f);
            yield return new WaitForSeconds(0.2f);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

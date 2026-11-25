using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace PickABoo.Jugador
{
    /// <summary>
    /// Muestra una mano en lugar del modelo del mando. Los dedos se cierran según lo que aprietas
    /// (índice con el gatillo, el resto con el agarre) y, al sostener algo, se cierran solo hasta tocar
    /// su superficie, sin atravesarlo. Los objetos con <see cref="AgarreMano"/> se colocan apoyados en la palma.
    /// Va en el GameObject del mando (Left/Right Controller).
    /// </summary>
    public class ManoMando : MonoBehaviour
    {
        public bool derecha = true;
        [Tooltip("Modelo de mano con huesos (LeftHand/RightHand.fbx)")]
        public GameObject modeloMano;
        public Material material;
        [Tooltip("Modelo del mando que se oculta")]
        public GameObject visualMando;
        public NearFarInteractor interactor;
        [Header("Colocación respecto al mando")]
        public Vector3 posicion = new Vector3(0.01f, -0.025f, -0.035f);
        public Vector3 rotacion = new Vector3(15f, 0f, -80f);
        [Header("Dedos")]
        [Range(0, 1)] public float reposo = 0.3f;
        [Range(0, 1)] public float sujetando = 1f;
        public float velocidad = 14f;
        [Tooltip("Distancia mínima entre los dedos y el objeto sostenido")]
        public float holguraDedos = 0.006f;
        [Tooltip("Grosor de la piel desde el hueso de la palma hasta su superficie")]
        public float pielPalma = 0.016f;
        [Tooltip("Tiempo que tarda un objeto en llegar a la palma")]
        public float tiempoLlegada = 0.12f;

        InputAction m_Gatillo, m_Agarre;
        Transform[][] m_Dedos;       // [dedo][articulación]
        Transform[][] m_Puntos;      // puntos de cada dedo que no pueden entrar en el objeto
        Quaternion[][] m_Iniciales;
        readonly float[] m_Cierre = new float[5];
        GameObject m_Mano;
        Transform m_Palma;

        // objeto sostenido
        XRGrabInteractable m_Objeto;
        Transform m_Contacto;
        bool m_Colocando, m_Fijo;
        float m_Llegada;
        Vector3 m_PosInicio; Quaternion m_RotInicio;
        readonly List<Collider> m_Colisiones = new List<Collider>();
        readonly Dictionary<Collider, bool> m_EraTrigger = new Dictionary<Collider, bool>();

        static readonly float[] k_Angulos = { 70f, 85f, 45f };
        static readonly float[] k_AngulosPulgar = { 20f, 35f, 40f };
        static readonly string[] k_Nombres = { "Thumb", "Index", "Middle", "Ring", "Little" };

        public bool Sostiene(Object o) => m_Objeto != null && (m_Objeto == o || m_Objeto.gameObject == o);

        void Awake()
        {
            string mano = derecha ? "RightHand" : "LeftHand";
            m_Gatillo = new InputAction("Gatillo", InputActionType.Value, "<XRController>{" + mano + "}/{Trigger}");
            m_Agarre = new InputAction("Agarre", InputActionType.Value, "<XRController>{" + mano + "}/{Grip}");

            m_Mano = Instantiate(modeloMano, transform, false);
            m_Mano.name = "Mano";
            m_Mano.transform.localPosition = posicion;
            m_Mano.transform.localRotation = Quaternion.Euler(derecha ? rotacion : new Vector3(rotacion.x, -rotacion.y, -rotacion.z));
            foreach (var r in m_Mano.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (material) r.sharedMaterial = material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.updateWhenOffscreen = true;
            }
            if (visualMando) visualMando.SetActive(false);

            string p = derecha ? "R_" : "L_";
            m_Palma = Buscar(m_Mano.transform, p + "Palm");
            m_Dedos = new Transform[5][]; m_Iniciales = new Quaternion[5][]; m_Puntos = new Transform[5][];
            for (int d = 0; d < 5; d++)
            {
                string[] art = d == 0 ? new[] { "Metacarpal", "Proximal", "Distal" } : new[] { "Proximal", "Intermediate", "Distal" };
                m_Dedos[d] = new Transform[3]; m_Iniciales[d] = new Quaternion[3];
                for (int a = 0; a < 3; a++)
                {
                    var t = Buscar(m_Mano.transform, p + k_Nombres[d] + art[a]);
                    m_Dedos[d][a] = t;
                    m_Iniciales[d][a] = t ? t.localRotation : Quaternion.identity;
                }
                var punta = Buscar(m_Mano.transform, p + k_Nombres[d] + "Tip");
                m_Puntos[d] = new[] { m_Dedos[d][1], m_Dedos[d][2], punta };
            }

            if (interactor)
            {
                interactor.selectEntered.AddListener(AlCoger);
                interactor.selectExited.AddListener(AlSoltar);
            }
        }

        static Transform Buscar(Transform t, string nombre)
        {
            if (t.name == nombre) return t;
            foreach (Transform c in t) { var r = Buscar(c, nombre); if (r) return r; }
            return null;
        }

        void OnEnable() { m_Gatillo.Enable(); m_Agarre.Enable(); if (m_Mano) m_Mano.SetActive(true); }
        void OnDisable() { m_Gatillo.Disable(); m_Agarre.Disable(); }
        void OnDestroy() { m_Gatillo.Dispose(); m_Agarre.Dispose(); }

        // ---------- sostener ----------

        void AlCoger(SelectEnterEventArgs a)
        {
            if (m_Fijo) return;
            var g = a.interactableObject as XRGrabInteractable;
            if (g == null) return;
            m_Objeto = g;
            ActualizarColisiones();
            var agarre = g.GetComponent<AgarreMano>();
            m_Contacto = agarre ? agarre.Contacto(derecha) : null;
            // si nadie más controla su posición (la cámara, por ejemplo, la mueve ella sola), la coloca la mano
            m_Colocando = m_Contacto != null && g.trackPosition && g.trackRotation;
            if (m_Colocando)
            {
                g.trackPosition = g.trackRotation = false;
                m_PosInicio = g.transform.position; m_RotInicio = g.transform.rotation; m_Llegada = 0f;
            }
        }

        void AlSoltar(SelectExitEventArgs a)
        {
            if (m_Fijo || m_Objeto == null || (Object)a.interactableObject != m_Objeto) return;
            if (m_Colocando) m_Objeto.trackPosition = m_Objeto.trackRotation = true;
            m_Objeto = null; m_Contacto = null; m_Colocando = false;
            m_Colisiones.Clear();
        }

        /// <summary>
        /// Deja el objeto que sostiene la mano pegado a ella para siempre (no se puede soltar ni lo puede coger la otra mano).
        /// Sus colisiones pasan a ser triggers para no empujar al jugador.
        /// </summary>
        public void Fijar()
        {
            if (m_Objeto == null) return;
            m_Fijo = true;
            foreach (var c in m_Colisiones) { if (c && !m_EraTrigger.ContainsKey(c)) m_EraTrigger[c] = c.isTrigger; if (c) c.isTrigger = true; }
            var rb = m_Objeto.GetComponent<Rigidbody>();
            if (rb) { rb.isKinematic = true; rb.useGravity = false; }
            if (!m_Colocando && m_Contacto == null)
            {
                // sin contacto definido: se queda donde está respecto a la mano
                var c = new GameObject("ContactoFijo").transform;
                c.SetParent(m_Objeto.transform, false);
                c.SetPositionAndRotation(MarcoPalma(out var r), r);
                m_Contacto = c;
            }
            m_Colocando = true;
            m_Llegada = 1f;
            m_Objeto.enabled = false; // nadie más puede cogerlo; la mano lo sigue colocando
        }

        // Todas las colisiones activas del objeto (incluidas las que aparecen al abrir el libro)
        void ActualizarColisiones()
        {
            m_Colisiones.Clear();
            if (m_Objeto == null) return;
            m_Objeto.GetComponentsInChildren(false, m_Colisiones);
            m_Colisiones.RemoveAll(c => !c.enabled);
        }

        // Marco de la palma: Z hacia los dedos, Y desde la palma hacia fuera (hacia lo que se sostiene)
        Vector3 MarcoPalma(out Quaternion rot)
        {
            Vector3 normal = -m_Palma.up;
            rot = Quaternion.LookRotation(m_Palma.forward, normal);
            return m_Palma.position + normal * pielPalma;
        }

        void ColocarObjeto()
        {
            if (!m_Colocando || m_Objeto == null || m_Contacto == null) return;
            var t = m_Objeto.transform;
            Vector3 pos = MarcoPalma(out var rot);
            // pose del objeto para que su contacto coincida con la palma
            Quaternion rObj = rot * Quaternion.Inverse(Quaternion.Inverse(t.rotation) * m_Contacto.rotation);
            Vector3 desplazamiento = rObj * Quaternion.Inverse(t.rotation) * (m_Contacto.position - t.position);
            Vector3 pObj = pos - desplazamiento;
            m_Llegada = Mathf.Min(1f, m_Llegada + Time.deltaTime / Mathf.Max(0.01f, tiempoLlegada));
            float k = Mathf.SmoothStep(0, 1, m_Llegada);
            t.SetPositionAndRotation(Vector3.Lerp(m_PosInicio, pObj, k), Quaternion.Slerp(m_RotInicio, rObj, k));
            if (m_Llegada >= 1f) { m_PosInicio = t.position; m_RotInicio = t.rotation; }
        }

        // ---------- dedos ----------

        void AplicarDedo(int d, float cierre)
        {
            var angulos = d == 0 ? k_AngulosPulgar : k_Angulos;
            for (int a = 0; a < 3; a++)
            {
                var t = m_Dedos[d][a];
                if (t) t.localRotation = m_Iniciales[d][a] * Quaternion.Euler(angulos[a] * cierre, 0, 0);
            }
        }

        bool Toca(int d)
        {
            foreach (var p in m_Puntos[d])
            {
                if (p == null) continue;
                foreach (var c in m_Colisiones)
                {
                    if (c == null) continue;
                    Vector3 cerca = c.ClosestPoint(p.position);
                    if ((cerca - p.position).sqrMagnitude < holguraDedos * holguraDedos) return true;
                }
            }
            return false;
        }

        // Cierra el dedo hasta "objetivo" pero se detiene en cuanto tocaría el objeto sostenido.
        float CierreSinAtravesar(int d, float objetivo)
        {
            if (m_Colisiones.Count == 0) return objetivo;
            AplicarDedo(d, objetivo);
            if (!Toca(d)) return objetivo;
            float lo = 0f, hi = objetivo;
            AplicarDedo(d, 0f);
            if (Toca(d)) return 0f;
            for (int i = 0; i < 6; i++)
            {
                float mid = (lo + hi) * 0.5f;
                AplicarDedo(d, mid);
                if (Toca(d)) hi = mid; else lo = mid;
            }
            return lo;
        }

        void LateUpdate()
        {
            ColocarObjeto();
            if (m_Objeto != null) ActualizarColisiones();

            float gatillo = m_Gatillo.ReadValue<float>();
            float agarre = m_Agarre.ReadValue<float>();
            bool sujeta = m_Objeto != null;

            float resto = Mathf.Max(reposo, agarre, sujeta ? sujetando : 0f);
            float indice = Mathf.Max(reposo * 0.6f, gatillo, sujeta ? sujetando : 0f);
            float pulgar = Mathf.Max(reposo, Mathf.Max(agarre, gatillo) * 0.7f, sujeta ? sujetando : 0f);
            float[] objetivo = { pulgar, indice, resto, resto, resto };

            float k = 1f - Mathf.Exp(-velocidad * Time.deltaTime);
            for (int d = 0; d < 5; d++)
            {
                float meta = sujeta ? CierreSinAtravesar(d, objetivo[d]) : objetivo[d];
                m_Cierre[d] = Mathf.Lerp(m_Cierre[d], meta, k);
                // nunca más cerrado de lo permitido (evita atravesar mientras se suaviza)
                if (sujeta) m_Cierre[d] = Mathf.Min(m_Cierre[d], meta + 0.001f);
                AplicarDedo(d, m_Cierre[d]);
            }
        }
    }
}

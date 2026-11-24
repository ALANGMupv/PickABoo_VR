using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace PickABoo.Jugador
{
    /// <summary>
    /// Muestra una mano en lugar del modelo del mando. Los dedos se cierran según lo que aprietas:
    /// el índice con el gatillo y el resto con el botón lateral (agarre). Al sostener un objeto, la mano lo rodea.
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
        [Range(0, 1)] public float sujetando = 0.85f;
        public float velocidad = 14f;

        InputAction m_Gatillo, m_Agarre;
        Transform[][] m_Dedos;       // [dedo][articulación]
        Quaternion[][] m_Iniciales;
        float[] m_Cierre = new float[5];
        GameObject m_Mano;

        // ángulos máximos de cierre por articulación (proximal, intermedia, distal)
        static readonly float[] k_Angulos = { 70f, 85f, 45f };
        static readonly float[] k_AngulosPulgar = { 20f, 35f, 40f };
        static readonly string[] k_Nombres = { "Thumb", "Index", "Middle", "Ring", "Little" };

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
            m_Dedos = new Transform[5][]; m_Iniciales = new Quaternion[5][];
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

        void LateUpdate()
        {
            float gatillo = m_Gatillo.ReadValue<float>();
            float agarre = m_Agarre.ReadValue<float>();
            bool sujeta = interactor != null && interactor.hasSelection;

            float resto = Mathf.Max(reposo, agarre, sujeta ? sujetando : 0f);
            float indice = Mathf.Max(reposo * 0.6f, gatillo, sujeta ? sujetando * 0.7f : 0f);
            float pulgar = Mathf.Max(reposo, Mathf.Max(agarre, gatillo) * 0.7f, sujeta ? 0.7f : 0f);
            float[] objetivo = { pulgar, indice, resto, resto, resto };

            float k = 1f - Mathf.Exp(-velocidad * Time.deltaTime);
            for (int d = 0; d < 5; d++)
            {
                m_Cierre[d] = Mathf.Lerp(m_Cierre[d], objetivo[d], k);
                var angulos = d == 0 ? k_AngulosPulgar : k_Angulos;
                for (int a = 0; a < 3; a++)
                {
                    var t = m_Dedos[d][a];
                    if (t) t.localRotation = m_Iniciales[d][a] * Quaternion.Euler(angulos[a] * m_Cierre[d], 0, 0);
                }
            }
        }
    }
}

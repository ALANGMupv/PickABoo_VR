using System.Collections;
using System.Collections.Generic;
using PickABoo.Sonido;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PickABoo.Menu
{
    /// <summary>
    /// Efecto "encantado" de los objetos del menú: borde de luz pulsante (shader PickABoo/Encantado),
    /// chispas, flotar suave y vuelta a su sitio al soltarlo. Al apuntarlo se intensifica y vibra el mando.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ObjetoEncantado : MonoBehaviour
    {
        [Header("Aspecto")]
        public Material materialAura;                 // material con el shader PickABoo/Encantado (se instancia)
        [ColorUsage(true, true)] public Color colorAura = new Color(1f, 0.65f, 0.25f) * 1.5f;
        public ParticleSystem chispas;
        [Header("Movimiento en reposo")]
        public float amplitudFlotar = 0.012f;
        public float velocidadFlotar = 0.6f;
        public float giroPorSegundo = 0f;
        [Header("Al soltarlo")]
        public bool volverAlSoltar = true;
        public float duracionVuelta = 0.7f;

        XRGrabInteractable m_Agarre;
        Material m_Aura;
        Vector3 m_PosReposo;
        Quaternion m_RotReposo;
        float m_Fase, m_Realce, m_RealceObjetivo, m_Visibilidad = 1f;
        bool m_Cogido, m_Volviendo, m_Apagado;
        readonly List<GameObject> m_Capas = new List<GameObject>();

        public XRGrabInteractable Agarre => m_Agarre;
        public bool Cogido => m_Cogido;
        /// <summary>Quieto en su sitio y aún encantado (ni en la mano, ni volviendo, ni apagado).</summary>
        public bool EnReposo => !m_Cogido && !m_Volviendo && !m_Apagado;
        public bool Apuntado => m_RealceObjetivo > 0f;
        public Vector3 PosicionReposo => m_PosReposo;

        void Awake()
        {
            m_Agarre = GetComponent<XRGrabInteractable>();
            m_PosReposo = transform.position;
            m_RotReposo = transform.rotation;
            m_Fase = Random.value * 10f;
            CrearCapasAura();
            m_Agarre.hoverEntered.AddListener(a => { m_RealceObjetivo = 1.2f; Vibrar(a.interactorObject, 0.15f, 0.05f); });
            m_Agarre.hoverExited.AddListener(_ => m_RealceObjetivo = 0f);
            m_Agarre.selectEntered.AddListener(a =>
            {
                m_Cogido = true;
                m_Volviendo = false;
                StopAllCoroutines();
                Vibrar(a.interactorObject, 0.35f, 0.08f);
                if (!GetComponent<LibroInstrucciones>())
                    AudioDemo.Efecto("sonidoRecogerObjeto", transform.position, 0.42f, seguir: transform);
            });
            m_Agarre.selectExited.AddListener(_ => { m_Cogido = false; if (volverAlSoltar && !m_Apagado) StartCoroutine(VolverASuSitio()); });
        }

        // Duplica las mallas del objeto con el material del aura (una capa por renderer).
        void CrearCapasAura()
        {
            if (materialAura == null) return;
            m_Aura = new Material(materialAura);
            m_Aura.SetColor("_Color", colorAura);
            foreach (var mf in GetComponentsInChildren<MeshFilter>(true))
            {
                var r = mf.GetComponent<MeshRenderer>();
                if (r == null || mf.sharedMesh == null || mf.GetComponentInParent<Canvas>() != null || mf.name.StartsWith("Aura")) continue;
                var capa = new GameObject("Aura_" + mf.name);
                capa.transform.SetParent(mf.transform, false);
                capa.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var cr = capa.AddComponent<MeshRenderer>();
                var mats = new Material[mf.sharedMesh.subMeshCount];
                for (int i = 0; i < mats.Length; i++) mats[i] = m_Aura;
                cr.sharedMaterials = mats;
                cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                cr.receiveShadows = false;
                cr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                cr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                m_Capas.Add(capa);
            }
        }

        public void MostrarAura(bool visible)
        {
            foreach (var c in m_Capas) c.SetActive(visible);
        }

        void Update()
        {
            m_Realce = Mathf.MoveTowards(m_Realce, m_RealceObjetivo + (m_Cogido ? 0.6f : 0f), Time.deltaTime * 4f);
            if (m_Aura != null)
            {
                m_Aura.SetFloat("_Realce", m_Realce);
                m_Aura.SetFloat("_Visibilidad", m_Visibilidad);
            }
            if (!m_Cogido && !m_Volviendo && !m_Apagado)
            {
                float y = Mathf.Sin((Time.time + m_Fase) * velocidadFlotar * Mathf.PI * 2f) * amplitudFlotar;
                transform.position = m_PosReposo + Vector3.up * y;
                if (giroPorSegundo != 0f) transform.rotation = Quaternion.Euler(0, (Time.time + m_Fase) * giroPorSegundo, 0) * m_RotReposo;
            }
        }

        IEnumerator VolverASuSitio()
        {
            m_Volviendo = true;
            Vector3 p0 = transform.position; Quaternion r0 = transform.rotation;
            for (float t = 0; t < duracionVuelta; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / duracionVuelta);
                transform.SetPositionAndRotation(Vector3.Lerp(p0, m_PosReposo, k), Quaternion.Slerp(r0, m_RotReposo, k));
                yield return null;
            }
            transform.SetPositionAndRotation(m_PosReposo, m_RotReposo);
            m_Volviendo = false;
            AudioDemo.Efecto("sonidoApoyarObjeto", transform.position, 0.44f, seguir: transform);
        }

        /// <summary>Apaga el encantamiento (al empezar la partida): el brillo y las chispas se desvanecen.</summary>
        public void Apagar(float duracion = 0.8f, bool desactivarAgarre = true)
        {
            m_Apagado = true;
            if (desactivarAgarre && !m_Cogido) m_Agarre.enabled = false;
            if (chispas != null) chispas.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            StartCoroutine(Desvanecer(duracion));
        }

        IEnumerator Desvanecer(float d)
        {
            for (float t = 0; t < d; t += Time.deltaTime) { m_Visibilidad = 1f - t / d; yield return null; }
            m_Visibilidad = 0f;
        }

        public static void Vibrar(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor, float amplitud, float duracion)
        {
            if (interactor == null) return;
            var h = interactor.transform.GetComponentInParent<HapticImpulsePlayer>();
            if (h != null) h.SendHapticImpulse(amplitud, duracion);
        }
    }
}


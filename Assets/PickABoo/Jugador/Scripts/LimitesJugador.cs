using PickABoo.Transiciones;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace PickABoo.Jugador
{
    /// <summary>
    /// Impide meter la cabeza en las paredes y rescata al jugador si cae fuera de la casa.
    /// - Cabeza: el seguimiento de la posición sigue activo, pero se anula solo la parte del movimiento
    ///   físico que atravesaría una pared (se desplaza el XR Origin en sentido contrario). Si se insiste, la vista se oscurece.
    /// - Caída: si el jugador baja de <see cref="alturaRescate"/>, vuelve con un fundido al último punto en el que pisaba suelo.
    /// Va en el XR Origin.
    /// </summary>
    [RequireComponent(typeof(XROrigin))]
    public class LimitesJugador : MonoBehaviour
    {
        [Header("Cabeza")]
        [Tooltip("Distancia mínima entre la cabeza y una pared")]
        public float radioCabeza = 0.15f;
        [Tooltip("Capas que cuentan como pared")]
        public LayerMask paredes = 1; // Default
        [Range(0, 1)] public float oscurecerMaximo = 0.75f;

        [Header("Caída")]
        public float alturaRescate = -3f;

        XROrigin m_Origen;
        CharacterController m_Cuerpo;
        Vector3 m_CabezaLocalAnterior;
        bool m_HayAnterior;
        float m_Oscuro, m_Empuje;
        Vector3 m_UltimoSeguro;
        float m_SiguienteGuardado;
        bool m_Rescatando;
        readonly Collider[] m_Cercanos = new Collider[16];

        void Awake()
        {
            m_Origen = GetComponent<XROrigin>();
            m_Cuerpo = GetComponent<CharacterController>();
            m_UltimoSeguro = transform.position;
        }

        void OnDisable() { m_HayAnterior = false; if (m_Oscuro > 0) { m_Oscuro = 0; Fundido.Instancia?.Fijar(new Color(0, 0, 0, 0)); } }

        void LateUpdate()
        {
            if (m_Origen.Camera == null) return;
            LimitarCabeza();
            VigilarCaida();
        }

        bool EsPared(Collider c) =>
            c != null && !c.isTrigger && c.attachedRigidbody == null && !c.transform.IsChildOf(transform);

        void LimitarCabeza()
        {
            var cam = m_Origen.Camera.transform;
            Vector3 cabeza = cam.position;
            Vector3 correccion = Vector3.zero;

            // 1) Movimiento físico de la cabeza desde el frame anterior (sin contar teletransportes ni joystick):
            //    si cruza una pared, se devuelve la cabeza a este lado.
            if (m_HayAnterior)
            {
                Vector3 desde = transform.TransformPoint(m_CabezaLocalAnterior);
                Vector3 paso = cabeza - desde; paso.y = 0;
                float largo = paso.magnitude;
                if (largo > 0.001f && Physics.Raycast(desde, paso / largo, out var golpe, largo + radioCabeza, paredes, QueryTriggerInteraction.Ignore) && EsPared(golpe.collider))
                {
                    Vector3 limite = golpe.point - paso / largo * radioCabeza;
                    Vector3 c = limite - cabeza; c.y = 0;
                    correccion += c;
                }
            }

            // 2) Mantener la cabeza a una distancia mínima de las paredes cercanas.
            Vector3 cab = cabeza + correccion;
            int n = Physics.OverlapSphereNonAlloc(cab, radioCabeza, m_Cercanos, paredes, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var col = m_Cercanos[i];
                if (!EsPared(col) || (col is MeshCollider mc && !mc.convex)) continue;
                Vector3 cerca = col.ClosestPoint(cab);
                Vector3 fuera = cab - cerca; fuera.y = 0;
                float d = fuera.magnitude;
                if (d < 0.0001f) continue; // ya dentro: lo resuelve el paso 1 en el siguiente frame
                if (d < radioCabeza) { Vector3 c = fuera / d * (radioCabeza - d); correccion += c; cab += c; }
            }

            if (correccion.sqrMagnitude > 1e-8f) transform.position += correccion;
            m_CabezaLocalAnterior = transform.InverseTransformPoint(cam.position);
            m_HayAnterior = true;

            // oscurecer mientras se empuja contra la pared
            m_Empuje = Mathf.Max(m_Empuje - Time.deltaTime * 0.5f, 0) + correccion.magnitude;
            float objetivo = Mathf.Clamp01(m_Empuje / 0.15f) * oscurecerMaximo;
            if (m_Rescatando) return;
            float nuevo = Mathf.MoveTowards(m_Oscuro, objetivo, Time.deltaTime * 2f);
            if (nuevo > 0 || m_Oscuro > 0)
            {
                m_Oscuro = nuevo;
                Fundido.Instancia?.Fijar(new Color(0, 0, 0, m_Oscuro));
            }
        }

        void VigilarCaida()
        {
            if (m_Rescatando) return;
            if (transform.position.y < alturaRescate) { StartCoroutine(Rescatar()); return; }
            // punto seguro: hay suelo justo debajo de los pies (no se usa isGrounded: el proveedor de gravedad no siempre mueve el cuerpo)
            if (Time.time > m_SiguienteGuardado &&
                Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.45f, paredes, QueryTriggerInteraction.Ignore))
            {
                m_UltimoSeguro = transform.position;
                m_SiguienteGuardado = Time.time + 0.5f;
            }
        }

        System.Collections.IEnumerator Rescatar()
        {
            m_Rescatando = true;
            var f = Fundido.Instancia;
            if (f) yield return f.A(Color.black, 0.15f);
            if (m_Cuerpo) m_Cuerpo.enabled = false;
            transform.position = m_UltimoSeguro + Vector3.up * 0.05f;
            m_HayAnterior = false;
            if (m_Cuerpo) m_Cuerpo.enabled = true;
            if (f) yield return f.Aparecer(0.4f);
            m_Oscuro = 0;
            m_Rescatando = false;
        }
    }
}

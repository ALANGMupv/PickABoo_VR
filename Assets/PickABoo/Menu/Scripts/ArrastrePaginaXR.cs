using UnityEngine;
using UnityEngine.EventSystems;

namespace PickABoo.Menu
{
    /// <summary>
    /// Permite pasar las hojas del libro (Book-Page Curl) arrastrándolas con el rayo del mando.
    /// Book.cs solo sabe leer el ratón; aquí se le da el punto del rayo sobre la página.
    /// Arrastrar desde la mitad derecha pasa hacia delante; desde la izquierda, hacia atrás.
    /// </summary>
    [RequireComponent(typeof(Book))]
    public class ArrastrePaginaXR : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("El BookPanel del libro (el rectángulo de las dos páginas)")]
        public RectTransform panelLibro;
        [Tooltip("Suavizado del seguimiento de la página")]
        public float suavizado = 14f;

        Book m_Libro;
        RectTransform m_Panel;
        Vector3 m_Objetivo, m_Actual;
        bool m_Arrastrando, m_HaciaDelante;

        void Awake()
        {
            m_Libro = GetComponent<Book>();
            m_Panel = panelLibro != null ? panelLibro : (RectTransform)transform;
        }

        bool PuntoLocal(PointerEventData e, out Vector3 local)
        {
            local = default;
            var r = e.pointerCurrentRaycast;
            if (!r.isValid) return false;
            Vector3 mundo = r.worldPosition;
            // por si el raycaster no rellena la posición en el mundo
            if (mundo == Vector3.zero && r.module is UnityEngine.UI.GraphicRaycaster && e.enterEventCamera)
                RectTransformUtility.ScreenPointToWorldPointInRectangle(m_Panel, r.screenPosition, e.enterEventCamera, out mundo);
            local = m_Panel.InverseTransformPoint(mundo);
            local.z = 0;
            return true;
        }

        public void OnInitializePotentialDrag(PointerEventData e) { e.useDragThreshold = true; }

        public void OnBeginDrag(PointerEventData e)
        {
            if (!PuntoLocal(e, out var p)) return;
            m_HaciaDelante = p.x > 0;
            if (m_HaciaDelante && m_Libro.currentPage >= m_Libro.bookPages.Length) return;
            if (!m_HaciaDelante && m_Libro.currentPage <= 0) return;
            m_Actual = m_Objetivo = p;
            if (m_HaciaDelante) m_Libro.DragRightPageToPoint(p); else m_Libro.DragLeftPageToPoint(p);
            m_Arrastrando = true;
        }

        public void OnDrag(PointerEventData e)
        {
            if (m_Arrastrando && PuntoLocal(e, out var p)) m_Objetivo = p;
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!m_Arrastrando) return;
            m_Arrastrando = false;
            m_Libro.ReleasePage();
        }

        void Update()
        {
            if (!m_Arrastrando) return;
            m_Actual = Vector3.Lerp(m_Actual, m_Objetivo, Time.deltaTime * suavizado);
            if (m_HaciaDelante) m_Libro.UpdateBookRTLToPoint(m_Actual); else m_Libro.UpdateBookLTRToPoint(m_Actual);
        }

        void OnDisable()
        {
            if (m_Arrastrando) { m_Arrastrando = false; m_Libro.ReleasePage(); }
        }
    }
}

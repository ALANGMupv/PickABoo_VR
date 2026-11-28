using System.Collections;
using PickABoo.Sonido;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PickABoo.Menu
{
    /// <summary>
    /// Libro de instrucciones: se abre al cogerlo, pasa páginas con el gatillo o con las flechas,
    /// y se cierra al soltarlo (ObjetoEncantado lo devuelve a la mesa).
    /// Las páginas usan el asset Book-Page Curl (Book + AutoFlip), el mismo del juego AR.
    /// </summary>
    [RequireComponent(typeof(ObjetoEncantado))]
    public class LibroInstrucciones : MonoBehaviour
    {
        public Transform bisagraTapa;          // pivote de la tapa delantera (en el lomo)
        public float anguloAbierto = -178f;
        public CanvasGroup paginas;            // Canvas con el Book
        public AutoFlip pasaPaginas;
        public Button botonAnterior, botonSiguiente;
        [Tooltip("Colisión de la mitad que aparece al abrir el libro (para que la mano no la atraviese)")]
        public Collider colisionAbierto;
        public float duracionApertura = 0.45f;

        XRGrabInteractable m_Agarre;
        Coroutine m_Anim;
        float m_Abierto;   // 0 cerrado, 1 abierto
        ObjetoEncantado m_Encantado;

        void Awake()
        {
            m_Agarre = GetComponent<XRGrabInteractable>();
            m_Agarre.selectEntered.AddListener(_ => Animar(true));
            m_Agarre.selectExited.AddListener(_ => Animar(false));
            m_Agarre.activated.AddListener(_ => Siguiente());
            botonSiguiente.onClick.AddListener(Siguiente);
            botonAnterior.onClick.AddListener(Anterior);
            pasaPaginas.ControledBook.OnFlipStarted += SonarPagina;
            Aplicar(0f);
        }

        void OnDestroy()
        {
            if (pasaPaginas && pasaPaginas.ControledBook)
                pasaPaginas.ControledBook.OnFlipStarted -= SonarPagina;
        }

        void SonarPagina() => StartCoroutine(ReproducirPagina());

        IEnumerator ReproducirPagina()
        {
            yield return new WaitForSeconds(0.12f);
            if (m_Agarre.isSelected)
                AudioDemo.Efecto("sonidoPaginasLibro", transform.position, 0.48f, 1.12f, seguir: transform);
        }

        public void Siguiente()
        {
            if (m_Abierto < 0.99f) return;
            if (pasaPaginas.ControledBook.currentPage >= pasaPaginas.ControledBook.TotalPageCount) return;
            pasaPaginas.FlipRightPage();
        }

        public void Anterior()
        {
            if (m_Abierto < 0.99f) return;
            pasaPaginas.FlipLeftPage();
        }

        void Animar(bool abrir)
        {
            AudioDemo.Efecto("sonidoPaginasLibro", transform.position, 0.46f, abrir ? 0.95f : 0.82f, seguir: transform);
            if (m_Anim != null) StopCoroutine(m_Anim);
            m_Anim = StartCoroutine(Abrir(abrir ? 1f : 0f));
        }

        IEnumerator Abrir(float destino)
        {
            float inicio = m_Abierto;
            float d = duracionApertura * Mathf.Abs(destino - inicio);
            for (float t = 0; t < d; t += Time.deltaTime) { Aplicar(Mathf.Lerp(inicio, destino, Mathf.SmoothStep(0, 1, t / d))); yield return null; }
            Aplicar(destino);
        }

        void Aplicar(float k)
        {
            m_Abierto = k;
            bisagraTapa.localRotation = Quaternion.Euler(0, 0, anguloAbierto * k);
            // las páginas aparecen cuando la tapa ya ha pasado la vertical
            float a = Mathf.InverseLerp(0.45f, 0.9f, k);
            paginas.alpha = a;
            paginas.interactable = paginas.blocksRaycasts = a > 0.95f;
            paginas.gameObject.SetActive(a > 0.001f);
            if (colisionAbierto) colisionAbierto.enabled = k > 0.5f;
            // el brillo encantado se dibujaría por encima de las páginas: solo con el libro cerrado
            if (m_Encantado == null) m_Encantado = GetComponent<ObjetoEncantado>();
            if (m_Encantado) m_Encantado.MostrarAura(k < 0.05f);
        }
    }
}


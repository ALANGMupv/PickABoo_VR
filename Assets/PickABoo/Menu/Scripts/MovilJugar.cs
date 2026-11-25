using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PickABoo.Menu
{
    /// <summary>
    /// El escáner (Nokia) es el botón de jugar. Al cogerlo la pantalla pide "JUGAR";
    /// con el gatillo se pulsa la tecla (el móvil se hunde un poco y la pantalla destella) y empieza la partida.
    /// </summary>
    [RequireComponent(typeof(ObjetoEncantado))]
    public class MovilJugar : MonoBehaviour
    {
        public GestorMenu gestor;
        public Transform modelo;                 // malla del móvil (se anima al pulsar)
        public Transform pantalla;               // Canvas de la pantalla (su eje Z apunta hacia dentro del móvil)
        public GameObject pantallaReposo;        // fantasma + "Pick a Boo"
        public GameObject pantallaJugar;         // "JUGAR" + "pulsa el gatillo"
        public GameObject pantallaEntrando;      // "¡A cazar!"
        public Graphic textoJugar;               // parpadea
        public Image destello;                   // blanco sobre la pantalla
        public AudioSource sonidoTecla;          // opcional (categoría Efectos)

        XRGrabInteractable m_Agarre;
        bool m_Pulsado;
        float m_Parpadeo;

        void Awake()
        {
            m_Agarre = GetComponent<XRGrabInteractable>();
            m_Agarre.selectEntered.AddListener(_ => { if (!m_Pulsado) Mostrar(pantallaJugar); });
            m_Agarre.selectExited.AddListener(_ => { if (!m_Pulsado) Mostrar(pantallaReposo); });
            m_Agarre.activated.AddListener(a => { if (!m_Pulsado) StartCoroutine(Pulsar(a.interactorObject)); });
            Mostrar(pantallaReposo);
            if (destello) destello.color = new Color(1, 1, 1, 0);
        }

        void Mostrar(GameObject p)
        {
            pantallaReposo.SetActive(p == pantallaReposo);
            pantallaJugar.SetActive(p == pantallaJugar);
            pantallaEntrando.SetActive(p == pantallaEntrando);
        }

        void Update()
        {
            if (pantallaJugar.activeSelf && textoJugar != null)
            {
                m_Parpadeo += Time.deltaTime;
                var c = textoJugar.color; c.a = Mathf.PingPong(m_Parpadeo * 1.6f, 1f) < 0.5f ? 1f : 0.25f; textoJugar.color = c;
            }
        }

        IEnumerator Pulsar(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor mano)
        {
            m_Pulsado = true;
            ObjetoEncantado.Vibrar(mano, 0.9f, 0.1f);
            if (sonidoTecla != null) sonidoTecla.Play();
            // pulsación de la tecla: el móvil se hunde y rebota
            Vector3 p0 = modelo.localPosition;
            Vector3 hundido = p0 + modelo.parent.InverseTransformDirection(pantalla.forward) * 0.006f;
            for (float t = 0; t < 0.08f; t += Time.deltaTime) { modelo.localPosition = Vector3.Lerp(p0, hundido, t / 0.08f); yield return null; }
            if (destello) destello.color = Color.white;
            for (float t = 0; t < 0.18f; t += Time.deltaTime)
            {
                modelo.localPosition = Vector3.Lerp(hundido, p0, Mathf.SmoothStep(0, 1, t / 0.18f));
                if (destello) destello.color = new Color(1, 1, 1, 1 - t / 0.18f);
                yield return null;
            }
            modelo.localPosition = p0;
            if (destello) destello.color = new Color(1, 1, 1, 0);
            Mostrar(pantallaEntrando);
            gestor.Jugar(this);
        }
    }
}


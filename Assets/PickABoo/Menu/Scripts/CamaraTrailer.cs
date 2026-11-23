using System.Collections;
using PickABoo.Transiciones;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PickABoo.Menu
{
    /// <summary>
    /// Cámara del menú: al cogerla se acerca sola a la cara, dispara un flash
    /// y la escena pasa al tráiler 360 (fundido a blanco).
    /// </summary>
    [RequireComponent(typeof(ObjetoEncantado))]
    public class CamaraTrailer : MonoBehaviour
    {
        public string escenaVideo = "Video360";
        public Transform objetivo;              // la lente (para orientar la cámara hacia el jugador)
        public AudioSource sonidoFlash;         // opcional: sonido de disparo (categoría Efectos)
        public float distanciaCara = 0.22f;
        public float duracionAcercar = 0.6f;

        XRGrabInteractable m_Agarre;
        bool m_Lanzado;

        void Awake()
        {
            m_Agarre = GetComponent<XRGrabInteractable>();
            m_Agarre.selectEntered.AddListener(a => { if (!m_Lanzado) StartCoroutine(Secuencia(a.interactorObject)); });
        }

        IEnumerator Secuencia(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor mano)
        {
            m_Lanzado = true;
            // la cámara deja de seguir a la mano y se acerca a la cara, como si fueras a hacer una foto
            m_Agarre.trackPosition = false;
            m_Agarre.trackRotation = false;
            var cabeza = Camera.main.transform;
            Vector3 p0 = transform.position; Quaternion r0 = transform.rotation;
            for (float t = 0; t < duracionAcercar; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / duracionAcercar);
                Vector3 adelante = Vector3.ProjectOnPlane(cabeza.forward, Vector3.up).normalized;
                Vector3 destino = cabeza.position + adelante * distanciaCara - Vector3.up * 0.04f;
                // la lente mira hacia fuera, en la misma dirección que el jugador
                Quaternion rot = Quaternion.LookRotation(adelante, Vector3.up) * Quaternion.Inverse(Quaternion.Inverse(transform.rotation) * objetivo.rotation);
                transform.SetPositionAndRotation(Vector3.Lerp(p0, destino, k), Quaternion.Slerp(r0, rot, k));
                yield return null;
            }
            ObjetoEncantado.Vibrar(mano, 0.8f, 0.12f);
            if (sonidoFlash != null) sonidoFlash.Play();
            // destello: blanco de golpe, un instante, y fundido a blanco total mientras carga el vídeo
            var f = Fundido.Instancia;
            yield return f.A(new Color(1, 1, 1, 0.85f), 0.06f);
            yield return f.A(new Color(1, 1, 1, 0.35f), 0.15f);
            CargadorEscenas.Ir(escenaVideo, Color.white, 0.5f);
        }
    }
}


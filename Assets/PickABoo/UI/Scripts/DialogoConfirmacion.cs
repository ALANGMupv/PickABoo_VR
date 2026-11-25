using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PickABoo.UI
{
    /// <summary>
    /// Ventana de confirmación con título, explicación y dos botones (Cancelar / acción).
    /// La usan el menú de pausa (volver al menú principal) y la puerta de salida (salir del juego).
    /// </summary>
    public class DialogoConfirmacion : MonoBehaviour
    {
        public CanvasGroup grupo;
        public TMP_Text titulo;
        public TMP_Text mensaje;
        public TMP_Text textoAceptar;
        public Button botonAceptar;
        public Button botonCancelar;
        public float duracion = 0.15f;

        Action m_SiAcepta, m_SiCancela;
        Coroutine m_Anim;

        public bool Abierto { get; private set; }

        void Awake()
        {
            botonAceptar.onClick.AddListener(() => { var a = m_SiAcepta; Cerrar(); a?.Invoke(); });
            botonCancelar.onClick.AddListener(() => { var c = m_SiCancela; Cerrar(); c?.Invoke(); });
            grupo.alpha = 0;
            grupo.interactable = grupo.blocksRaycasts = false;
            grupo.gameObject.SetActive(false);
        }

        public void Preguntar(string tituloTexto, string mensajeTexto, string aceptar, Action siAcepta, Action siCancela = null)
        {
            titulo.text = tituloTexto;
            mensaje.text = mensajeTexto;
            textoAceptar.text = aceptar;
            m_SiAcepta = siAcepta;
            m_SiCancela = siCancela;
            Abierto = true;
            Animar(1);
        }

        public void Cerrar()
        {
            if (!Abierto) return;
            Abierto = false;
            m_SiAcepta = m_SiCancela = null;
            if (isActiveAndEnabled) Animar(0);
            else { grupo.alpha = 0; grupo.gameObject.SetActive(false); }
        }

        void Animar(float destino)
        {
            if (m_Anim != null) StopCoroutine(m_Anim);
            // el propio objeto puede estar desactivado (empieza oculto): hay que activarlo antes de animar
            grupo.gameObject.SetActive(true);
            m_Anim = StartCoroutine(Fundir(destino));
        }

        IEnumerator Fundir(float destino)
        {
            grupo.gameObject.SetActive(true);
            grupo.interactable = grupo.blocksRaycasts = destino > 0.5f;
            float a0 = grupo.alpha;
            for (float t = 0; t < duracion; t += Time.unscaledDeltaTime)
            {
                grupo.alpha = Mathf.Lerp(a0, destino, t / duracion);
                yield return null;
            }
            grupo.alpha = destino;
            if (destino < 0.5f) grupo.gameObject.SetActive(false);
        }
    }
}

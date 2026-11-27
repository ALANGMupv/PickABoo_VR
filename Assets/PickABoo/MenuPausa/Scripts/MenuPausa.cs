using System.Collections;
using System.Collections.Generic;
using PickABoo.Transiciones;
using PickABoo.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

namespace PickABoo.MenuPausa
{
    /// <summary>
    /// Menú de pausa y ajustes: se abre y cierra con el botón ≡ del mando izquierdo (Esc en el editor),
    /// en la entrada y en la partida. Aparece delante del jugador; mientras está abierto no se puede mover ni girar.
    /// </summary>
    public class MenuPausa : MonoBehaviour
    {
        public CanvasGroup grupo;
        [Header("Configuración")]
        public SelectorOpciones selectorGiro;   // 0 = por saltos, 1 = continuo
        public Toggle interruptorMareo;
        [Header("Botones")]
        public Button botonContinuar;
        public Button botonMenuPrincipal;
        [Tooltip("Ventana que pide confirmación antes de volver al menú principal")]
        public DialogoConfirmacion dialogo;
        [Header("Colocación")]
        public float distancia = 0.85f;
        public float bajada = 0.12f;
        public float duracionFundido = 0.2f;

        InputAction m_Boton;
        bool m_Abierto;
        Coroutine m_Anim;
        readonly List<LocomotionProvider> m_Parados = new List<LocomotionProvider>();

        public bool Abierto => m_Abierto;

        void Awake()
        {
            Bloqueado = false;
            m_Boton = new InputAction("Pausa", InputActionType.Button);
            m_Boton.AddBinding("<XRController>{LeftHand}/{MenuButton}");
            m_Boton.AddBinding("<XRController>{LeftHand}/menu");
            m_Boton.AddBinding("<Keyboard>/escape");
            m_Boton.performed += _ => Alternar();

            botonContinuar.onClick.AddListener(Cerrar);
            botonMenuPrincipal.onClick.AddListener(MenuPrincipal);

            selectorGiro.Cambio += i => GestorPreferencias.GiroContinuo = i == 1;
            interruptorMareo.onValueChanged.AddListener(v => GestorPreferencias.ReducirMareo = v);

            grupo.alpha = 0;
            grupo.gameObject.SetActive(false);
        }

        void OnEnable() { m_Boton.Enable(); }
        void OnDisable() { m_Boton.Disable(); }
        void OnDestroy() { m_Boton.Dispose(); }

        // En la entrada (escena Menu) el menú sirve igual para el sonido y los ajustes, pero sin "Menú principal".
        static bool EnMenuPrincipal => SceneManager.GetSceneByName("Menu").isLoaded;

        /// <summary>Impide abrirlo (p. ej. durante el viaje guiado al empezar la partida).</summary>
        public static bool Bloqueado { get; set; }

        public void Alternar() { if (m_Abierto) Cerrar(); else Abrir(); }

        public void Abrir()
        {
            if (m_Abierto || Bloqueado) return;
            var cam = Camera.main;
            if (cam == null) return;
            m_Abierto = true;

            // delante del jugador, a la altura de los ojos un poco más abajo, mirando hacia él
            Vector3 frente = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            if (frente.sqrMagnitude < 0.01f) frente = Vector3.ProjectOnPlane(cam.transform.up, Vector3.up);
            frente.Normalize();
            Vector3 pos = cam.transform.position + frente * distancia - Vector3.up * bajada;
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cam.transform.position, Vector3.up));

            selectorGiro.Fijar(GestorPreferencias.GiroContinuo ? 1 : 0);
            interruptorMareo.SetIsOnWithoutNotify(GestorPreferencias.ReducirMareo);
            botonMenuPrincipal.gameObject.SetActive(!EnMenuPrincipal);
            if (dialogo) dialogo.Cerrar();

            // parar el movimiento mientras está abierto
            m_Parados.Clear();
            foreach (var p in FindObjectsByType<LocomotionProvider>(FindObjectsSortMode.None))
                if (p.enabled) { p.enabled = false; m_Parados.Add(p); }

            Animar(1);
        }

        public void Cerrar()
        {
            if (!m_Abierto) return;
            m_Abierto = false;
            if (dialogo) dialogo.Cerrar();
            foreach (var p in m_Parados) if (p) p.enabled = true;
            m_Parados.Clear();
            Animar(0);
        }

        void MenuPrincipal()
        {
            // se pregunta en una ventana propia para que el texto se lea entero y la salida no sea accidental
            dialogo.Preguntar("¿Volver al menú principal?", "Perderás el progreso de esta partida.", "Sí, volver", () =>
            {
                botonMenuPrincipal.interactable = botonContinuar.interactable = false;
                CargadorEscenas.Ir("Menu", Color.black, 0.5f);
            });
        }

        void Animar(float destino)
        {
            if (m_Anim != null) StopCoroutine(m_Anim);
            m_Anim = StartCoroutine(Fundir(destino));
        }

        IEnumerator Fundir(float destino)
        {
            grupo.gameObject.SetActive(true);
            grupo.interactable = grupo.blocksRaycasts = destino > 0.5f;
            float a0 = grupo.alpha;
            Vector3 e0 = grupo.transform.localScale, e1 = Vector3.one * (destino > 0.5f ? 1f : 0.92f);
            if (destino > 0.5f && a0 < 0.01f) e0 = Vector3.one * 0.92f;
            for (float t = 0; t < duracionFundido; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / duracionFundido);
                grupo.alpha = Mathf.Lerp(a0, destino, k);
                grupo.transform.localScale = Vector3.Lerp(e0, e1, k);
                yield return null;
            }
            grupo.alpha = destino;
            grupo.transform.localScale = Vector3.one;
            if (destino < 0.5f) grupo.gameObject.SetActive(false);
        }
    }
}

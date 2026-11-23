using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

namespace PickABoo.Video360
{
    /// <summary>
    /// Mando del reproductor 360: flota delante del jugador (se recoloca si gira la cabeza),
    /// se oculta solo tras unos segundos sin usarlo y reaparece al pulsar cualquier botón.
    /// </summary>
    public class PanelReproductor360 : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public ReproductorVideo360 reproductor;
        public CanvasGroup grupo;
        [Header("Controles")]
        public Button botonPlayPausa; public Image iconoPlayPausa; public Sprite spritePlay, spritePausa;
        public Button botonAtrasar, botonAdelantar, botonSalir;
        public Slider barraTiempo; public TMP_Text textoTiempo;
        public Button botonSilencio; public Image iconoSilencio; public Sprite spriteVolumen, spriteSilencio;
        public Slider barraVolumen;
        [Header("Final del vídeo")]
        public GameObject panelFinal; public Button botonOtraVez, botonVolver;
        [Header("Colocación")]
        public float distancia = 1.1f, bajada = 0.32f, anguloRecolocar = 45f;
        public float segundosOcultar = 4f;

        Transform m_Cabeza;
        float m_Inactivo, m_AlfaObjetivo = 1f;
        bool m_Encima, m_Arrastrando, m_Final;
        IDisposable m_Botones;

        void Awake()
        {
            botonPlayPausa.onClick.AddListener(() => { reproductor.PlayPausa(); Actividad(); });
            botonAtrasar.onClick.AddListener(() => { reproductor.Saltar(-10); Actividad(); });
            botonAdelantar.onClick.AddListener(() => { reproductor.Saltar(10); Actividad(); });
            botonSalir.onClick.AddListener(reproductor.Salir);
            botonSilencio.onClick.AddListener(() => { reproductor.Silenciar(!reproductor.Silenciado); Actividad(); });
            barraVolumen.onValueChanged.AddListener(v => { reproductor.FijarVolumen(v); Actividad(); });
            botonOtraVez.onClick.AddListener(() => { m_Final = false; panelFinal.SetActive(false); reproductor.VerOtraVez(); Actividad(); });
            botonVolver.onClick.AddListener(reproductor.Salir);
            reproductor.Terminado += () => { m_Final = true; panelFinal.SetActive(true); Actividad(); };
            panelFinal.SetActive(false);
            // la barra de tiempo se mueve sola; si el jugador la arrastra, se salta al soltar
            var arrastre = barraTiempo.gameObject.AddComponent<ArrastreBarra>();
            arrastre.alEmpezar = () => m_Arrastrando = true;
            arrastre.alSoltar = () => { m_Arrastrando = false; reproductor.IrA(barraTiempo.value * reproductor.Duracion); Actividad(); };
        }

        void OnEnable() { m_Botones = InputSystem.onAnyButtonPress.Call(_ => Actividad()); }
        void OnDisable() { m_Botones?.Dispose(); }

        void Start()
        {
            barraVolumen.SetValueWithoutNotify(reproductor.Volumen);
            if (Camera.main) m_Cabeza = Camera.main.transform;
            Recolocar(true);
        }

        public void OnPointerEnter(PointerEventData e) { m_Encima = true; Actividad(); }
        public void OnPointerExit(PointerEventData e) { m_Encima = false; }

        void Actividad() { m_Inactivo = 0f; m_AlfaObjetivo = 1f; }

        void Update()
        {
            if (m_Cabeza == null && Camera.main) m_Cabeza = Camera.main.transform;
            // ocultar solo si se está reproduciendo y nadie lo usa
            if (reproductor.Reproduciendo && !m_Encima && !m_Arrastrando && !m_Final) m_Inactivo += Time.deltaTime;
            if (m_Inactivo > segundosOcultar) m_AlfaObjetivo = 0f;
            grupo.alpha = Mathf.MoveTowards(grupo.alpha, m_AlfaObjetivo, Time.deltaTime * 3f);
            grupo.interactable = grupo.blocksRaycasts = grupo.alpha > 0.5f;

            if (reproductor.Preparado)
            {
                if (!m_Arrastrando && reproductor.Duracion > 0) barraTiempo.SetValueWithoutNotify((float)(reproductor.Tiempo / reproductor.Duracion));
                double mostrado = m_Arrastrando ? barraTiempo.value * reproductor.Duracion : reproductor.Tiempo;
                textoTiempo.text = Formato(mostrado) + " / " + Formato(reproductor.Duracion);
            }
            iconoPlayPausa.sprite = reproductor.Reproduciendo ? spritePausa : spritePlay;
            iconoSilencio.sprite = reproductor.Silenciado ? spriteSilencio : spriteVolumen;
            // el primer medio segundo el tracking aún se asienta: se coloca delante sin esperar al giro
            Recolocar(Time.timeSinceLevelLoad < 0.5f);
        }

        // Se queda quieto mientras miras alrededor y solo se recoloca si te giras mucho.
        void Recolocar(bool inmediato)
        {
            if (m_Cabeza == null) return;
            Vector3 frente = Vector3.ProjectOnPlane(m_Cabeza.forward, Vector3.up).normalized;
            if (frente.sqrMagnitude < 0.01f) return;
            Vector3 actual = Vector3.ProjectOnPlane(transform.position - m_Cabeza.position, Vector3.up).normalized;
            bool fuera = Vector3.Angle(actual, frente) > anguloRecolocar || inmediato;
            if (!fuera && !m_Recolocando) return;
            m_Recolocando = true;
            Vector3 destino = m_Cabeza.position + frente * distancia - Vector3.up * bajada;
            Quaternion rot = Quaternion.LookRotation(destino - m_Cabeza.position, Vector3.up);
            float k = inmediato ? 1f : Time.deltaTime * 3f;
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, destino, k), Quaternion.Slerp(transform.rotation, rot, k));
            if ((transform.position - destino).sqrMagnitude < 0.0004f) m_Recolocando = false;
        }
        bool m_Recolocando;

        static string Formato(double s) { int t = Mathf.Max(0, (int)s); return (t / 60) + ":" + (t % 60).ToString("00"); }

        class ArrastreBarra : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            public Action alEmpezar, alSoltar;
            public void OnPointerDown(PointerEventData e) => alEmpezar?.Invoke();
            public void OnPointerUp(PointerEventData e) => alSoltar?.Invoke();
        }
    }
}

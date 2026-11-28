using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PickABoo.Sonido;
using PickABoo.Transiciones;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace PickABoo.Menu
{
    /// <summary>
    /// Escena Menu: carga la casa (MainDemo) debajo, limita el movimiento a la entrada
    /// y, al pulsar JUGAR en el escáner, lleva al jugador "justo después de la entrada" y descarga el menú.
    /// </summary>
    public class GestorMenu : MonoBehaviour, ITunnelingVignetteProvider
    {
        [Header("Escenas")]
        public string escenaCasa = "MainDemo";
        [Header("Entrada")]
        public BoxCollider bloqueo;               // pared invisible del hueco de los postes
        public Bounds zonaMenu;                   // suelo de la entrada (solo ahí funciona el teletransporte)
        public ObjetoEncantado[] objetosMenu;     // se apagan al empezar
        [Header("Transición al jugar")]
        public Transform destinoJuego;            // posición en la que empieza la partida
        public float duracionTransicion = 3.2f;
        public float aperturaVineta = 0.65f;
        public GameObject prefabVineta;           // viñeta de confort de XRIT (se añade si el XR Origin no la trae)

        XROrigin m_Origen;
        readonly List<TeleportationArea> m_AreasBloqueadas = new List<TeleportationArea>();
        bool m_Jugando;
        VignetteParameters m_Parametros;
        public VignetteParameters vignetteParameters => m_Parametros;

        IEnumerator Start()
        {
            var casa = SceneManager.GetSceneByName(escenaCasa);
            if (!casa.isLoaded)
            {
                // la cámara del jugador viene con la casa: hasta que no está cargada no hay nada que ver,
                // así que la carga puede usar más tiempo de cada fotograma y termina antes
                var prioridad = Application.backgroundLoadingPriority;
                Application.backgroundLoadingPriority = ThreadPriority.High;
                var op = SceneManager.LoadSceneAsync(escenaCasa, LoadSceneMode.Additive);
                while (!op.isDone) yield return null;
                Application.backgroundLoadingPriority = prioridad;
                casa = SceneManager.GetSceneByName(escenaCasa);
            }
            SceneManager.SetActiveScene(casa);              // la luz, el cielo y las sondas son los de la casa
            LightProbes.TetrahedralizeAsync();
            m_Origen = FindFirstObjectByType<XROrigin>();
            var f = Fundido.Instancia;
            if (f != null) { f.Fijar(new Color(CargadorEscenas.ColorEntrada.r, CargadorEscenas.ColorEntrada.g, CargadorEscenas.ColorEntrada.b, 1)); StartCoroutine(f.Aparecer(1.2f)); }
            LimitarAEntrada(true);
        }

        void LimitarAEntrada(bool limitar)
        {
            bloqueo.enabled = limitar;
            if (limitar)
            {
                m_AreasBloqueadas.Clear();
                foreach (var a in FindObjectsByType<TeleportationArea>(FindObjectsSortMode.None))
                {
                    var c = a.GetComponent<Collider>();
                    if (c != null && !zonaMenu.Intersects(c.bounds)) { a.enabled = false; m_AreasBloqueadas.Add(a); }
                }
            }
            else
            {
                foreach (var a in m_AreasBloqueadas) if (a) a.enabled = true;
                m_AreasBloqueadas.Clear();
            }
        }

        public void Jugar(MovilJugar movil)
        {
            if (m_Jugando) return;
            m_Jugando = true;
            // el menú de pausa no se abre durante el viaje guiado (reactivaría el movimiento a mitad)
            MenuPausa.MenuPausa.Bloqueado = true;
            var pausa = FindFirstObjectByType<MenuPausa.MenuPausa>();
            if (pausa) pausa.Cerrar();
            StartCoroutine(Transicion(movil));
        }

        IEnumerator Transicion(MovilJugar movil)
        {
            foreach (var o in objetosMenu) if (o && o.gameObject != movil.gameObject) o.Apagar();
            // el escáner se queda en la mano del jugador durante toda la partida: ya no se puede soltar
            var agarreMovil = movil.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            var encantado = movil.GetComponent<ObjetoEncantado>();
            if (encantado) encantado.Apagar(0.8f, false);
            foreach (var mano in m_Origen.GetComponentsInChildren<PickABoo.Jugador.ManoMando>(true))
                if (mano.Sostiene(agarreMovil)) { mano.Fijar(); break; }
            yield return new WaitForSeconds(0.36f);
            AudioDemo.ComenzarViaje(m_Origen.Camera.transform.position);
            yield return new WaitForSeconds(0.24f);

            // movimiento guiado con viñeta de confort; el jugador no controla el desplazamiento mientras dura
            var proveedores = m_Origen.GetComponentsInChildren<LocomotionProvider>().Where(p => p.enabled).ToList();
            foreach (var p in proveedores) p.enabled = false;
            var vineta = m_Origen.GetComponentInChildren<TunnelingVignetteController>(true);
            if (vineta == null && prefabVineta != null)
            {
                var v = Instantiate(prefabVineta, m_Origen.Camera.transform, false);
                vineta = v.GetComponentInChildren<TunnelingVignetteController>(true);
            }
            m_Parametros = new VignetteParameters { apertureSize = aperturaVineta, featheringEffect = 0.25f, easeInTime = 0.4f, easeOutTime = 0.6f, easeInTimeLock = false };
            if (vineta) { vineta.gameObject.SetActive(true); vineta.BeginTunnelingVignette(this); }
            bloqueo.enabled = false;

            var cc = m_Origen.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            Vector3 p0 = m_Origen.transform.position;
            Vector3 p1 = new Vector3(destinoJuego.position.x, p0.y, destinoJuego.position.z);
            for (float t = 0; t < duracionTransicion; t += Time.deltaTime)
            {
                m_Origen.transform.position = Vector3.Lerp(p0, p1, Mathf.SmoothStep(0, 1, t / duracionTransicion));
                yield return null;
            }
            m_Origen.transform.position = p1;
            if (cc) cc.enabled = true;
            if (vineta) vineta.EndTunnelingVignette(this);
            foreach (var p in proveedores) if (p) p.enabled = true;
            LimitarAEntrada(false);
            MenuPausa.MenuPausa.Bloqueado = false;
            AudioDemo.LlegarACasa();

            // el escáner se queda en la casa; el resto del menú se descarga
            var casa = SceneManager.GetSceneByName(escenaCasa);
            movil.transform.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(movil.gameObject, casa);
            yield return new WaitForSeconds(0.5f);
            SceneManager.UnloadSceneAsync(gameObject.scene);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.4f);
            Gizmos.DrawWireCube(zonaMenu.center, zonaMenu.size);
            if (destinoJuego) Gizmos.DrawSphere(destinoJuego.position, 0.15f);
        }
    }
}

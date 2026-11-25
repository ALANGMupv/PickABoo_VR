using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PickABoo.Transiciones
{
    /// <summary>
    /// Cambio de escena con fundido. Basado en "jumpSceneSmooth" (PDF XRIT parte 2) y en
    /// "videoMonitoring" (PDF vídeo 360): carga asíncrona y activación cuando toca.
    /// </summary>
    public static class CargadorEscenas
    {
        class Ejecutor : MonoBehaviour { }
        static Ejecutor s_Ejecutor;

        static Ejecutor Runner
        {
            get
            {
                if (s_Ejecutor == null)
                {
                    var go = new GameObject("CargadorEscenas");
                    Object.DontDestroyOnLoad(go);
                    s_Ejecutor = go.AddComponent<Ejecutor>();
                }
                return s_Ejecutor;
            }
        }

        public static bool Cargando { get; private set; }

        /// <summary>Funde al color indicado, carga la escena y deja que la nueva escena haga su propio fundido de entrada.</summary>
        public static void Ir(string escena, Color colorFundido, float duracionFundido = 0.6f)
        {
            if (Cargando) return;
            Runner.StartCoroutine(Cargar(escena, colorFundido, duracionFundido));
        }

        static IEnumerator Cargar(string escena, Color color, float duracion)
        {
            Cargando = true;
            var carga = SceneManager.LoadSceneAsync(escena);
            carga.allowSceneActivation = false;
            var f = Fundido.Instancia;
            if (f != null) yield return f.A(new Color(color.r, color.g, color.b, 1), duracion);
            while (carga.progress < 0.9f) yield return null;
            ColorEntrada = color;
            carga.allowSceneActivation = true;
            while (!carga.isDone) yield return null;
            Cargando = false;
        }

        /// <summary>Color con el que debe empezar la escena recién cargada (lo lee su script de entrada).</summary>
        public static Color ColorEntrada { get; private set; } = Color.black;
    }
}

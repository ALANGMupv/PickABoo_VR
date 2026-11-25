using UnityEngine;
using UnityEngine.SceneManagement;

namespace PickABoo.Menu
{
    /// <summary>
    /// Muestra su objeto "contenido" solo cuando la escena del menú no está cargada.
    /// Se usa en la entrada de MainDemo: el libro y la cámara del menú siguen ahí durante la partida,
    /// como simple decoración, sin duplicarse mientras el menú (con los de verdad) está abierto.
    /// </summary>
    public class SoloFueraDelMenu : MonoBehaviour
    {
        public GameObject contenido;
        public string escenaMenu = "Menu";

        void OnEnable()
        {
            SceneManager.sceneLoaded += AlCargar;
            SceneManager.sceneUnloaded += AlDescargar;
            Actualizar();
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= AlCargar;
            SceneManager.sceneUnloaded -= AlDescargar;
        }

        void AlCargar(Scene s, LoadSceneMode m) => Actualizar();
        void AlDescargar(Scene s) => Actualizar();

        void Actualizar()
        {
            if (contenido) contenido.SetActive(!SceneManager.GetSceneByName(escenaMenu).isLoaded);
        }
    }
}

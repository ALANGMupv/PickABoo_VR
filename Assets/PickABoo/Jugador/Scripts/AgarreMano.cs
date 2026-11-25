using UnityEngine;

namespace PickABoo.Jugador
{
    /// <summary>
    /// Indica cómo se sujeta un objeto con la mano: dónde apoya la palma en cada mano.
    /// Cada contacto es un hijo cuyo eje Z apunta hacia donde van los dedos y cuyo eje Y entra en el objeto
    /// (la palma apoyada en ese punto). Lo usa <see cref="ManoMando"/> para colocar el objeto en la palma.
    /// </summary>
    public class AgarreMano : MonoBehaviour
    {
        public Transform contactoDerecha;
        public Transform contactoIzquierda;

        public Transform Contacto(bool derecha) => derecha ? contactoDerecha : (contactoIzquierda != null ? contactoIzquierda : contactoDerecha);
    }
}

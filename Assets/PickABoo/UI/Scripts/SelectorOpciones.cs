using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PickABoo.UI
{
    /// <summary>Botones de opción única (p. ej. "Por saltos | Continuo"): la elegida se resalta en mostaza.</summary>
    public class SelectorOpciones : MonoBehaviour
    {
        public Button[] opciones;
        public event Action<int> Cambio;
        public int Seleccion { get; private set; }

        void Awake()
        {
            for (int i = 0; i < opciones.Length; i++)
            {
                int k = i;
                opciones[i].onClick.AddListener(() => { if (Seleccion != k) { Fijar(k); Cambio?.Invoke(k); } });
            }
        }

        public void Fijar(int i)
        {
            Seleccion = i;
            for (int j = 0; j < opciones.Length; j++)
            {
                bool on = j == i;
                opciones[j].targetGraphic.color = on ? PaletaPickABoo.Mostaza : PaletaPickABoo.Apagado;
                var t = opciones[j].GetComponentInChildren<TMP_Text>();
                if (t) t.color = on ? PaletaPickABoo.Granate : PaletaPickABoo.Crema;
            }
        }
    }
}

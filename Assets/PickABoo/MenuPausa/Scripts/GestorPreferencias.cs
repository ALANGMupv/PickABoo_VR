using System;
using UnityEngine;

namespace PickABoo.MenuPausa
{
    /// <summary>Opciones de comodidad del jugador, guardadas en PlayerPrefs.</summary>
    public static class GestorPreferencias
    {
        const string ClaveGiro = "PickABoo.GiroContinuo";
        const string ClaveMareo = "PickABoo.ReducirMareo";

        public static event Action Cambio;

        /// <summary>false = giro por saltos (por defecto, marea menos); true = giro continuo.</summary>
        public static bool GiroContinuo
        {
            get => PlayerPrefs.GetInt(ClaveGiro, 0) == 1;
            set { PlayerPrefs.SetInt(ClaveGiro, value ? 1 : 0); PlayerPrefs.Save(); Cambio?.Invoke(); }
        }

        /// <summary>Oscurece los bordes de la vista mientras te desplazas o giras (viñeta de confort). Activado por defecto.</summary>
        public static bool ReducirMareo
        {
            get => PlayerPrefs.GetInt(ClaveMareo, 1) == 1;
            set { PlayerPrefs.SetInt(ClaveMareo, value ? 1 : 0); PlayerPrefs.Save(); Cambio?.Invoke(); }
        }
    }
}

using System;
using UnityEngine;

namespace PickABoo.AjustesSonido
{
    public enum CategoriaAudio { General, Musica, Efectos, Ambiente }

    /// <summary>
    /// Volúmenes del juego por categoría (0-1), guardados en PlayerPrefs.
    /// Cualquier AudioSource con <see cref="FuenteAudioCategoria"/> se ajusta solo cuando cambian.
    /// </summary>
    public static class GestorVolumen
    {
        public const float PorDefecto = 0.8f;
        const string Prefijo = "PickABoo.Volumen.";
        const string ClaveSilencio = "PickABoo.Silencio";

        /// <summary>Se lanza cada vez que cambia cualquier volumen o el silencio.</summary>
        public static event Action Cambio;

        public static float Obtener(CategoriaAudio c) => PlayerPrefs.GetFloat(Prefijo + c, c == CategoriaAudio.General ? 1f : PorDefecto);

        public static void Fijar(CategoriaAudio c, float valor)
        {
            PlayerPrefs.SetFloat(Prefijo + c, Mathf.Clamp01(valor));
            PlayerPrefs.Save();
            Cambio?.Invoke();
        }

        public static bool Silenciado
        {
            get => PlayerPrefs.GetInt(ClaveSilencio, 0) == 1;
            set { PlayerPrefs.SetInt(ClaveSilencio, value ? 1 : 0); PlayerPrefs.Save(); Cambio?.Invoke(); }
        }

        /// <summary>Volumen final que debe sonar para una categoría (incluye el general y el silencio).</summary>
        public static float Efectivo(CategoriaAudio c)
        {
            if (Silenciado) return 0f;
            float g = Obtener(CategoriaAudio.General);
            return c == CategoriaAudio.General ? g : g * Obtener(c);
        }

        public static void Restablecer()
        {
            foreach (CategoriaAudio c in Enum.GetValues(typeof(CategoriaAudio))) PlayerPrefs.DeleteKey(Prefijo + c);
            PlayerPrefs.DeleteKey(ClaveSilencio);
            PlayerPrefs.Save();
            Cambio?.Invoke();
        }
    }
}

using UnityEngine;

namespace PickABoo.UI
{
    /// <summary>Colores de la interfaz de Pick a Boo (los mismos que el menú del proyecto AR).</summary>
    public static class PaletaPickABoo
    {
        public static readonly Color Granate = Hex(0x4A0010);       // fondo de los paneles
        public static readonly Color GranateClaro = Hex(0x7A2A3A);  // barras vacías, elementos inactivos
        public static readonly Color Crema = Hex(0xFFF4E6);         // texto y botón secundario
        public static readonly Color Mostaza = Hex(0xFFC14D);       // botón principal, iconos, valores
        public static readonly Color MostazaOscura = Hex(0xE0A030); // pulsado

        public static Color Hex(int rgb, float a = 1f) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, a);
    }
}

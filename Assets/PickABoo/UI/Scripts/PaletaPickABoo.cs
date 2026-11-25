using UnityEngine;

namespace PickABoo.UI
{
    /// <summary>Colores de la interfaz de Pick a Boo (los mismos que el menú del proyecto AR).</summary>
    public static class PaletaPickABoo
    {
        public static readonly Color Granate = Hex(0x4A0010);       // fondo de los paneles
        public static readonly Color Apagado = new Color(1f, 0.957f, 0.902f, 0.16f); // opción no elegida / interruptor apagado (crema translúcido)
        public static readonly Color Crema = Hex(0xFFF4E6);         // texto y botón secundario
        public static readonly Color Mostaza = Hex(0xFFC14D);       // botón principal, iconos, valores

        public static Color Hex(int rgb, float a = 1f) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, a);
    }
}

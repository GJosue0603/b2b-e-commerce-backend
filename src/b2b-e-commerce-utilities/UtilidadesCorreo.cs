using System.Text.RegularExpressions;

namespace b2b_e_commerce_utilities
{
    public static class UtilidadesCorreo
    {
        private static readonly Regex _regex = new Regex(
            @"^[a-zA-Z0-9_+-]+(?:\.[a-zA-Z0-9_+-]+)*@(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$"
        );

        public static bool EsCorreoValido(string correo)
        {
            return _regex.IsMatch(correo);
        }

        public static string Normalizar(string correo)
        {
            return correo.ToLowerInvariant().Trim();
        }
    }
}

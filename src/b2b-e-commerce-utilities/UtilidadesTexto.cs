using System.Text.RegularExpressions;

namespace b2b_e_commerce_utilities 
{
    public static class UtilidadesTexto
    {
        public static string NormalizarTexto(string? texto) 
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return string.Empty;
            }

            var textoNormalizado = Regex.Replace(texto.Trim(),@"\s+"," ");
            return textoNormalizado.ToUpperInvariant();
        }
    }
}
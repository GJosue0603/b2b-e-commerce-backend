using System.Security.Claims;

namespace b2b_e_commerce_utilities
{
    public static class UtilidadesDeUsuario
    {
        public static string? ObtenerIdUsuarioActual(ClaimsPrincipal usuario)
        {
            return usuario.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                ?? usuario.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        }
    }
}

using System.Security.Claims;

namespace b2b_e_commerce_utilities
{
    public static class UtilidadesUsuario
    {
        public static string? ObtenerIdUsuarioEnBaseAJWT(ClaimsPrincipal usuario)
        {
            return usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? usuario.FindFirst("sub")?.Value;
        }
    }
}
using b2b_e_commerce_models;
using b2b_e_commerce_repository;

namespace b2b_e_commerce_bll
{
    public interface IServicioDeValidacionDeUsuarios
    {
        Task<Usuario> ValidarUsuario(string usuarioId);
    }

    public class ServicioDeValidacionDeUsuarios : IServicioDeValidacionDeUsuarios
    {
        private readonly IRepositorioDeUsuarios _repositorioDeUsuarios;

        public ServicioDeValidacionDeUsuarios(IRepositorioDeUsuarios repositorioDeUsuarios)
        {
            _repositorioDeUsuarios = repositorioDeUsuarios ?? throw new ArgumentNullException(nameof(repositorioDeUsuarios));
        }

        public async Task<Usuario> ValidarUsuario(string usuarioId)
        {
            var usuario = await _repositorioDeUsuarios.ObtenerUsuarioPorId(usuarioId);

            if (usuario == null || !usuario.Activo)
            {
                throw new UnauthorizedAccessException("Usuario no autorizado o inactivo.");
            }

            return usuario;
        }
    }
}
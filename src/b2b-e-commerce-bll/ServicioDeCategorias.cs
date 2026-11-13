using b2b_e_commerce_models;
using b2b_e_commerce_repository;
using b2b_e_commerce_utilities;

namespace b2b_e_commerce_bll
{
    public interface IServicioDeCategorias
    {
        Task<Categoria?> ObtenerCategoriaPorId(string id);
        Task<ResultadoPaginado<Categoria>> ObtenerListaDeCategorias(
            bool? activo = null,
            int numeroDePagina = 1,
            int cantidadPorPagina = 25
        );
        Task<Categoria> InsertarCategoria(SolicitudDeCategoria solicitud, string usuarioId);
        Task<Categoria?> ModificarCategoria(string id, SolicitudDeCategoria solicitud, string usuarioId);
        Task<int> DesactivarCategoria(string id, string usuarioId);
    }
    public class ServicioDeCategorias : IServicioDeCategorias
    {
        private readonly IRepositorioDeCategorias _repositorioDeCategorias;
        private readonly IRepositorioDeUsuarios _repositorioDeUsuarios;
        public ServicioDeCategorias(IRepositorioDeCategorias repositorioDeCategorias, IRepositorioDeUsuarios repositorioDeUsuarios)
        {
            _repositorioDeCategorias = repositorioDeCategorias ?? throw new ArgumentNullException(nameof(repositorioDeCategorias));
            _repositorioDeUsuarios = repositorioDeUsuarios ?? throw new ArgumentNullException(nameof(repositorioDeUsuarios));
        }
        public async Task<Categoria?> ObtenerCategoriaPorId(string id)
        {
            return await _repositorioDeCategorias.ObtenerCategoriaPorId(id);
        }
        public async Task<ResultadoPaginado<Categoria>> ObtenerListaDeCategorias(
           bool? activo = null,
           int numeroDePagina = 1,
           int cantidadPorPagina = 25
        )
        {
            return await _repositorioDeCategorias.ObtenerListaDeCategorias(activo, numeroDePagina, cantidadPorPagina);
        }

        public async Task<Categoria> InsertarCategoria(SolicitudDeCategoria solicitud, string usuarioId)
        {
            var usuario = await _repositorioDeUsuarios.ObtenerUsuarioPorId(usuarioId);

            if (usuario == null || !usuario.Activo)
            {
                throw new UnauthorizedAccessException("Usuario no autorizado o inactivo.");
            }

            var nombre = UtilidadesTexto.NormalizarTexto(solicitud.Nombre);

            if (string.IsNullOrWhiteSpace(nombre))
            {
                throw new ArgumentException("El nombre de la categoria es requerido.");
            }

            var fechaActual = DateTime.UtcNow;

            var categoria = new Categoria
            {
                Nombre = nombre,
                Descripcion = UtilidadesTexto.NormalizarTexto(solicitud.Descripcion),
                Activo = true,
                CreadoPor = new Colaborador
                {
                    Id = usuario.Id,
                    Nombre = usuario.Nombre,
                    Rol = usuario.Rol
                },
                FechaCreacion = fechaActual,
                FechaModificacion = fechaActual
            };
            return await _repositorioDeCategorias.InsertarCategoria(categoria);
        }

        public async Task<Categoria?> ModificarCategoria(string id, SolicitudDeCategoria solicitud, string usuarioId)
        {
            var usuario = await _repositorioDeUsuarios.ObtenerUsuarioPorId(usuarioId);

            if (usuario == null || !usuario.Activo)
            {
                throw new UnauthorizedAccessException("Usuario no autorizado o inactivo.");
            }

            var nombre = UtilidadesTexto.NormalizarTexto(solicitud.Nombre);

            if (string.IsNullOrWhiteSpace(nombre))
            {
                throw new ArgumentException("El nombre de la categoria es requerido.");
            }

            var categoria = new Categoria
            {
                Nombre = nombre,
                Descripcion = UtilidadesTexto.NormalizarTexto(solicitud.Descripcion),
                ModificadoPor = new Colaborador
                {
                    Id = usuario.Id,
                    Nombre = usuario.Nombre,
                    Rol = usuario.Rol
                },
                FechaModificacion = DateTime.UtcNow
            };
            return await _repositorioDeCategorias.ModificarCategoria(id, categoria);
        }

        public async Task<int> DesactivarCategoria(string id, string usuarioId)
        {
            var usuario = await _repositorioDeUsuarios.ObtenerUsuarioPorId(usuarioId);

            if (usuario == null || !usuario.Activo)
            {
                throw new UnauthorizedAccessException("Usuario no autorizado o inactivo.");
            }

            var modificador = new Colaborador
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Rol = usuario.Rol
            };

            var categoria = await _repositorioDeCategorias.DesactivarCategoria(id, modificador);

            if (categoria != null)
            {
                return 1;
            }

            var categoriaExistente = await _repositorioDeCategorias.ObtenerCategoriaPorId(id);

            if (categoriaExistente == null)
            {
                return 0;
            }

            if (!categoriaExistente.Activo)
            {
                return 2;
            }

            return 0;
        }

    }
}

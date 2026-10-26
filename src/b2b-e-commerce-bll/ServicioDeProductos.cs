using b2b_e_commerce_models;
using b2b_e_commerce_repository;

namespace b2b_e_commerce_bll
{
    public interface IServicioDeProductos
    {
        Task<Producto> ObtenerProductoPorId(string id);
        Task<ResultadoPaginado<Producto>> ObtenerListaDeProductos(
            bool? activo = null,
            int numeroDePagina = 1,
            int cantidadPorPagina = 25
        );
    }
    public class ServicioDeProductos : IServicioDeProductos
    {
        private readonly IRepositorioDeProductos _repositorioDeProductos;

        public ServicioDeProductos(IRepositorioDeProductos repositorioDeProductos)
        {
            _repositorioDeProductos = repositorioDeProductos ?? throw new ArgumentNullException(nameof(repositorioDeProductos));
        }
        public async Task<Producto> ObtenerProductoPorId(string id)
        {
            return await _repositorioDeProductos.ObtenerProductoPorId(id);
        }
        public async Task<ResultadoPaginado<Producto>> ObtenerListaDeProductos(
           bool? activo = null,
           int numeroDePagina = 1,
           int cantidadPorPagina = 25
       )
        {
            return await _repositorioDeProductos.ObtenerListaDeProductos(activo, numeroDePagina, cantidadPorPagina);
        }

    }
}

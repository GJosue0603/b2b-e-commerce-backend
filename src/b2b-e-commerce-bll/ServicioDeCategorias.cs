using b2b_e_commerce_models;
using b2b_e_commerce_repository;

namespace b2b_e_commerce_bll
{
    public interface IServicioDeCategorias
    {
        Task<Categoria> ObtenerCategoriaPorId(string id);
        Task<ResultadoPaginado<Categoria>> ObtenerListaDeCategorias(
            bool? activo = null,
            int numeroDePagina = 1,
            int cantidadPorPagina = 25
        );
    }
    public class ServicioDeCategorias : IServicioDeCategorias
    {
        private readonly IRepositorioDeCategorias _repositorioDeCategorias;
        public ServicioDeCategorias(IRepositorioDeCategorias repositorioDeCategorias)
        {
            _repositorioDeCategorias = repositorioDeCategorias ?? throw new ArgumentNullException(nameof(repositorioDeCategorias));
        }
        public async Task<Categoria> ObtenerCategoriaPorId(string id)
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
    }
}

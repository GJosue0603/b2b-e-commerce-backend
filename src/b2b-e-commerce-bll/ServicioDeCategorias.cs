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
        Task<Categoria> InsertarCategoria(Categoria categoria);
        Task <Categoria> ModificarCategoria(string id, Categoria categoria);
        Task<int> EliminarCategoria(string id, Colaborador modificador);
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
        public async Task<Categoria> InsertarCategoria(Categoria categoria)
        {
            return await _repositorioDeCategorias.InsertarCategoria(categoria);
        }
        public async Task<int> EliminarCategoria(string id, Colaborador modificador)
        {
            return await _repositorioDeCategorias.EliminarCategoria(id, modificador);
        }
        public async Task<Categoria> ModificarCategoria(string id, Categoria categoria)
        {
            return await _repositorioDeCategorias.ModificarCategoria(id, categoria);
        }
    }
}

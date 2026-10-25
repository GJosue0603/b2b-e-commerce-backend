
using b2b_e_commerce_models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace b2b_e_commerce_repository
{
    public interface IRepositorioDeCategorias
    {
       Task<Categoria> ObtenerCategoriaPorId(string id);
       Task<ResultadoPaginado<Categoria>> ObtenerListaDeCategorias(
            bool? activo = null,
            int numeroDePagina = 1,
            int cantidadPorPagina = 25
        );
    }

    public class RepositorioDeCategorias : IRepositorioDeCategorias
    {
        private readonly string COLLECTION_NAME = "categorias";
        private readonly IMongoCollection<Categoria> _collection;

        public RepositorioDeCategorias(IMongoClient client, IConfiguration configuracion)
        {
            ArgumentNullException.ThrowIfNull(client);
            ;
            var databaseName = configuracion["Mongo:Database"];

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new InvalidOperationException("MongoDB database name is not configured.");
            }

            _collection = client.GetDatabase(databaseName).GetCollection<Categoria>(COLLECTION_NAME);
        }
        public async Task<Categoria> ObtenerCategoriaPorId(string id)
        {
            var filter = Builders<Categoria>.Filter.Eq(c => c.Id, id);

            var categoria = await _collection.Find(filter).FirstOrDefaultAsync();

            return categoria;
        }
        public async Task<ResultadoPaginado<Categoria>> ObtenerListaDeCategorias(
            bool? activo = null,
            int numeroDePagina = 1,
            int cantidadPorPagina = 25)
        {
            var filter = FilterDefinition<Categoria>.Empty;

            if (activo.HasValue)
            {
                filter &= Builders<Categoria>.Filter.Eq(
                    c => c.Activo,
                    activo.Value);
            }

            var ordenar = Builders<Categoria>.Sort
                .Ascending(c => c.Nombre)
                .Ascending(c => c.Id);

            int cantidadDeRegistrosAOmitir = checked(
                (int)(((long)numeroDePagina - 1) * cantidadPorPagina));

            var categoriasTask = _collection
                .Find(filter)
                .Sort(ordenar)
                .Skip(cantidadDeRegistrosAOmitir)
                .Limit(cantidadPorPagina)
                .ToListAsync();

            var cantidadTotalTask = _collection.CountDocumentsAsync(filter);

            await Task.WhenAll(categoriasTask, cantidadTotalTask);

            return new ResultadoPaginado<Categoria>(
                categoriasTask.Result,
                cantidadTotalTask.Result,
                numeroDePagina,
                cantidadPorPagina);
        }
    }

}

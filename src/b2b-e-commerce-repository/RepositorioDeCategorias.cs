
using b2b_e_commerce_models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace b2b_e_commerce_repository
{
    public interface IRepositorioDeCategorias
    {
        Task<List<Categoria>> ObtenerCategorias();
        Task<Categoria> ObtenerCategoriaPorId(string id);
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

        public async Task<List<Categoria>> ObtenerCategorias()
        {
            var filter = Builders<Categoria>.Filter.Empty;

            var categorias = await _collection.Find(filter).ToListAsync();

            return categorias;
        }

        public async Task<Categoria> ObtenerCategoriaPorId(string id)
        {
            var filter = Builders<Categoria>.Filter.Eq(c => c.Id, id);

            var categoria = await _collection.Find(filter).FirstOrDefaultAsync();

            return categoria;
        }
    }

}

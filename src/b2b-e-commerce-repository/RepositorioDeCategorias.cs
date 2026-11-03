
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
        Task<Categoria> InsertarCategoria(Categoria categoria, IClientSessionHandle? sesion = null);
        Task<Categoria> ModificarCategoria(string id,Categoria categoria, IClientSessionHandle? sesion = null);
        Task<int> EliminarCategoria(string id, Colaborador modificador);
    }

    public class RepositorioDeCategorias : IRepositorioDeCategorias
    {
        private readonly string COLLECTION_NAME = "categorias";
        private readonly IMongoCollection<Categoria> _collection;

        public RepositorioDeCategorias(IMongoClient client, IConfiguration configuracion)
        {
            ArgumentNullException.ThrowIfNull(client);
            
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
        public async Task<Categoria> InsertarCategoria(Categoria categoria, IClientSessionHandle? sesion = null)
        {
            if (sesion != null)
            {
                await _collection.InsertOneAsync(sesion, categoria);
            }
            else
            {
                await _collection.InsertOneAsync(categoria);
            }

            return categoria;
        }
        public async Task<Categoria> ModificarCategoria(string id, Categoria categoria, IClientSessionHandle? sesion = null)
        {
            var filter = Builders < Categoria>.Filter.And(Builders<Categoria>.Filter.Eq(c => c.Id, id), Builders<Categoria>.Filter.Eq(c => c.Activo, true));

            var actualizacion = Builders<Categoria>.Update
                .Set(c => c.Nombre, categoria.Nombre)
                .Set(c => c.Descripcion, categoria.Descripcion)
                .Set(c => c.ModificadoPor, categoria.ModificadoPor)
                .Set(c => c.FechaModificacion, DateTime.UtcNow);

            var resultado = sesion != null ? await _collection.UpdateOneAsync(sesion, filter, actualizacion): await _collection.UpdateOneAsync(filter, actualizacion);

            if (resultado.MatchedCount == 0) 
            {
                // No se encontro la categoria para actualizar
                return null;
            }

            return await _collection.Find(filter).FirstOrDefaultAsync();
        }
        public async Task<int> EliminarCategoria(string id, Colaborador modificador)
        {
            var filter = Builders<Categoria>.Filter.Eq(c => c.Id, id);

            var categoria = await _collection.Find(filter).FirstOrDefaultAsync();

            if (categoria == null)
            {
                // La categoría no existe
                return 0;
            }
            if(!categoria.Activo)
            {
                //La categoría ya está inactiva
                return 2;
            }

            var actualizacion = Builders<Categoria>.Update
                .Set(c => c.Activo, false)
                .Set(c => c.ModificadoPor, modificador)
                .Set(c => c.FechaModificacion, DateTime.UtcNow);

            await _collection.UpdateOneAsync(filter, actualizacion);

            // La categoría se desactivo correctamente
            return 1;
        }
    }

}

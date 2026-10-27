
using b2b_e_commerce_models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace b2b_e_commerce_repository
{
    public interface IRepositorioDeProductos
    {
        Task<Producto> ObtenerProductoPorId(string id);
        Task<ResultadoPaginado<Producto>> ObtenerListaDeProductos(
            bool? activo = null,
            int numeroDePagina = 1,
            int cantidadPorPagina = 25
        );
    }
    public class RepositorioDeProductos : IRepositorioDeProductos
    {
        private readonly string COLLECTION_NAME = "productos";
        private readonly IMongoCollection<Producto> _collection;

        public RepositorioDeProductos(IMongoClient client, IConfiguration configuracion)
        {
            ArgumentNullException.ThrowIfNull(client);
            var databaseName = configuracion["Mongo:Database"];

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new InvalidOperationException("MongoDB database name is not configured.");
            }

            _collection = client.GetDatabase(databaseName).GetCollection<Producto>(COLLECTION_NAME);
        }
        public async Task<Producto> ObtenerProductoPorId(string id)
        {
            var filter = Builders<Producto>.Filter.Eq(p => p.Id, id);

            var producto = await _collection.Find(filter).FirstOrDefaultAsync();

            return producto;
        }

        public async Task<ResultadoPaginado<Producto>> ObtenerListaDeProductos(
            bool? activo = null,
            int numeroDePagina = 1,
            int cantidadPorPagina = 25
        )
        {
            var filter = FilterDefinition<Producto>.Empty;

            if (activo.HasValue)
            {
                filter &= Builders<Producto>.Filter.Eq(p => p.Activo, activo);
            }

            var project = Builders<Producto>.Projection.Expression(producto => new Producto
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Categoria = producto.Categoria,
                PrecioRegular = producto.PrecioRegular,
                PrecioPyme = producto.PrecioPyme,
                Existencias = producto.Existencias,
                ImagenUrl = producto.ImagenUrl,
                Activo = producto.Activo,
                CreadoPor = producto.CreadoPor,
                FechaCreacion = producto.FechaCreacion,
                ModificadoPor = producto.ModificadoPor,
                FechaModificacion = producto.FechaModificacion
            });

            var ordenar = Builders<Producto>.Sort.Descending(p => p.FechaCreacion)
                .Descending(p => p.Nombre)
                .Descending(p => p.Id);

            // Calcular cantidad de registros a omitir
            int cantidadDeRegistrosAOmitir = checked((int)(((long)numeroDePagina - 1) * cantidadPorPagina));

            // Traer productos de base de datos
            var ProductosTask = _collection
                .Find(filter)
                .Sort(ordenar)
                .Skip(cantidadDeRegistrosAOmitir)
                .Limit(cantidadPorPagina)
                .ToListAsync();

            // Traer cantidad total de productos
            var cantidadTotalTask = _collection.CountDocumentsAsync(filter);

            await Task.WhenAll(ProductosTask, cantidadTotalTask);

            return new ResultadoPaginado<Producto>(
                ProductosTask.Result,
                cantidadTotalTask.Result,
                numeroDePagina,
                cantidadPorPagina
            );
        }
    }
}


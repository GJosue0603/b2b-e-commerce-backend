using b2b_e_commerce_models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace b2b_e_commerce_repository
{
    public interface IRepositorioDeCarritos
    {
        Task<Carrito> ObtenerCarritoPorUsuario(string usuarioId);
        Task<long> ActualizarCantidadDeItem(string usuarioId, string productoId, int cantidad, DateTime fechaYHora);
        Task<long> AgregarItem(string usuarioId, ItemCarrito item, int maximoDeItems, DateTime fechaYHora);
        Task<long> QuitarItem(string usuarioId, string productoId, DateTime fechaYHora);
        Task<long> EliminarCarritoDeUsuario(string usuarioId, IClientSessionHandle? sesion = null);
    }

    public class RepositorioDeCarritos : IRepositorioDeCarritos
    {
        private readonly string COLLECTION_NAME = "carritos";
        private readonly IMongoCollection<Carrito> _collection;
        public RepositorioDeCarritos(IMongoClient client, IConfiguration configuracion)
        {
            ArgumentNullException.ThrowIfNull(client);

            var databaseName = configuracion["Mongo:Database"];

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new InvalidOperationException("El nombre de la base de datos de MongoDB no está configurado.");
            }

            _collection = client.GetDatabase(databaseName).GetCollection<Carrito>(COLLECTION_NAME);
        }

        public async Task<Carrito> ObtenerCarritoPorUsuario(string usuarioId)
        {
            var filter = Builders<Carrito>.Filter.Eq(c => c.Id, usuarioId);

            var carrito = await _collection.Find(filter).FirstOrDefaultAsync();

            return carrito;
        }

        public async Task<long> ActualizarCantidadDeItem(string usuarioId, string productoId, int cantidad, DateTime fechaYHora)
        {
            var filter = Builders<Carrito>.Filter.Eq(c => c.Id, usuarioId);
            filter &= Builders<Carrito>.Filter.ElemMatch(c => c.Items, i => i.ProductoId == productoId);
            var update = Builders<Carrito>.Update
                .Set(c => c.Items.FirstMatchingElement().Cantidad, cantidad)
                .Set(c => c.FechaModificacion, fechaYHora);

            var res = await _collection.UpdateOneAsync(filter, update);

            return res.MatchedCount;
        }

        public async Task<long> AgregarItem(string usuarioId, ItemCarrito item, int maximoDeItems, DateTime fechaYHora)
        {
            // Crea el carrito vacío solo si no existe; al usar el Id del usuario no puede duplicarse
            var filterDeCarrito = Builders<Carrito>.Filter.Eq(c => c.Id, usuarioId);
            var updateDeCarrito = Builders<Carrito>.Update
                .SetOnInsert(c => c.UsuarioId, usuarioId)
                .SetOnInsert(c => c.Items, new List<ItemCarrito>())
                .SetOnInsert(c => c.FechaCreacion, fechaYHora);

            await _collection.UpdateOneAsync(filterDeCarrito, updateDeCarrito, new UpdateOptions { IsUpsert = true });

            var filter = Builders<Carrito>.Filter.Eq(c => c.Id, usuarioId);
            filter &= Builders<Carrito>.Filter.Not(Builders<Carrito>.Filter.ElemMatch(c => c.Items, i => i.ProductoId == item.ProductoId));
            filter &= Builders<Carrito>.Filter.SizeLt(c => c.Items, maximoDeItems);
            var update = Builders<Carrito>.Update
                .Push(c => c.Items, item)
                .Set(c => c.FechaModificacion, fechaYHora);

            var res = await _collection.UpdateOneAsync(filter, update);

            return res.ModifiedCount;
        }

        public async Task<long> QuitarItem(string usuarioId, string productoId, DateTime fechaYHora)
        {
            var filter = Builders<Carrito>.Filter.Eq(c => c.Id, usuarioId);
            var update = Builders<Carrito>.Update
                .PullFilter(c => c.Items, i => i.ProductoId == productoId)
                .Set(c => c.FechaModificacion, fechaYHora);

            var res = await _collection.UpdateOneAsync(filter, update);

            return res.ModifiedCount;
        }

        public async Task<long> EliminarCarritoDeUsuario(string usuarioId, IClientSessionHandle? sesion = null)
        {
            var filter = Builders<Carrito>.Filter.Eq(c => c.Id, usuarioId);

            if (sesion != null)
            {
                var res = await _collection.DeleteOneAsync(sesion, filter);
                return res.DeletedCount;
            }
            else
            {
                var res = await _collection.DeleteOneAsync(filter);
                return res.DeletedCount;
            }
        }
    }
}

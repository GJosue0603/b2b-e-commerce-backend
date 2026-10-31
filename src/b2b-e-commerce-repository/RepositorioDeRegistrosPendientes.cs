using b2b_e_commerce_models;
using b2b_e_commerce_utilities.Excepciones;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace b2b_e_commerce_repository
{
    public interface IRepositorioDeRegistrosPendientes
    {
        Task<RegistroPendiente> ObtenerRegistroPendientePorCorreo(string correo);
        Task<RegistroPendiente> RegistrarUsuario(RegistroPendiente registroPendiente, DateTime fechaYHora);
        Task<long> IncrementarIntentosFallidos(RegistroPendiente registroPendiente, int cantidad = 1);
        Task<long> RemoverRegistroPendiente(string id, IClientSessionHandle? sesion = null);
    }

    public class RepositorioDeRegistrosPendientes : IRepositorioDeRegistrosPendientes
    {
        private readonly string COLLECTION_NAME = "registros_pendientes";
        private readonly IMongoCollection<RegistroPendiente> _collection;
        public RepositorioDeRegistrosPendientes(IMongoClient client, IConfiguration configuracion)
        {
            ArgumentNullException.ThrowIfNull(client);

            var databaseName = configuracion["Mongo:Database"];

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new InvalidOperationException("MongoDB database name is not configured.");
            }

            _collection = client.GetDatabase(databaseName).GetCollection<RegistroPendiente>(COLLECTION_NAME);
        }

        public async Task<RegistroPendiente> ObtenerRegistroPendientePorCorreo(string correo)
        {
            var filter = Builders<RegistroPendiente>.Filter.Eq(r => r.Correo, correo);

            var res = await _collection.Find(filter).FirstOrDefaultAsync();

            return res;
        }

        public async Task<RegistroPendiente> RegistrarUsuario(RegistroPendiente registroPendiente, DateTime fechaYHora)
        {
            var filter = Builders<RegistroPendiente>.Filter.Eq(r => r.Correo, registroPendiente.Correo);
            filter &= Builders<RegistroPendiente>.Filter.Lte(r => r.FechaExpiracion, fechaYHora);

            try
            {
                var res = await _collection.FindOneAndReplaceAsync(filter, registroPendiente, new FindOneAndReplaceOptions<RegistroPendiente> { IsUpsert = true, ReturnDocument = ReturnDocument.After });

                return res;
            }
            // El código 11,000 es el código de DuplicateKey para esta Excepción de Mongo
            catch (MongoCommandException ex) when (ex.Code == 11000)
            {
                throw new CorreoDuplicadoException("Ya existe una solicitud de registro pendiente para este correo electrónico.");
            }
        }

        public async Task<long> IncrementarIntentosFallidos(RegistroPendiente registroPendiente, int cantidad = 1)
        {
            var filter = Builders<RegistroPendiente>.Filter.Eq(r => r.Correo, registroPendiente.Correo);
            filter &= Builders<RegistroPendiente>.Filter.Lt(r => r.IntentosFallidos, Constantes.LIMITE_INTENTOS_FALLIDOS);
            var update = Builders<RegistroPendiente>.Update.Inc(r => r.IntentosFallidos, cantidad);

            var res = await _collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = false });

            return res.ModifiedCount;
        }

        public async Task<long> RemoverRegistroPendiente(string id, IClientSessionHandle? sesion = null)
        {
            var filter = Builders<RegistroPendiente>.Filter.Eq(r => r.Id, id);

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
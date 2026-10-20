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
    }
}
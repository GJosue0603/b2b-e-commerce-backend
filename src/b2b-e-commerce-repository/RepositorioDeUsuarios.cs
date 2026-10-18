using b2b_e_commerce_models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace b2b_e_commerce_repository
{
    public interface IRepositorioDeUsuarios
    {
        Task<Usuario> ObtenerUsuarioPorId(string id);
        Task<Usuario> ObtenerUsuarioPorCorreo(string correo);
        Task<ResultadoPaginado<RespuestaDeUsuario>> ObtenerListaDeUsuarios(
            bool? activo = null,
            int numeroDePagina = 1,
            int cantidadPorPagina = 25
        );
    }

    public class RepositorioDeUsuarios : IRepositorioDeUsuarios
    {
        private readonly string COLLECTION_NAME = "usuarios";
        private readonly IMongoCollection<Usuario> _collection;
        public RepositorioDeUsuarios(IMongoClient client, IConfiguration configuracion)
        {
            ArgumentNullException.ThrowIfNull(client);

            var databaseName = configuracion["Mongo:Database"];

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new InvalidOperationException("MongoDB database name is not configured.");
            }

            _collection = client.GetDatabase(databaseName).GetCollection<Usuario>(COLLECTION_NAME);
        }

        public async Task<Usuario> ObtenerUsuarioPorId(string id)
        {
            var filter = Builders<Usuario>.Filter.Eq(u => u.Id, id);

            var usuario = await _collection.Find(filter).FirstOrDefaultAsync();

            return usuario;
        }

        public async Task<Usuario> ObtenerUsuarioPorCorreo(string correo)
        {
            var filter = Builders<Usuario>.Filter.Eq(u => u.Correo, correo);

            var usuario = await _collection.Find(filter).FirstOrDefaultAsync();

            return usuario;
        }

        public async Task<ResultadoPaginado<RespuestaDeUsuario>> ObtenerListaDeUsuarios(
            bool? activo = null,
            int numeroDePagina = 1,
            int cantidadPorPagina = 25
        )
        {
            var filter = FilterDefinition<Usuario>.Empty;

            if (activo.HasValue)
            {
                filter &= Builders<Usuario>.Filter.Eq(u => u.Activo, activo);
            }

            var project = Builders<Usuario>.Projection.Expression(usuario => new RespuestaDeUsuario
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Apellido1 = usuario.Apellido1,
                Apellido2 = usuario.Apellido2,
                Correo = usuario.Correo,
                Telefono = usuario.Telefono,
                Activo = usuario.Activo,
                Rol = usuario.Rol,
                FechaCreacion = usuario.FechaCreacion,
                FechaActualizacion = usuario.FechaActualizacion
            });

            var ordenar = Builders<Usuario>.Sort.Descending(u => u.FechaCreacion)
                .Descending(u => u.Apellido1)
                .Descending(u => u.Id);

            // Calcular cantidad de registros a omitir
            int cantidadDeRegistrosAOmitir = checked((int)(((long)numeroDePagina - 1) * cantidadPorPagina));

            // Traer usuarios de base de datos
            var usuariosTask = _collection
                .Find(filter)
                .Sort(ordenar)
                .Skip(cantidadDeRegistrosAOmitir)
                .Limit(cantidadPorPagina)
                .Project(project)
                .ToListAsync();

            // Traer cantidad total de usuarios
            var cantidadTotalTask = _collection.CountDocumentsAsync(filter);

            await Task.WhenAll(usuariosTask, cantidadTotalTask);

            return new ResultadoPaginado<RespuestaDeUsuario>(
                usuariosTask.Result,
                cantidadTotalTask.Result,
                numeroDePagina,
                cantidadPorPagina
            );
        }
    }
}
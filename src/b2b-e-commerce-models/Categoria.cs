
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace b2b_e_commerce_models
{
    [BsonIgnoreExtraElements]
    public class Categoria
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfNull]
        public string Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public bool Activo { get; set; }
        public Colaborador CreadoPor { get; set; }
        public DateTime FechaCreacion { get; set; }
        public Colaborador ModificadoPor { get; set; }
        public DateTime FechaModificacion { get; set; }
    }
}

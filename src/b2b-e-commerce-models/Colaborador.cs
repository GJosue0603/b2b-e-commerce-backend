using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace b2b_e_commerce_models
{
    [BsonIgnoreExtraElements]
    public class Colaborador
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfNull]
        public string Id { get; set; }
        public string Nombre { get; set; }
        public string Rol { get; set; }
    }
}

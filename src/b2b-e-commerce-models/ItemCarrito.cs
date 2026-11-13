using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace b2b_e_commerce_models
{
    [BsonIgnoreExtraElements]
    public class ItemCarrito
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string ProductoId { get; set; }
        public int Cantidad { get; set; }
    }
}

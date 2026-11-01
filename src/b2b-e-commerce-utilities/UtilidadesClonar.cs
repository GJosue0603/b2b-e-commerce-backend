using System.Text.Json;

namespace b2b_e_commerce_utilities
{
    public static class UtilidadesClonar
    {
        public static T? Clonar<T>(this T obj)
        {
            var serialized = JsonSerializer.Serialize(obj);
            return JsonSerializer.Deserialize<T>(serialized);
        }
    }
}
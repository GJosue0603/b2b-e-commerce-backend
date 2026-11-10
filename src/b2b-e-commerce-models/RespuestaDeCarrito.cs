namespace b2b_e_commerce_models
{
    public class RespuestaDeCarrito
    {
        public List<RespuestaDeItemCarrito> Items { get; set; } = new List<RespuestaDeItemCarrito>();
        // Suma solo de los productos disponibles
        public decimal Total { get; set; }
    }
}

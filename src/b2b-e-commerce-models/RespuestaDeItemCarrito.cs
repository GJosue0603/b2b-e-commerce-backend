namespace b2b_e_commerce_models
{
    public class RespuestaDeItemCarrito
    {
        public string ProductoId { get; set; }
        public string NombreProducto { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
        public decimal Subtotal { get; set; }
        // Falso si el producto fue desactivado o ya no alcanza el stock
        public bool Disponible { get; set; }
    }
}

namespace b2b_e_commerce_utilities.Excepciones
{
    public class ProductoNoEncontradoException : Exception
    {
        public ProductoNoEncontradoException() : base("El producto no existe o no está disponible.")
        {

        }

        public ProductoNoEncontradoException(string mensaje) : base(mensaje)
        {

        }
    }
}

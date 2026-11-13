namespace b2b_e_commerce_utilities.Excepciones
{
    public class CarritoInvalidoException : Exception
    {
        public CarritoInvalidoException() : base("El carrito no es válido.")
        {

        }

        public CarritoInvalidoException(string mensaje) : base(mensaje)
        {

        }
    }
}

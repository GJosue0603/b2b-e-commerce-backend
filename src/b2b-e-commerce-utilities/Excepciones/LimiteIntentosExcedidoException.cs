namespace b2b_e_commerce_utilities.Excepciones
{
    public class LimiteIntentosExcedidoException : Exception
    {
        public LimiteIntentosExcedidoException() : base("Has excedido el número máximo de intentos de verificación. Solicita un nuevo código para continuar.")
        {

        }

        public LimiteIntentosExcedidoException(string mensaje) : base(mensaje)
        {

        }
    }
}
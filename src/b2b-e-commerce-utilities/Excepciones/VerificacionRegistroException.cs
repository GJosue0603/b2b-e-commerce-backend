namespace b2b_e_commerce_utilities.Excepciones
{
    public class VerificacionRegistroException : Exception
    {
        public VerificacionRegistroException() : base("No fue posible completar la verificación del registro. Inténtalo nuevamente más tarde.")
        {

        }

        public VerificacionRegistroException(string mensaje) : base(mensaje)
        {

        }
    }
}
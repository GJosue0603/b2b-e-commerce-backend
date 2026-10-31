namespace b2b_e_commerce_utilities.Excepciones
{
    public class CodigoVerificacionExpiradoException : Exception
    {
        public CodigoVerificacionExpiradoException() : base("El código de verificación ha expirado. Solicita uno nuevo para continuar.")
        {

        }

        public CodigoVerificacionExpiradoException(string mensaje) : base(mensaje)
        {

        }
    }
}
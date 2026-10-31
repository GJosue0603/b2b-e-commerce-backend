namespace b2b_e_commerce_utilities.Excepciones
{
    public class CodigoVerificacionInvalidoException : Exception
    {
        public CodigoVerificacionInvalidoException() : base("El código de verificación proporcionado es incorrecto.")
        {

        }

        public CodigoVerificacionInvalidoException(string mensaje) : base(mensaje)
        {

        }
    }
}
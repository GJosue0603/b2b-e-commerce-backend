namespace b2b_e_commerce_utilities.Excepciones
{
    public class CodigoVerificacionNoExpiradoException : Exception
    {
        public CodigoVerificacionNoExpiradoException() : base("Ya existe un código de verificación no expirado para este correo electrónico. Por favor, espera a que expire antes de solicitar uno nuevo.")
        {

        }

        public CodigoVerificacionNoExpiradoException(string mensaje) : base(mensaje)
        {

        }
    }
}
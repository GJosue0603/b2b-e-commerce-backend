namespace b2b_e_commerce_utilities.Excepciones
{
    public class RegistroPendienteNoEncontradoException : Exception
    {
        public RegistroPendienteNoEncontradoException() : base("No se encontró una solicitud de registro pendiente para el correo electrónico proporcionado.")
        {

        }

        public RegistroPendienteNoEncontradoException(string mensaje) : base(mensaje)
        {

        }
    }
}
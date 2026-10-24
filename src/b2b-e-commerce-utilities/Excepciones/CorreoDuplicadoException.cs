namespace b2b_e_commerce_utilities.Excepciones
{
    public class CorreoDuplicadoException : Exception
    {
        public CorreoDuplicadoException() : base("El correo electrónico ingresado ya está registrado.")
        {

        }

        public CorreoDuplicadoException(string mensaje) : base(mensaje)
        {
        
        }
    }
}
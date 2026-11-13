namespace b2b_e_commerce_utilities.Excepciones
{
    public class ExistenciasInsuficientesException : Exception
    {
        public ExistenciasInsuficientesException() : base("No hay existencias suficientes del producto.")
        {

        }

        public ExistenciasInsuficientesException(string mensaje) : base(mensaje)
        {

        }
    }
}

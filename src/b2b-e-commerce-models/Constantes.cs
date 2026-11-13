namespace b2b_e_commerce_models
{
    public struct Constantes
    {
        public const string ADMINISTRADOR = "ADMINISTRADOR";
        public const string VENDEDOR = "VENDEDOR";
        public const string COMPRADOR = "COMPRADOR";
        public const string PLANTILLA_VERIFICACION_REGISTRO = "VerificacionRegistro.html";
        public const int LIMITE_INTENTOS_FALLIDOS = 5;
        public const int MAXIMO_CANTIDAD_POR_PRODUCTO = 1000;
        public const int MAXIMO_PRODUCTOS_DISTINTOS_EN_CARRITO = 50;
        // JWT Claims
        public const string ROL_CLAIM = "rol";
        public const string ACTIVO_CLAIM = "activo";
    }
}
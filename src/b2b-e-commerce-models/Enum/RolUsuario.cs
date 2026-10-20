using System.ComponentModel;

namespace b2b_e_commerce_models.Enum
{
    public enum RolUsuario
    {
        [Description(Constantes.COMPRADOR)]
        Comprador = 1,

        [Description(Constantes.VENDEDOR)]
        Vendedor = 2,

        [Description(Constantes.ADMINISTRADOR)]
        Administrador = 3
    }
}
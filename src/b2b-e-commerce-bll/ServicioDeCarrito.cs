using b2b_e_commerce_models;
using b2b_e_commerce_repository;
using b2b_e_commerce_utilities.Excepciones;

namespace b2b_e_commerce_bll
{
    public interface IServicioDeCarrito
    {
        Task<RespuestaDeCarrito> ObtenerCarrito(string usuarioId);
        Task<RespuestaDeCarrito> EstablecerCantidad(string usuarioId, string productoId, int cantidad);
        Task<RespuestaDeCarrito> QuitarProducto(string usuarioId, string productoId);
        Task VaciarCarrito(string usuarioId);
    }

    public class ServicioDeCarrito : IServicioDeCarrito
    {
        private readonly IRepositorioDeCarritos _repositorioDeCarritos;
        private readonly IRepositorioDeProductos _repositorioDeProductos;

        public ServicioDeCarrito(IRepositorioDeCarritos repositorioDeCarritos, IRepositorioDeProductos repositorioDeProductos)
        {
            _repositorioDeCarritos = repositorioDeCarritos ?? throw new ArgumentNullException(nameof(repositorioDeCarritos));
            _repositorioDeProductos = repositorioDeProductos ?? throw new ArgumentNullException(nameof(repositorioDeProductos));
        }

        public async Task<RespuestaDeCarrito> ObtenerCarrito(string usuarioId)
        {
            var carrito = await _repositorioDeCarritos.ObtenerCarritoPorUsuario(usuarioId);

            return await ConstruirRespuesta(carrito);
        }

        public async Task<RespuestaDeCarrito> EstablecerCantidad(string usuarioId, string productoId, int cantidad)
        {
            if (cantidad < 1 || cantidad > Constantes.MAXIMO_CANTIDAD_POR_PRODUCTO)
            {
                throw new CarritoInvalidoException($"La cantidad debe estar entre 1 y {Constantes.MAXIMO_CANTIDAD_POR_PRODUCTO}.");
            }

            var producto = await _repositorioDeProductos.ObtenerProductoPorId(productoId);

            if (producto == null || !producto.Activo)
            {
                throw new ProductoNoEncontradoException();
            }

            if (cantidad > producto.Existencias)
            {
                throw new ExistenciasInsuficientesException($"Solo hay {producto.Existencias} unidades disponibles de este producto.");
            }

            var fechaYHora = DateTime.UtcNow;

            // Cada paso es una operación atómica en Mongo, por lo que cambios simultáneos del mismo usuario no se pisan
            var actualizados = await _repositorioDeCarritos.ActualizarCantidadDeItem(usuarioId, productoId, cantidad, fechaYHora);

            if (actualizados == 0)
            {
                var agregados = await _repositorioDeCarritos.AgregarItem(usuarioId, new ItemCarrito { ProductoId = productoId, Cantidad = cantidad }, Constantes.MAXIMO_PRODUCTOS_DISTINTOS_EN_CARRITO, fechaYHora);

                if (agregados == 0)
                {
                    // Otra petición pudo agregar el producto en este instante; si sigue sin estar, se alcanzó el máximo
                    actualizados = await _repositorioDeCarritos.ActualizarCantidadDeItem(usuarioId, productoId, cantidad, fechaYHora);

                    if (actualizados == 0)
                    {
                        throw new CarritoInvalidoException($"El carrito admite como máximo {Constantes.MAXIMO_PRODUCTOS_DISTINTOS_EN_CARRITO} productos distintos.");
                    }
                }
            }

            var res = await _repositorioDeCarritos.ObtenerCarritoPorUsuario(usuarioId);

            return await ConstruirRespuesta(res);
        }

        public async Task<RespuestaDeCarrito> QuitarProducto(string usuarioId, string productoId)
        {
            await _repositorioDeCarritos.QuitarItem(usuarioId, productoId, DateTime.UtcNow);

            var res = await _repositorioDeCarritos.ObtenerCarritoPorUsuario(usuarioId);

            return await ConstruirRespuesta(res);
        }

        public async Task VaciarCarrito(string usuarioId)
        {
            await _repositorioDeCarritos.EliminarCarritoDeUsuario(usuarioId);
        }

        private async Task<RespuestaDeCarrito> ConstruirRespuesta(Carrito carrito)
        {
            var respuesta = new RespuestaDeCarrito();

            if (carrito == null || carrito.Items.Count == 0)
            {
                return respuesta;
            }

            var productos = (await _repositorioDeProductos.ObtenerProductosPorIds(carrito.Items.Select(i => i.ProductoId)))
                .ToDictionary(p => p.Id);

            foreach (var item in carrito.Items)
            {
                productos.TryGetValue(item.ProductoId, out var producto);

                // shortcut: siempre precio regular, falta la acreditación PYME; usar PrecioPyme cuando exista.
                var precio = producto == null ? 0m : Math.Round((decimal)producto.PrecioRegular, 2);

                respuesta.Items.Add(new RespuestaDeItemCarrito
                {
                    ProductoId = item.ProductoId,
                    NombreProducto = producto == null ? "Producto no disponible" : producto.Nombre,
                    PrecioUnitario = precio,
                    Cantidad = item.Cantidad,
                    Subtotal = precio * item.Cantidad,
                    Disponible = producto != null && producto.Activo && producto.Existencias >= item.Cantidad
                });
            }

            respuesta.Total = respuesta.Items.Where(i => i.Disponible).Sum(i => i.Subtotal);

            return respuesta;
        }
    }
}

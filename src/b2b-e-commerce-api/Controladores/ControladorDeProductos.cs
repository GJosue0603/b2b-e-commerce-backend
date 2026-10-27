using b2b_e_commerce_bll;
using b2b_e_commerce_models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Net;


namespace b2b_e_commerce_api.Controladores
{
    [ApiController]
    [Route("v1/productos")]
    [Produces("application/json")]
    public class ControladorDeProductos : Controller
    {
        private readonly IServicioDeProductos _servicioDeProductos;

        public ControladorDeProductos(IServicioDeProductos servicioDeProductos)
        {
            _servicioDeProductos = servicioDeProductos ?? throw new ArgumentNullException(nameof(servicioDeProductos));
        }

        [HttpGet]
        [Route("{productoId}")]
        [ProducesResponseType(typeof(Producto), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ObtenerProducto(string productoId)
        {
            if (string.IsNullOrWhiteSpace(productoId))
            {
                return BadRequest("El ProductoId es requerido.");
            }

            if (!ObjectId.TryParse(productoId, out var id))
            {
                return BadRequest("ProductoId debe ser un ObjectId (24 hex string) válido.");
            }

            var res = await _servicioDeProductos.ObtenerProductoPorId(productoId);

            if (res == null)
            {
                return NotFound("Producto no encontrado.");
            }

            return Ok(res);
        }

        [HttpGet]
        [Route("")]
        [ProducesResponseType(typeof(ResultadoPaginado<Producto>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ObtenerListaDeProductos(
            bool? activo = null,
            int numeroDePagina = 1,
            int cantidadPorPagina = 25
        )
        {
            if (numeroDePagina < 1)
            {
                return BadRequest("El número de página tiene que ser mayor o igual a 1.");
            }

            if (cantidadPorPagina < 1 || cantidadPorPagina > 100)
            {
                return BadRequest("La cantidad de productos por página tiene que estar entre 1 y 100.");
            }

            long cantidadDeRegistrosAOmitir = ((long)numeroDePagina - 1) * cantidadPorPagina;
            if (cantidadDeRegistrosAOmitir > int.MaxValue)
            {
                return BadRequest("El número de página solicitado supera el límite permitido.");
            }

            var res = await _servicioDeProductos.ObtenerListaDeProductos(activo, numeroDePagina, cantidadPorPagina);

            return Ok(res);
        }


    }
}

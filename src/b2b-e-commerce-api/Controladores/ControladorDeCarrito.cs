using b2b_e_commerce_bll;
using b2b_e_commerce_models;
using b2b_e_commerce_utilities;
using b2b_e_commerce_utilities.Excepciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Net;

namespace b2b_e_commerce_api.Controladores
{
    [ApiController]
    [Route("v1/carrito")]
    [Produces("application/json")]
    [Authorize]
    public class ControladorDeCarrito : Controller
    {
        private const string RolesConCarrito = Constantes.COMPRADOR + "," + Constantes.VENDEDOR + "," + Constantes.ADMINISTRADOR;

        private readonly IServicioDeCarrito _servicioDeCarrito;

        public ControladorDeCarrito(IServicioDeCarrito servicioDeCarrito)
        {
            _servicioDeCarrito = servicioDeCarrito ?? throw new ArgumentNullException(nameof(servicioDeCarrito));
        }

        private string UsuarioIdActual => UtilidadesUsuario.ObtenerIdUsuarioEnBaseAJWT(User)!;

        [HttpGet]
        [Route("")]
        [Authorize(Roles = RolesConCarrito)]
        [ProducesResponseType(typeof(RespuestaDeCarrito), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ObtenerCarrito()
        {
            var res = await _servicioDeCarrito.ObtenerCarrito(UsuarioIdActual);

            return Ok(res);
        }

        [HttpPut]
        [Route("items/{productoId}")]
        [Authorize(Roles = RolesConCarrito)]
        [ProducesResponseType(typeof(RespuestaDeCarrito), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> AgregarProductoAlCarrito(string productoId, [FromBody] SolicitudDeItemCarrito solicitud)
        {
            if (string.IsNullOrWhiteSpace(productoId))
            {
                return BadRequest("El ProductoId es requerido.");
            }

            if (!ObjectId.TryParse(productoId, out var id))
            {
                return BadRequest("ProductoId debe ser un ObjectId (24 hex string) válido.");
            }

            if (solicitud == null)
            {
                return BadRequest("La cantidad es requerida.");
            }

            try
            {
                var res = await _servicioDeCarrito.AgregarProductoAlCarrito(UsuarioIdActual, productoId, solicitud.Cantidad);

                return Ok(res);
            }
            catch (ProductoNoEncontradoException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ExistenciasInsuficientesException ex)
            {
                return Conflict(ex.Message);
            }
            catch (CarritoInvalidoException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Route("items/{productoId}")]
        [Authorize(Roles = RolesConCarrito)]
        [ProducesResponseType(typeof(RespuestaDeCarrito), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> QuitarProducto(string productoId)
        {
            if (string.IsNullOrWhiteSpace(productoId))
            {
                return BadRequest("El ProductoId es requerido.");
            }

            if (!ObjectId.TryParse(productoId, out var id))
            {
                return BadRequest("ProductoId debe ser un ObjectId (24 hex string) válido.");
            }

            var res = await _servicioDeCarrito.QuitarProducto(UsuarioIdActual, productoId);

            return Ok(res);
        }

        [HttpDelete]
        [Route("")]
        [Authorize(Roles = RolesConCarrito)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> VaciarCarrito()
        {
            await _servicioDeCarrito.VaciarCarrito(UsuarioIdActual);

            return NoContent();
        }
    }
}

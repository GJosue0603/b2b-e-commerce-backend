using b2b_e_commerce_bll;
using b2b_e_commerce_models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Net;
using b2b_e_commerce_utilities;

namespace b2b_e_commerce_api.Controladores
{
    [ApiController]
    [Route("v1/categorias")]
    [Produces("application/json")]
    public class ControladorDeCategorias : Controller
    {
        private readonly IServicioDeCategorias _servicioDeCategorias;
        public ControladorDeCategorias(IServicioDeCategorias servicioDeCategorias)
        {
            _servicioDeCategorias = servicioDeCategorias ?? throw new ArgumentNullException(nameof(servicioDeCategorias));
        }

        [HttpGet]
        [Route("{categoriaId}")]
        [ProducesResponseType(typeof(Categoria), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ObtenerCategoria(string categoriaId)
        {
            if (string.IsNullOrWhiteSpace(categoriaId))
            {
                return BadRequest("La CategoriaId es requerido.");
            }

            if (!ObjectId.TryParse(categoriaId, out var id))
            {
                return BadRequest("CategoriaId debe ser un ObjectId (24 hex string) válido.");
            }

            var res = await _servicioDeCategorias.ObtenerCategoriaPorId(categoriaId);

            if (res == null)
            {
                return NotFound("Categoria no encontrada.");
            }

            return Ok(res);
        }

        [HttpGet]
        [Route("")]
        [ProducesResponseType(typeof(ResultadoPaginado<Categoria>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ObtenerListaDeCategorias(
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

            var res = await _servicioDeCategorias.ObtenerListaDeCategorias(activo, numeroDePagina, cantidadPorPagina);

            return Ok(res);
        }
        [HttpPost]
        [Route("")]
        [Authorize(Roles = Constantes.ADMINISTRADOR)]
        [ProducesResponseType(typeof(Categoria), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> InsertarCategoria([FromBody] SolicitudDeCategoria solicitud)
        {
            if (solicitud == null)
            {
                return BadRequest("La categoría es requerida.");
            }

            if (string.IsNullOrWhiteSpace(solicitud.Nombre))
            {
                return BadRequest("El nombre de la categoría es requerido.");
            }

            var usuarioId = UtilidadesUsuario.ObtenerIdUsuarioEnBaseAJWT(User);

            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return Unauthorized("No se pudo identificar al usuario autenticado.");
            }
            try
            {
                var categoria = await _servicioDeCategorias.InsertarCategoria(solicitud, usuarioId);
                return StatusCode((int)HttpStatusCode.Created, categoria);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Route("{categoriaId}")]
        [Authorize(Roles = Constantes.ADMINISTRADOR)]
        [ProducesResponseType(typeof(Categoria), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ModificarCategoria(string categoriaId,[FromBody] SolicitudDeCategoria solicitud)
        {
            if (string.IsNullOrWhiteSpace(categoriaId))
            {
                return BadRequest("El ID de la categoría es requerido.");
            }

            if (!ObjectId.TryParse(categoriaId, out _))
            {
                return BadRequest("El ID de la categoría no es válido.");
            }

            if (solicitud == null)
            {
                return BadRequest("La categoría es requerida.");
            }

            if (string.IsNullOrWhiteSpace(solicitud.Nombre))
            {
                return BadRequest("El nombre de la categoría es requerido.");
            }

            var usuarioId = UtilidadesUsuario.ObtenerIdUsuarioEnBaseAJWT(User);

            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return Unauthorized("No se pudo identificar al usuario autenticado.");
            }

            try
            {
                var categoria = await _servicioDeCategorias.ModificarCategoria(categoriaId, solicitud, usuarioId);

                if (categoria == null)
                {
                    return NotFound("La categoría no existe o está inactiva.");
                }

                return Ok(categoria);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpDelete]
        [Route("{categoriaId}")]
        [Authorize(Roles = Constantes.ADMINISTRADOR)]
        public async Task<IActionResult> DesactivarCategoria(string categoriaId)
        {
            if (string.IsNullOrWhiteSpace(categoriaId))
            {
                return BadRequest("El ID de la categoría es requerido.");
            }

            if (!ObjectId.TryParse(categoriaId, out _))
            {
                return BadRequest("El ID de la categoría no es válido.");
            }

            var usuarioId = UtilidadesUsuario.ObtenerIdUsuarioEnBaseAJWT(User);

            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return Unauthorized("No se pudo identificar al usuario autenticado.");
            }

            try
            {
                var resultado = await _servicioDeCategorias.DesactivarCategoria(categoriaId, usuarioId);

                if (resultado == 0)
                {
                    return NotFound("La categoría no existe.");
                }

                if (resultado == 2)
                {
                    return BadRequest("La categoría ya está inactiva.");
                }

                return Ok("La categoría se desactivó correctamente.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

    }
}

using b2b_e_commerce_bll;
using b2b_e_commerce_models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace b2b_e_commerce_api.Controladores
{
    [ApiController]
    [Route("v1/categorias")]
    [Produces("application/json")]
    public class ControladorDeCategorias : Controller
    {
        private readonly IServicioDeCategorias _servicioDeCategorias;
        private readonly IServicioDeUsuarios _servicioDeUsuarios;

        public ControladorDeCategorias(IServicioDeCategorias servicioDeCategorias, IServicioDeUsuarios servicioDeUsuarios)
        {
            _servicioDeCategorias = servicioDeCategorias ?? throw new ArgumentNullException(nameof(servicioDeCategorias));
            _servicioDeUsuarios = servicioDeUsuarios ?? throw new ArgumentNullException(nameof(servicioDeUsuarios));
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
        [Route("InsertarCategoria")]
        [Authorize(Roles = Constantes.ADMINISTRADOR)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> InsertarCategoria([FromBody] SolicitudDeCategoria solicitud)
        {
            if (solicitud == null)
            {
                return BadRequest("La categoria es requerida");
            }
            if (string.IsNullOrWhiteSpace(solicitud.Nombre))
            {
                return BadRequest("El nombre de la categoria es requerido");
            }

            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return Unauthorized("No se pudo identificar al usuario autenticado.");
            }
            var usuario = await _servicioDeUsuarios.ObtenerUsuarioPorId(usuarioId);

            if (usuario == null)
            {
                return Unauthorized("No se encontró el usuario autenticado.");
            }

            var fechaActual = DateTime.UtcNow;

            var categoria = new Categoria
            {
                Nombre = solicitud.Nombre,
                Descripcion = solicitud.Descripcion,
                Activo = true,
                CreadoPor = new Colaborador {
                    Id = usuario.Id,
                    Nombre = usuario.Nombre,
                    Rol = usuario.Rol
                },
                FechaCreacion = fechaActual,
                FechaModificacion = fechaActual
            };

            var res = await _servicioDeCategorias.InsertarCategoria(categoria);

            return StatusCode((int)HttpStatusCode.Created, res);
        }

        [HttpPut]
        [Route("ModificarCategoria/{categoriaId}")]
        [Authorize(Roles = Constantes.ADMINISTRADOR)]
        [ProducesResponseType(typeof(Categoria), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ModificarCategoria(string categoriaId, [FromBody] SolicitudDeCategoria solicitud) 
        {
            if(string.IsNullOrWhiteSpace(categoriaId))
            {
                return BadRequest("El ID de la categoria es requerido.");
            }

            if(!ObjectId.TryParse(categoriaId, out var id))
            {
                return BadRequest("El ID de la categoria no se encuentra.");
            }

            if(solicitud == null)
            {
                return BadRequest("La categoria es requerida.");
            }

            if(string.IsNullOrWhiteSpace(solicitud.Nombre))
            {
                return BadRequest("El nombre de la categoria es requerido.");
            }

            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if(string.IsNullOrWhiteSpace(usuarioId))
            {
                return Unauthorized("No se pudo identificar al usuario autenticado.");
            }

            var usuario = await _servicioDeUsuarios.ObtenerUsuarioPorId(usuarioId);

            if(usuario == null || !usuario.Activo)
            {
                return Unauthorized("No se encontró un usuario activo.");
            }

            var fechaActual = DateTime.UtcNow;

            var categoria = new Categoria
            {
                Nombre = solicitud.Nombre,
                Descripcion = solicitud.Descripcion,
                ModificadoPor = new Colaborador
                {
                    Id = usuario.Id,
                    Nombre = usuario.Nombre,
                    Rol = usuario.Rol
                },
                FechaModificacion = fechaActual
            };

            var resultado = await _servicioDeCategorias.ModificarCategoria(categoriaId, categoria);

            if(resultado == null)
            {
                return NotFound("La categoria no existe o ya esta inactiva.");
            }

            return Ok(resultado);
        }

        [HttpDelete]
        [Route("EliminarCategoria/{categoriaId}")]
        [Authorize(Roles = Constantes.ADMINISTRADOR)]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> EliminarCategoria(string categoriaId)
        {
            if (string.IsNullOrWhiteSpace(categoriaId))
            {
                return BadRequest("El ID de la Categoria es requerida.");
            }

            if (!ObjectId.TryParse(categoriaId, out var id))
            {
                return BadRequest("El ID de la categoria no se encuentra o ya esta eliminado.");
            }

            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return Unauthorized("No se pudo identificar al usuario autenticado.");
            }

            var usuario = await _servicioDeUsuarios.ObtenerUsuarioPorId(usuarioId);

            if (usuario == null || !usuario.Activo)
            {
                return Unauthorized("No se encontró un usuario activo.");
            }

            var modificador = new Colaborador
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Rol = usuario.Rol
            };

            var resultado = await _servicioDeCategorias.EliminarCategoria(categoriaId,modificador);

            if(resultado == 0)
            {
                return NotFound("La categoria no existe.");
            }
            if (resultado == 2)
            {
                return BadRequest("La categoria ya está inactiva.");
            }

            return Ok("La categoria se desactivo correctamente.");
        }
    }
}

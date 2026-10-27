using b2b_e_commerce_bll;
using b2b_e_commerce_models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Net;


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


    }
}

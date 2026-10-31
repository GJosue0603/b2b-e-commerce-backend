using b2b_e_commerce_bll;
using b2b_e_commerce_models;
using b2b_e_commerce_utilities;
using b2b_e_commerce_utilities.Excepciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace b2b_e_commerce_api.Controladores
{
    [ApiController]
    [Route("v1/autenticacion")]
    [Authorize]
    [Produces("application/json")]
    public class ControladorDeAutenticacion : Controller
    {
        private readonly IServicioDeAutenticacion _servicioDeAutenticacion;

        public ControladorDeAutenticacion(IServicioDeAutenticacion servicioDeAutenticacion)
        {
            _servicioDeAutenticacion = servicioDeAutenticacion ?? throw new ArgumentNullException(nameof(servicioDeAutenticacion));
        }

        [HttpPost]
        [Route("registrar")]
        [AllowAnonymous]
        // [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> Registrar([FromBody] RegistroDeUsuario registroDeUsuario)
        {
            if (registroDeUsuario == null)
            {
                return BadRequest("El usuario a registrar no puede estar nulo.");
            }

            if (string.IsNullOrWhiteSpace(registroDeUsuario.Nombre) ||
                string.IsNullOrWhiteSpace(registroDeUsuario.Apellido1) ||
                string.IsNullOrWhiteSpace(registroDeUsuario.Correo) ||
                string.IsNullOrWhiteSpace(registroDeUsuario.Contrasena) ||
                string.IsNullOrWhiteSpace(registroDeUsuario.Telefono))
            {
                return BadRequest("El Nombre, Apellido1, Correo, Contraseña y Teléfono son requeridos para el registro del usuario.");
            }

            if (!UtilidadesCorreo.EsCorreoValido(registroDeUsuario.Correo))
            {
                return BadRequest("El formato del correo electrónico no es válido.");
            }

            if (!UtilidadesTelefono.EsNumeroValido(registroDeUsuario.Telefono))
            {
                return BadRequest("El formato del número de teléfono no es válido.");
            }

            if (!UtilidadesContrasena.EsContrasenaValida(registroDeUsuario.Contrasena))
            {
                return BadRequest("La contraseña debe contener al menos 8 caractéres, una letra, una mayúscula y un número.");
            }

            try
            {
                var res = await _servicioDeAutenticacion.RegistrarUsuario(registroDeUsuario);

                return Created("Registro Pendiente Creado", "El registro se ha procesado, pronto recibirás un código de confirmación.");
            }
            catch (CorreoDuplicadoException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPost]
        [Route("login")]
        [AllowAnonymous]
        // [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Login([FromBody] LoginDeUsuario loginDeUsuario)
        {
            if (string.IsNullOrWhiteSpace(loginDeUsuario.Correo))
            {
                return BadRequest("El Correo es requerido.");
            }

            if (string.IsNullOrWhiteSpace(loginDeUsuario.Contrasena))
            {
                return BadRequest("La Contraseña es requerida.");
            }

            if (!UtilidadesCorreo.EsCorreoValido(loginDeUsuario.Correo))
            {
                return BadRequest("El formato del correo electrónico no es válido.");
            }

            try
            {
                var token = await _servicioDeAutenticacion.Login(loginDeUsuario);
                return Ok(token);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost]
        [Route("verificar-correo")]
        [AllowAnonymous]
        // [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(typeof(RespuestaDeUsuario), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> VerificarCorreo([FromBody] VerificacionDeCorreo verificacionDeCorreo)
        {
            // Validar la información del usuario
            if (string.IsNullOrWhiteSpace(verificacionDeCorreo.Correo))
            {
                return BadRequest("El Correo es requerido.");
            }

            if (string.IsNullOrWhiteSpace(verificacionDeCorreo.Codigo))
            {
                return BadRequest("El Código es requerido.");
            }

            if (!UtilidadesCorreo.EsCorreoValido(verificacionDeCorreo.Correo))
            {
                return BadRequest("El formato del correo electrónico no es válido.");
            }

            if (verificacionDeCorreo.Codigo.Trim().Length != 6)
            {
                return BadRequest("El Código debe contener 6 digitos.");
            }

            if (!verificacionDeCorreo.Codigo.Trim().All(char.IsDigit))
            {
                return BadRequest("El Código solo debe contener caractéres numéricos.");
            }

            // Verificar el correo y crear el usuario en la base de datos
            try
            {
                var usuario = await _servicioDeAutenticacion.VerificarCorreo(verificacionDeCorreo);
                return Ok(usuario);
            }
            catch (RegistroPendienteNoEncontradoException ex)
            {
                return NotFound(ex.Message);
            }
            catch (CorreoDuplicadoException ex)
            {
                return Conflict(ex.Message);
            }
            catch (CodigoVerificacionExpiradoException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (CodigoVerificacionInvalidoException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (LimiteIntentosExcedidoException ex)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, ex.Message);
            }
            catch (VerificacionRegistroException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
using b2b_e_commerce_bll.JWT;
using b2b_e_commerce_bll.Mensajeria;
using b2b_e_commerce_models;
using b2b_e_commerce_repository;
using b2b_e_commerce_utilities;
using b2b_e_commerce_utilities.Excepciones;
using System.Security.Cryptography;

namespace b2b_e_commerce_bll
{
    public interface IServicioDeAutenticacion
    {
        Task<RegistroPendiente> RegistrarUsuario(RegistroDeUsuario registroDeUsuario);
        Task<string> Login(LoginDeUsuario loginDeUsuario);
    }

    public class ServicioDeAutenticacion : IServicioDeAutenticacion
    {
        private readonly IRepositorioDeUsuarios _repositorioDeUsuarios;
        private readonly IRepositorioDeRegistrosPendientes _repositorioDeRegistrosPendientes;
        private readonly IProveedorDeTokens _proveedorDeTokens;
        private readonly IServicioDeColaCorreos _servicioDeColaCorreos;

        public ServicioDeAutenticacion(IRepositorioDeUsuarios repositorioDeUsuarios,
            IRepositorioDeRegistrosPendientes repositorioDeRegistrosPendientes,
            IProveedorDeTokens proveedorDeTokens,
            IServicioDeColaCorreos servicioDeColaCorreos)
        {
            _repositorioDeUsuarios = repositorioDeUsuarios ?? throw new ArgumentNullException(nameof(repositorioDeUsuarios));
            _repositorioDeRegistrosPendientes = repositorioDeRegistrosPendientes ?? throw new ArgumentNullException(nameof(repositorioDeRegistrosPendientes));
            _proveedorDeTokens = proveedorDeTokens ?? throw new ArgumentNullException(nameof(proveedorDeTokens));
            _servicioDeColaCorreos = servicioDeColaCorreos ?? throw new ArgumentNullException(nameof(servicioDeColaCorreos));
        }

        public async Task<RegistroPendiente> RegistrarUsuario(RegistroDeUsuario registroDeUsuario)
        {
            // Normalizar correo
            var correoNormalizado = UtilidadesCorreo.Normalizar(registroDeUsuario.Correo);
            var fechaYHora = DateTime.UtcNow;

            // Verificar que no exista un usuario con este correo la base de datos
            var usuarioPorCorreo = await _repositorioDeUsuarios.ObtenerUsuarioPorCorreo(correoNormalizado);
            if (usuarioPorCorreo != null)
            {
                throw new CorreoDuplicadoException();
            }

            // Verificar que no exista un registro pendiente activo en la base de datos
            var registroPendientePorCorreo = await _repositorioDeRegistrosPendientes.ObtenerRegistroPendientePorCorreo(correoNormalizado);
            if (registroPendientePorCorreo != null && registroPendientePorCorreo.FechaExpiracion > fechaYHora)
            {
                throw new CorreoDuplicadoException("Ya existe una solicitud de registro pendiente activa para este correo electrónico.");
            }

            // Crear el modelo de registro pendiente
            var contrasenaHash = UtilidadesContrasena.Hashear(registroDeUsuario.Contrasena);
            var codigo = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var codigoHash = UtilidadesContrasena.Hashear(codigo);
            var registroPendiente = new RegistroPendiente
            {
                Nombre = registroDeUsuario.Nombre,
                Apellido1 = registroDeUsuario.Apellido1,
                Apellido2 = registroDeUsuario.Apellido2,
                Correo = correoNormalizado,
                ContrasenaHash = contrasenaHash,
                Telefono = UtilidadesTelefono.FormatearParaBaseDeDatos(registroDeUsuario.Telefono),
                CodigoHash = codigoHash,
                CodigoExpiracion = fechaYHora.AddMinutes(10),
                FechaExpiracion = fechaYHora.AddHours(24),
                IntentosFallidos = 0
            };

            // Guardar el usuario en base de datos
            var res = await _repositorioDeRegistrosPendientes.RegistrarUsuario(registroPendiente, fechaYHora);

            // Crear el mensaje de verificación
            var mensajeCorreo = new MensajeDeCorreo
            {
                Destinatario = correoNormalizado,
                Asunto = "Código de verificación - B2B E-Commerce",
                Plantilla = TipoPlantillaCorreo.VerificacionRegistro,
                DatosPlantilla = new Dictionary<string, string>
                {
                    { "Nombre", registroDeUsuario.Nombre },
                    { "Codigo", codigo }
                }
            };

            // Publicar el mensaje en SQS
            await _servicioDeColaCorreos.PublicarCorreoAsync(mensajeCorreo);

            return res;
        }

        public async Task<string> Login(LoginDeUsuario loginDeUsuario)
        {
            // Normalizar correo
            var correoNormalizado = UtilidadesCorreo.Normalizar(loginDeUsuario.Correo);
            var usuario = await _repositorioDeUsuarios.ObtenerUsuarioPorCorreo(correoNormalizado);

            if (usuario == null || !usuario.Activo || !UtilidadesContrasena.Verificar(loginDeUsuario.Contrasena, usuario.ContrasenaHash))
            {
                throw new UnauthorizedAccessException("Correo o contraseña incorrectos");
            }

            var token = _proveedorDeTokens.CrearToken(usuario);

            return token;
        }
    }
}
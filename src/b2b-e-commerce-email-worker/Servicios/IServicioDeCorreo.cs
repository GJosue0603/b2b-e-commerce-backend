using b2b_e_commerce_models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace b2b_e_commerce_email_worker.Servicios
{
    public interface IServicioDeCorreo
    {
        Task EnviarCorreoAsync(MensajeDeCorreo mensaje, CancellationToken cancellationToken);
    }

    public class ServicioDeCorreo : IServicioDeCorreo
    {
        private readonly IConfiguration _config;
        private readonly IServicioDePlantillas _servicioDePlantillas;

        public ServicioDeCorreo(IConfiguration config, IServicioDePlantillas servicioDePlantillas)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _servicioDePlantillas = servicioDePlantillas ?? throw new ArgumentNullException(nameof(servicioDePlantillas));
        }

        public async Task EnviarCorreoAsync(MensajeDeCorreo mensaje, CancellationToken cancellationToken)
        {
            var correo = new MimeMessage();

            correo.From.Add(new MailboxAddress(_config["SMTP:SenderName"], _config["SMTP:SenderEmail"]));
            correo.To.Add(MailboxAddress.Parse(mensaje.Destinatario));
            correo.Subject = mensaje.Asunto;
            if (mensaje.Plantilla.HasValue)
            {
                var html = await _servicioDePlantillas.RenderizarAsync(mensaje.Plantilla.Value, mensaje.DatosPlantilla, cancellationToken);
                correo.Body = new TextPart("html")
                {
                    Text = html
                };
            }
            else
            {
                correo.Body = new TextPart("plain")
                {
                    Text = mensaje.Cuerpo
                };
            }

            var host = _config["SMTP:Host"] ?? throw new InvalidOperationException("No se configuró el servidor SMTP.");
            var port = _config.GetValue<int>("SMTP:Port");
            var security = _config["SMTP:Security"] switch
            {
                "StartTls" => SecureSocketOptions.StartTls,
                "SslOnConnect" => SecureSocketOptions.SslOnConnect,
                "None" => SecureSocketOptions.None,
                _ => throw new InvalidOperationException(
                    "La configuración de seguridad SMTP no es válida.")
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(host, port, security, cancellationToken);

            var usuario = _config["SMTP:Username"];
            var contrasena = _config["SMTP:Password"];

            if (!string.IsNullOrWhiteSpace(usuario) && !string.IsNullOrWhiteSpace(contrasena))
            {
                await smtp.AuthenticateAsync(usuario, contrasena, cancellationToken);
            }

            await smtp.SendAsync(correo, cancellationToken);
            await smtp.DisconnectAsync(true, cancellationToken);
        }
    }
}
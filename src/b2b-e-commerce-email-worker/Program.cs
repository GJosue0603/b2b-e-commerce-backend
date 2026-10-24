using Amazon.Runtime;
using Amazon.SQS;
using b2b_e_commerce_email_worker;
using b2b_e_commerce_email_worker.Servicios;

var builder = Host.CreateApplicationBuilder(args);

// Cargar la configuración YAML y las variables de entorno.
builder.Configuration
    .AddYamlFile("appSettings.yaml.token", optional: true, reloadOnChange: true)
    .AddYamlFile("appSettings.yaml", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var configuracion = builder.Configuration;

// Verificar los valores obligatorios antes de iniciar el Worker.
ValidarConfiguracion(configuracion);

builder.Services.AddSingleton<IAmazonSQS>(_ =>
{
    var configuracionSqs = new AmazonSQSConfig
    {
        ServiceURL = configuracion["AWS:ServiceURL"],
        AuthenticationRegion = configuracion["AWS:Region"]
    };

    // Credenciales de prueba exclusivas del entorno local con LocalStack.
    return new AmazonSQSClient(new BasicAWSCredentials("test", "test"), configuracionSqs);
});

builder.Services.AddSingleton<IServicioDeCorreo, ServicioDeCorreo>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();

static void ValidarConfiguracion(IConfiguration configuracion)
{
    string[] camposObligatorios =
    [
        "AWS:ServiceURL",
        "AWS:Region",
        "AWS:QueueName",
        "SMTP:Host",
        "SMTP:SenderEmail",
        "SMTP:SenderName",
        "SMTP:Security"
    ];

    foreach (var campo in camposObligatorios)
    {
        if (string.IsNullOrWhiteSpace(configuracion[campo]))
        {
            throw new InvalidOperationException($"Falta la configuración obligatoria: {campo}.");
        }
    }

    if (!Uri.TryCreate(configuracion["AWS:ServiceURL"], UriKind.Absolute, out var urlSqs) || (urlSqs.Scheme != Uri.UriSchemeHttp && urlSqs.Scheme != Uri.UriSchemeHttps))
    {
        throw new InvalidOperationException("AWS:ServiceURL debe ser una URL HTTP o HTTPS válida.");
    }

    if (!int.TryParse(configuracion["SMTP:Port"], out var puertoSmtp) || puertoSmtp is < 1 or > 65535)
    {
        throw new InvalidOperationException("SMTP:Port debe ser un puerto válido entre 1 y 65535.");
    }

    string[] opcionesSeguridad = ["None", "StartTls", "SslOnConnect"];
    if (!opcionesSeguridad.Contains(configuracion["SMTP:Security"]))
    {
        throw new InvalidOperationException("SMTP:Security debe ser None, StartTls o SslOnConnect.");
    }

    var usuarioSmtp = configuracion["SMTP:Username"];
    var contrasenaSmtp = configuracion["SMTP:Password"];
    if (string.IsNullOrWhiteSpace(usuarioSmtp) != string.IsNullOrWhiteSpace(contrasenaSmtp))
    {
        throw new InvalidOperationException("SMTP:Username y SMTP:Password deben configurarse juntos.");
    }
}
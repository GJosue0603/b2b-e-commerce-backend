using b2b_e_commerce_email_worker;
using Amazon.Runtime;
using Amazon.SQS;
using b2b_e_commerce_email_worker.Servicios;

var builder = Host.CreateApplicationBuilder(args);

// Load YAML Configuration
builder.Configuration
    .AddYamlFile("appSettings.yaml.token", optional: true, reloadOnChange: true)
    .AddYamlFile("appSettings.yaml", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddSingleton<IAmazonSQS>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();

    var serviceUrl = configuration["AWS:ServiceURL"];

    var sqsConfig = new AmazonSQSConfig
    {
        ServiceURL = serviceUrl,
        AuthenticationRegion = configuration["AWS:Region"]
    };

    return new AmazonSQSClient(
        new BasicAWSCredentials("test", "test"),
        sqsConfig
    );
});

builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<IServicioDeCorreo, ServicioDeCorreo>();

var host = builder.Build();
host.Run();
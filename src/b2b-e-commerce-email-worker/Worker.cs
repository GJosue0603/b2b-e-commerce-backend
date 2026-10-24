using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using b2b_e_commerce_email_worker.Servicios;
using b2b_e_commerce_models;

namespace b2b_e_commerce_email_worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IAmazonSQS _sqs;
        private readonly IConfiguration _configuration;
        private readonly IServicioDeCorreo _servicioDeCorreo;
        private static readonly TimeSpan _tiempoDeReintento = TimeSpan.FromSeconds(5);

        public Worker(
            ILogger<Worker> logger,
            IAmazonSQS sqs,
            IConfiguration configuration,
            IServicioDeCorreo servicioDeCorreo)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sqs = sqs ?? throw new ArgumentNullException(nameof(sqs));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _servicioDeCorreo = servicioDeCorreo ?? throw new ArgumentNullException(nameof(servicioDeCorreo));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string? urlCola = null;
            var nombreCola = _configuration["AWS:QueueName"];
            if (string.IsNullOrWhiteSpace(nombreCola))
            {
                throw new InvalidOperationException("No se configuró el nombre de la cola SQS.");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Se inicia el ciclo de búsqueda de mensajes...");

                    if (urlCola is null)
                    {
                        var respuestaCola = await _sqs.GetQueueUrlAsync(new GetQueueUrlRequest { QueueName = nombreCola }, stoppingToken);
                        urlCola = respuestaCola.QueueUrl;
                        _logger.LogInformation("Worker de correos conectado a la cola: {NombreCola}", nombreCola);
                    }

                    var respuesta = await _sqs.ReceiveMessageAsync(
                        new ReceiveMessageRequest
                        {
                            QueueUrl = urlCola,
                            MaxNumberOfMessages = 1,
                            WaitTimeSeconds = 10
                        },
                        stoppingToken
                    );

                    foreach (var mensaje in respuesta.Messages ?? [])
                    {
                        _logger.LogInformation("Mensaje recibido: {MessageId}", mensaje.MessageId);

                        await ProcesarMensajeAsync(mensaje, stoppingToken);

                        await _sqs.DeleteMessageAsync(
                            new DeleteMessageRequest
                            {
                                QueueUrl = urlCola,
                                ReceiptHandle = mensaje.ReceiptHandle
                            },
                            stoppingToken
                        );

                        _logger.LogInformation("Mensaje procesado y eliminado correctamente: {MessageId}", mensaje.MessageId);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (QueueDoesNotExistException)
                {
                    urlCola = null;
                    _logger.LogWarning("La cola SQS {NombreCola} no está disponible. Se intentará nuevamente.", nombreCola);

                    if (!await EsperarReintentoAsync(stoppingToken)) break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ocurrió un error durante la comunicación o el procesamiento de mensajes SQS.");

                    if (!await EsperarReintentoAsync(stoppingToken)) break;
                }
            }
        }

        private static async Task<bool> EsperarReintentoAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(_tiempoDeReintento, cancellationToken);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
        }

        private async Task ProcesarMensajeAsync(Message mensaje, CancellationToken cancellationToken)
        {
            var correo = JsonSerializer.Deserialize<MensajeDeCorreo>(mensaje.Body);

            // Validar mensaje
            if (correo is null)
            {
                throw new JsonException("No se pudo deserializar el mensaje de correo.");
            }

            if (string.IsNullOrWhiteSpace(correo.Destinatario) ||
                string.IsNullOrWhiteSpace(correo.Asunto) ||
                string.IsNullOrWhiteSpace(correo.Cuerpo))
            {
                throw new InvalidOperationException("El mensaje de correo contiene campos obligatorios vacíos.");
            }

            _logger.LogInformation("Mensaje de correo validado correctamente: {MessageId}", mensaje.MessageId);

            await _servicioDeCorreo.EnviarCorreoAsync(correo, cancellationToken);
        }
    }
}
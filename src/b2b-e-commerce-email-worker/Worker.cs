namespace b2b_e_commerce_email_worker
{
    using System.Text.Json;
    using Amazon.SQS;
    using Amazon.SQS.Model;
    using b2b_e_commerce_email_worker.Servicios;
    using b2b_e_commerce_models;

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IAmazonSQS _sqs;
        private readonly IConfiguration _configuration;
        private readonly IServicioDeCorreo _servicioDeCorreo;

        public Worker(ILogger<Worker> logger, IAmazonSQS sqs, IConfiguration config, IServicioDeCorreo servicioDeCorreo)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sqs = sqs ?? throw new ArgumentNullException(nameof(sqs));
            _configuration = config ?? throw new ArgumentNullException(nameof(config));
            _servicioDeCorreo = servicioDeCorreo ?? throw new ArgumentNullException(nameof(servicioDeCorreo));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queueName = _configuration["AWS:QueueName"];

            var queueResponse = await _sqs.GetQueueUrlAsync(
                new GetQueueUrlRequest
                {
                    QueueName = queueName
                },
                stoppingToken
            );

            var queueUrl = queueResponse.QueueUrl;

            _logger.LogInformation("Email Worker listening to queue: {QueueName}", queueName);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var response = await _sqs.ReceiveMessageAsync(
                        new ReceiveMessageRequest
                        {
                            QueueUrl = queueUrl,
                            MaxNumberOfMessages = 1,
                            WaitTimeSeconds = 10
                        },
                        stoppingToken
                    );

                    foreach (var message in response.Messages)
                    {
                        _logger.LogInformation("Message received: {MessageId}", message.MessageId);

                        // Procesar el mensaje
                        await ProcesarMensajeAsync(message, stoppingToken);

                        // Eliminar el mensaje de la cola después de procesarlo
                        await _sqs.DeleteMessageAsync(
                            new DeleteMessageRequest
                            {
                                QueueUrl = queueUrl,
                                ReceiptHandle = message.ReceiptHandle
                            },
                            stoppingToken
                        );

                        _logger.LogInformation("Message processed and deleted: {MessageId}", message.MessageId);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing SQS messages");

                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }

        private async Task ProcesarMensajeAsync(Message mensaje, CancellationToken cancellationToken)
        {
            var correo = JsonSerializer.Deserialize<MensajeDeCorreo>(mensaje.Body);

            if (correo == null)
            {
                throw new JsonException("No se pudo deserializar el mensaje de correo.");
            }

            if (string.IsNullOrWhiteSpace(correo.Destinatario) ||
                string.IsNullOrWhiteSpace(correo.Asunto) ||
                string.IsNullOrWhiteSpace(correo.Cuerpo))
            {
                throw new InvalidOperationException("El mensaje de correo contiene campos obligatorios vacíos.");
            }

            _logger.LogInformation("Mensaje de correo validado correctamente. MessageId: {MessageId}", mensaje.MessageId);

            await _servicioDeCorreo.EnviarCorreoAsync(correo, cancellationToken);
        }
    }
}
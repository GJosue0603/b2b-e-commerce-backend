using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using b2b_e_commerce_models;
using Microsoft.Extensions.Configuration;

namespace b2b_e_commerce_bll.Mensajeria
{
    public interface IServicioDeColaCorreos
    {
        Task PublicarCorreoAsync(MensajeDeCorreo mensaje, CancellationToken cancellationToken = default);
    }

    public class ServicioDeColaCorreos : IServicioDeColaCorreos
    {
        private readonly IAmazonSQS _sqs;
        private readonly IConfiguration _configuration;

        public ServicioDeColaCorreos(
            IAmazonSQS sqs,
            IConfiguration config)
        {
            _sqs = sqs ?? throw new ArgumentNullException(nameof(sqs));
            _configuration = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task PublicarCorreoAsync(MensajeDeCorreo mensaje, CancellationToken cancellationToken = default)
        {
            var nombreCola = _configuration["AWS:QueueName"];

            if (string.IsNullOrWhiteSpace(nombreCola))
            {
                throw new InvalidOperationException("No se configuró el nombre de la cola SQS.");
            }

            var respuestaCola = await _sqs.GetQueueUrlAsync(
                new GetQueueUrlRequest
                {
                    QueueName = nombreCola
                },
                cancellationToken
            );

            var mensajeJson = JsonSerializer.Serialize(mensaje);
            await _sqs.SendMessageAsync(
                new SendMessageRequest
                {
                    QueueUrl = respuestaCola.QueueUrl,
                    MessageBody = mensajeJson
                },
                cancellationToken
            );
        }
    }
}
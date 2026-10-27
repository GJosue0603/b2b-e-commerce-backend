using System.Net;
using System.Text.RegularExpressions;
using b2b_e_commerce_models.Enum;
using b2b_e_commerce_utilities;

namespace b2b_e_commerce_email_worker.Servicios
{
    public interface IServicioDePlantillas
    {
        Task<string> RenderizarAsync(TipoPlantillaCorreo plantilla, Dictionary<string, string> datos, CancellationToken cancellationToken = default);
    }

    public class ServicioDePlantillas : IServicioDePlantillas
    {
        private readonly string _directorioPlantillas;

        public ServicioDePlantillas(IHostEnvironment entorno)
        {
            _directorioPlantillas = Path.Combine(entorno.ContentRootPath, "Plantillas");
        }

        public async Task<string> RenderizarAsync(TipoPlantillaCorreo plantilla, Dictionary<string, string> datos, CancellationToken cancellationToken = default)
        {
            var nombreArchivo = plantilla switch
            {
                TipoPlantillaCorreo.VerificacionRegistro => UtilidadesEnums.ObtenerDescripcionEnBaseAlValor(TipoPlantillaCorreo.VerificacionRegistro),
                _ => throw new InvalidOperationException($"La plantilla '{plantilla}' no está permitida.")
            };

            var rutaPlantilla = Path.Combine(_directorioPlantillas, nombreArchivo);
            var html = await File.ReadAllTextAsync(rutaPlantilla, cancellationToken);

            foreach (var dato in datos)
            {
                var marcador = "{{" + dato.Key + "}}";
                var valorSeguro = WebUtility.HtmlEncode(dato.Value);

                html = html.Replace(marcador, valorSeguro, StringComparison.Ordinal);
            }

            if (Regex.IsMatch(html, @"\{\{[A-Za-z][A-Za-z0-9]*\}\}"))
            {
                throw new InvalidOperationException("La plantilla contiene campos sin completar.");
            }

            return html;
        }
    }
}
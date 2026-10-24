using b2b_e_commerce_models.Enum;

namespace b2b_e_commerce_models
{
    public class MensajeDeCorreo
    {
        public string Destinatario { get; set; } = string.Empty;
        public string Asunto { get; set; } = string.Empty;
        public string Cuerpo { get; set; } = string.Empty;
        public TipoPlantillaCorreo? Plantilla { get; set; }
        public Dictionary<string, string> DatosPlantilla { get; set; } = new();
    }
}
using b2b_e_commerce_bll;
using b2b_e_commerce_bll.JWT;
using b2b_e_commerce_models;
using b2b_e_commerce_repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Text;

namespace b2b_e_commerce_api
{
    public static class ConfiguracionDeServicios
    {
        public static IServiceCollection ConfigurarServicios(this IServiceCollection servicios, IConfiguration configuracion)
        {
            // Servicios de JWT
            var secreto = configuracion["Jwt:Secreto"];
            var emisor = configuracion["Jwt:Emisor"];
            var audiencia = configuracion["Jwt:Audiencia"];
            var expiracion = configuracion.GetValue<int>("Jwt:ExpiracionEnMinutos");

            if (string.IsNullOrWhiteSpace(secreto) || secreto.StartsWith("__") || Encoding.UTF8.GetByteCount(secreto) < 32)
            {
                throw new InvalidOperationException("Jwt:Secreto debe contener una clave válida de al menos 32 bytes.");
            }

            if (string.IsNullOrWhiteSpace(emisor) || string.IsNullOrWhiteSpace(audiencia) || emisor.StartsWith("__") || audiencia.StartsWith("__"))
            {
                throw new InvalidOperationException("Jwt:Emisor y Jwt:Audiencia deben estar configurados.");
            }

            if (expiracion <= 0)
            {
                throw new InvalidOperationException("Jwt:ExpiracionEnMinutos debe ser mayor que cero.");
            }

            servicios.AddAuthorization();
            servicios.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = emisor,
                        ValidAudience = audiencia,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secreto)),
                        ClockSkew = TimeSpan.Zero,
                        RoleClaimType = Constantes.ROL_CLAIM
                    };
            });
            servicios.AddSingleton<IProveedorDeTokens, ProveedorDeTokens>();

            // Servicios
            servicios.AddScoped<IServicioDeUsuarios, ServicioDeUsuarios>();
            servicios.AddScoped<IServicioDeAutenticacion, ServicioDeAutenticacion>();
            servicios.AddScoped<IServicioDeProductos, ServicioDeProductos>();

            // Repositorios
            var mongoConnectionString = configuracion["Mongo:ConnectionString"];
            if (string.IsNullOrWhiteSpace(mongoConnectionString) || mongoConnectionString.StartsWith("__"))
            {
                throw new InvalidOperationException("Mongo:ConnectionString no está configurada.");
            }

            servicios.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
            servicios.AddScoped<IRepositorioDeUsuarios, RepositorioDeUsuarios>();
            servicios.AddScoped<IRepositorioDeRegistrosPendientes, RepositorioDeRegistrosPendientes>();
            servicios.AddScoped<IRepositorioDeProductos, RepositorioDeProductos>();

            return servicios;
        }
    }
}
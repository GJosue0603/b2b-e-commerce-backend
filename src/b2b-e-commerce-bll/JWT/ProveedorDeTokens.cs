using b2b_e_commerce_models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JsobWebToken = Microsoft.IdentityModel.JsonWebTokens;

namespace b2b_e_commerce_bll.JWT
{
    public interface IProveedorDeTokens
    {
        string CrearToken(Usuario usuario);
    }

    public class ProveedorDeTokens(IConfiguration configuracion) : IProveedorDeTokens
    {
        public string CrearToken(Usuario usuario)
        {
            string llaveSecreta = configuracion["Jwt:Secreto"];
            var llaveDeSeguridad = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(llaveSecreta));

            var credenciales = new SigningCredentials(llaveDeSeguridad, SecurityAlgorithms.HmacSha256);

            var descripcionDelToken = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([
                    new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, usuario.Correo),
                    new Claim(Constantes.ACTIVO_CLAIM, usuario.Activo.ToString()),
                    new Claim(Constantes.ROL_CLAIM, usuario.Rol.ToString())
                ]),
                Expires = DateTime.UtcNow.AddMinutes(configuracion.GetValue<int>("Jwt:ExpiracionEnMinutos")),
                SigningCredentials = credenciales,
                Issuer = configuracion["Jwt:Emisor"],
                Audience = configuracion["Jwt:Audiencia"]
            };

            var manejadorDeTokens = new JsobWebToken.JsonWebTokenHandler();
            var token = manejadorDeTokens.CreateToken(descripcionDelToken);

            return token;
        }
    }
}
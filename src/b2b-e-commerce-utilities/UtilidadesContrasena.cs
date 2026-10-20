using System.Security.Cryptography;

namespace b2b_e_commerce_utilities
{
    public static class UtilidadesContrasena
    {
        private const int Iteraciones = 600_000;
        private const int TamanoSalt = 16;
        private const int TamanoHash = 32;

        // Hashear una contraseña y generar un salt aleatorio
        public static string Hashear(string contrasena)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(TamanoSalt);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                contrasena,
                salt,
                Iteraciones,
                HashAlgorithmName.SHA256,
                TamanoHash
            );

            // Guardar iteraciones, salt y hash en una sola cadena
            return $"{Iteraciones}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        // Verificar una contraseña contra el hash almacenado
        public static bool Verificar(
            string contrasena,
            string hashAlmacenado)
        {
            if (string.IsNullOrWhiteSpace(contrasena) ||
                string.IsNullOrWhiteSpace(hashAlmacenado))
            {
                return false;
            }

            string[] partes = hashAlmacenado.Split('.');

            if (partes.Length != 3 ||
                !int.TryParse(partes[0], out int iteraciones) ||
                iteraciones < 1 ||
                iteraciones > 1_000_000)
            {
                return false;
            }

            try
            {
                byte[] salt = Convert.FromBase64String(partes[1]);
                byte[] hashOriginal = Convert.FromBase64String(partes[2]);

                if (salt.Length != TamanoSalt ||
                    hashOriginal.Length != TamanoHash)
                {
                    return false;
                }

                byte[] hashIngresado = Rfc2898DeriveBytes.Pbkdf2(
                    contrasena,
                    salt,
                    iteraciones,
                    HashAlgorithmName.SHA256,
                    hashOriginal.Length
                );

                return CryptographicOperations.FixedTimeEquals(
                    hashOriginal,
                    hashIngresado
                );
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static bool EsContrasenaValida(string contrasena)
        {
            var mayusculas = contrasena.Where(char.IsUpper).ToList();
            var letras = contrasena.Where(char.IsLetter).ToList();
            var numeros = contrasena.Where(char.IsDigit).ToList();

            // La contraseña debe contener al menos 8 caractéres
            if (contrasena.Length < 8)
            {
                return false;
            }

            // La contraseña debe contener al menos una letra
            if (letras.Count < 1)
            {
                return false;
            }

            // La contraseña debe contener al menos una mayúscula
            if (mayusculas.Count < 1)
            {
                return false;
            }

            // La contraseña debe contener al menos un número
            if (numeros.Count < 1)
            {
                return false;
            }

            return true;
        }
    }
}
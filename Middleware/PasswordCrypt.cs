using System.Security.Cryptography;
using System.Text;

namespace EcommerceAPI.Middleware
{
    public static class PasswordCrypt
    {
        public static void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = Convert.ToBase64String(hmac.Key);
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                passwordHash = Convert.ToBase64String(hmac.ComputeHash(passwordBytes));
            }
        }
        
        public static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            using (var hmac = new HMACSHA512(storedSalt))
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] computedHash = hmac.ComputeHash(passwordBytes);
                bool hashesMatch = computedHash.SequenceEqual(storedHash);
                return hashesMatch;
            }
        }
    }
}

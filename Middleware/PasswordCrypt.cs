using System.Security.Cryptography;
using System.Text;

namespace EcommerceAPI.Middleware
{
    public static class PasswordCrypt
    {
        public static class SecurityConfig
        {
            // Static, hardcoded salt (for demo only)
            public static readonly byte[] PasswordSalt = 
                Convert.FromBase64String("hS4WfZmN+3q4nUo9rQ5cBA==");
        }
        public static void CreatePasswordHash(string password, out string passwordHash, out string? passwordSalt)
        {
            using (var hmac = new HMACSHA512(SecurityConfig.PasswordSalt))
            {
                passwordSalt = Convert.ToBase64String(hmac.Key);
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                passwordHash = Convert.ToBase64String(hmac.ComputeHash(passwordBytes));
            }
        }
        
        public static bool VerifyPasswordHash(string password, string storedHash)
        {
            using (var hmac = new HMACSHA512(SecurityConfig.PasswordSalt))
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] computedHash = hmac.ComputeHash(passwordBytes);
                bool hashesMatch = computedHash.SequenceEqual(Convert.FromBase64String(storedHash));
                return hashesMatch;
            }
        }
    }
}

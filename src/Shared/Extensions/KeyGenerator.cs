using System.Security.Cryptography;

namespace Shared.Extensions;

public class KeyGenerator
{
    public static string Generate256BitSecret()
    {
        using (var rng = new RNGCryptoServiceProvider())
        {
            byte[] key = new byte[32]; // 256 bits
            rng.GetBytes(key);
            return Convert.ToBase64String(key);
        }
    }
}

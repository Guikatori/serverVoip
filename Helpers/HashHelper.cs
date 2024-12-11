using System.Text;
using System.Security.Cryptography;

namespace Helpers.HashHelper;

public static class HashHelper
{
    public static string GenerateFileKey(string fileName)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fileName));
        return $"{Path.GetFileNameWithoutExtension(fileName)}-{Convert.ToHexString(hashBytes)}.mp3";
    }
}
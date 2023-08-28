using System.Security.Cryptography;
using System.Text;

namespace Bitwarden.SecretOperator.Helpers;

public static class DictionaryHelper
{
    
    public static string ComputeHash(this Dictionary<string, string> dict)
    {
        // Sorting dictionary by keys to ensure consistent ordering
        IEnumerable<string> combinedPairs = dict
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => $"{kvp.Key}={kvp.Value}");

        string concatenatedString = string.Join("|", combinedPairs);

        byte[] inputBytes = Encoding.UTF8.GetBytes(concatenatedString);
        
        // sha256
        // byte[] hash = SHA256.HashData(inputBytes);
        // return Convert.ToBase64String(hash).ToLower();

        byte[] hash = SHA1.HashData(inputBytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
    
    public  static string ComputeHash(this Dictionary<string, byte[]> dict)
    {
        // Sorting dictionary by keys to ensure consistent ordering
        List<KeyValuePair<string, byte[]>> sortedDict = dict.OrderBy(kvp => kvp.Key).ToList();

        using var stream = new MemoryStream();
        foreach (KeyValuePair<string, byte[]> pair in sortedDict)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(pair.Key);
            stream.Write(keyBytes, 0, keyBytes.Length);
            stream.WriteByte((byte)'|');
            stream.Write(pair.Value, 0, pair.Value.Length);
        }
        
        // sha256
        // byte[] hash = SHA256.HashData(stream.ToArray());
        // return Convert.ToBase64String(hash);
        
        
        byte[] hash = SHA1.HashData(stream.ToArray());
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}
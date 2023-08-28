using System.Text;
using Bitwarden.SecretOperator.Helpers;
using NFluent;

namespace Bitwarden.SecretOperator.Tests;

public class DictionaryHashComputation
{
    [Fact]
    public void ComputeHashDictionaryStringString()
    {
        var dico1 = new Dictionary<string, string>()
        {
            {"test1", "test2"},
            {"test2", "test3"}
        };
        var dico2 = new Dictionary<string, string>()
        {
            {"test1", "test3"},
            {"test2", "test2"}
        };

        string hash1 = dico1.ComputeHash();
        string hash1Bis = dico1.ComputeHash();
        Check.That(hash1).IsEqualTo(hash1Bis);
        
        string hash2 = dico2.ComputeHash();
        
        Check.That(hash1).IsNotEqualTo(hash2);
    }
    
    [Fact]
    public void ComputeHashDictionaryStringByteArray()
    {
        var dico1 = new Dictionary<string, byte[]>()
        {
            {"test1", "test2"u8.ToArray()},
            {"test2", "test3"u8.ToArray()}
        };
        var dico2 = new Dictionary<string, byte[]>()
        {
            {"test1", "test3"u8.ToArray()},
            {"test2", "test2"u8.ToArray()}
        };

        string hash1 = dico1.ComputeHash();
        string hash1Bis = dico1.ComputeHash();
        Check.That(hash1).IsEqualTo(hash1Bis);
        
        string hash2 = dico2.ComputeHash();
        
        Check.That(hash1).IsNotEqualTo(hash2);
    }
}
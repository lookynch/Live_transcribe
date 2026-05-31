using System.Security.Cryptography;
using System.Text;
using LiveTranscribe.Core;
using LiveTranscribe.Core.Abstractions;

namespace LiveTranscribe.Platform.Security;

/// <summary>
/// Stores the OpenAI API key encrypted with DPAPI (CurrentUser scope). The ciphertext
/// lives in %LocalAppData% — never in the program directory and never in the repo.
/// </summary>
public sealed class DpapiCredentialService : ISecureCredentialService
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("LiveTranscribe.OpenAI.v1");
    private static string KeyFile => Path.Combine(AppPaths.Local, "openai.key");

    public bool HasApiKey => File.Exists(KeyFile);

    public void StoreApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            DeleteApiKey();
            return;
        }

        var protectedBytes = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(apiKey), Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(KeyFile, protectedBytes);
    }

    public string? GetApiKey()
    {
        if (!File.Exists(KeyFile)) return null;
        try
        {
            var protectedBytes = File.ReadAllBytes(KeyFile);
            var bytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (CryptographicException)
        {
            return null; // corrupted or written under a different user
        }
    }

    public void DeleteApiKey()
    {
        try { if (File.Exists(KeyFile)) File.Delete(KeyFile); }
        catch (IOException) { /* best effort */ }
    }
}

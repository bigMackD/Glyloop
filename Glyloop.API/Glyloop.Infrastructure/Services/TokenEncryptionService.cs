using Microsoft.AspNetCore.DataProtection;
using System.Text;

namespace Glyloop.Infrastructure.Services;

/// <summary>
/// Service for encrypting and decrypting OAuth tokens using ASP.NET Core Data Protection API.
/// 
/// Security:
/// - Uses purpose string "Glyloop.DexcomTokens" to isolate keys
/// - Keys are persisted to file system (Docker volume) or Azure Key Vault (production)
/// - Automatic key rotation and expiration handled by Data Protection
/// 
/// Usage:
/// - Encrypt tokens before storing in database
/// - Decrypt tokens when needed for API calls
/// </summary>
public class TokenEncryptionService : ITokenEncryptionService
{
    private readonly IDataProtector _protector;

    public TokenEncryptionService(IDataProtectionProvider dataProtectionProvider)
    {
        if (dataProtectionProvider == null)
            throw new ArgumentNullException(nameof(dataProtectionProvider));

        // Create a protector with a specific purpose string
        // This ensures tokens can only be decrypted by this specific purpose
        _protector = dataProtectionProvider.CreateProtector("Glyloop.DexcomTokens");
    }

    /// <inheritdoc/>
    public byte[] Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            throw new ArgumentException("Plaintext cannot be null or empty.", nameof(plaintext));

        // Convert string to bytes, encrypt, and return encrypted bytes
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        return _protector.Protect(plaintextBytes);
    }

    /// <inheritdoc/>
    public string Decrypt(byte[] ciphertext)
    {
        if (ciphertext == null || ciphertext.Length == 0)
            throw new ArgumentException("Ciphertext cannot be null or empty.", nameof(ciphertext));

        // Decrypt bytes and convert back to string
        var plaintextBytes = _protector.Unprotect(ciphertext);
        return Encoding.UTF8.GetString(plaintextBytes);
    }
}

/// <summary>
/// Interface for token encryption/decryption service.
/// </summary>
public interface ITokenEncryptionService
{
    /// <summary>
    /// Encrypts a plaintext string into encrypted bytes.
    /// </summary>
    /// <param name="plaintext">The plaintext token to encrypt</param>
    /// <returns>Encrypted token as byte array</returns>
    byte[] Encrypt(string plaintext);

    /// <summary>
    /// Decrypts encrypted bytes back to plaintext string.
    /// </summary>
    /// <param name="ciphertext">The encrypted token bytes</param>
    /// <returns>Decrypted plaintext token</returns>
    string Decrypt(byte[] ciphertext);
}


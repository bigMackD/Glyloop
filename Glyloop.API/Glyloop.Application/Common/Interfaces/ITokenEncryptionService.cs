namespace Glyloop.Application.Common.Interfaces;

/// <summary>
/// Service for encrypting and decrypting OAuth tokens.
/// Abstracts Infrastructure implementation from Application layer.
/// </summary>
public interface ITokenEncryptionService
{
    /// <summary>
    /// Encrypts a plaintext token into encrypted bytes.
    /// </summary>
    /// <param name="plaintext">The plaintext token to encrypt</param>
    /// <returns>Encrypted token as byte array</returns>
    byte[] Encrypt(string plaintext);

    /// <summary>
    /// Decrypts encrypted bytes back to plaintext token.
    /// </summary>
    /// <param name="ciphertext">The encrypted token bytes</param>
    /// <returns>Decrypted plaintext token</returns>
    string Decrypt(byte[] ciphertext);
}


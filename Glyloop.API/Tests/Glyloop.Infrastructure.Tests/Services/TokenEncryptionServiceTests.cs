using Glyloop.Application.Common.Interfaces;
using Glyloop.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using NUnit.Framework;

namespace Glyloop.Infrastructure.Tests.Services;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class TokenEncryptionServiceTests
{
    private ITokenEncryptionService CreateService()
    {
        var provider = new EphemeralDataProtectionProvider();
        return new TokenEncryptionService(provider);
    }

    [Test]
    public void EncryptDecrypt_ShouldRoundtrip()
    {
        var svc = CreateService();
        const string plaintext = "secret-token-abc";

        var cipher = svc.Encrypt(plaintext);
        var roundtrip = svc.Decrypt(cipher);

        Assert.Multiple(() =>
        {
            Assert.That(cipher, Is.Not.Null);
            Assert.That(cipher.Length, Is.GreaterThan(0));
            Assert.That(roundtrip, Is.EqualTo(plaintext));
        });
    }

    [Test]
    public void Encrypt_ShouldThrow_OnNullOrEmpty()
    {
        var svc = CreateService();
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => svc.Encrypt(""));
            Assert.Throws<ArgumentException>(() => svc.Encrypt(null!));
        });
    }

    [Test]
    public void Decrypt_ShouldThrow_OnNullOrEmpty()
    {
        var svc = CreateService();
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => svc.Decrypt(Array.Empty<byte>()));
            Assert.Throws<ArgumentException>(() => svc.Decrypt(null!));
        });
    }
}




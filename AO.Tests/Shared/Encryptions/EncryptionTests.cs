using System.Collections.Generic;
using AO.Core.Shared.Encryptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace AO.Tests.Shared.Encryptions;

[TestFixture]
public sealed class EncryptionTests
{
    [Test]
    public void EncryptAndDecrypt_StringWithExplicitPassphrase_RoundTripsPlainText()
    {
        var encryption = new Encryption();
        const string plainText = "hello world";
        const string passphrase = "strong-passphrase";

        var cipherText = encryption.Encrypt(plainText, passphrase);
        var decryptedText = encryption.Decrypt(cipherText, passphrase);

        Assert.That(cipherText, Is.Not.EqualTo(plainText));
        Assert.That(decryptedText, Is.EqualTo(plainText));
    }

    [Test]
    public void EncryptAndDecrypt_BytesWithExplicitPassphrase_RoundTripsPayload()
    {
        var encryption = new Encryption();
        var plainBytes = new byte[] { 1, 2, 3, 4, 5 };
        const string passphrase = "strong-passphrase";

        var cipherBytes = encryption.Encrypt(plainBytes, passphrase);
        var decryptedBytes = encryption.Decrypt(cipherBytes, passphrase);

        Assert.That(cipherBytes, Is.Not.EqualTo(plainBytes));
        Assert.That(decryptedBytes, Is.EqualTo(plainBytes));
    }

    [Test]
    public void Encrypt_WithConfiguration_UsesConfiguredPassphrase()
    {
        var configuration = new TestConfiguration(new Dictionary<string, string?>
        {
            [Encryption.PassphraseConfigurationKey] = "configured-passphrase"
        });
        var encryption = new Encryption(configuration);

        var cipherText = encryption.Encrypt("configured text");
        var decryptedText = encryption.Decrypt(cipherText);

        Assert.That(decryptedText, Is.EqualTo("configured text"));
    }

    [Test]
    public void Encrypt_WithoutConfiguredPassphrase_ThrowsInvalidOperationException()
    {
        var encryption = new Encryption(new TestConfiguration(new Dictionary<string, string?>()));

        var action = () => encryption.Encrypt("plain text");

        Assert.That(action, Throws.InvalidOperationException.With.Message.Contains(Encryption.PassphraseConfigurationKey));
    }

    [Test]
    public void Decrypt_WithInvalidCipherPayload_ThrowsArgumentException()
    {
        var encryption = new Encryption();

        var action = () => encryption.Decrypt(new byte[4], "strong-passphrase");

        Assert.That(action, Throws.ArgumentException.With.Message.Contains("Cipher text payload is invalid."));
    }

    private sealed class TestConfiguration(IDictionary<string, string?> values) : IConfiguration
    {
        public string? this[string key]
        {
            get => values.TryGetValue(key, out var value) ? value : null;
            set => values[key] = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren() => [];

        public IChangeToken GetReloadToken() => NoopChangeToken.Instance;

        public IConfigurationSection GetSection(string key)
        {
            return new TestConfigurationSection(key, this[key]);
        }
    }

    private sealed class TestConfigurationSection(string key, string? value) : IConfigurationSection
    {
        public string? this[string key]
        {
            get => string.Empty;
            set { }
        }

        public string Key => key;

        public string Path => key;

        public string? Value { get; set; } = value;

        public IEnumerable<IConfigurationSection> GetChildren() => [];

        public IChangeToken GetReloadToken() => NoopChangeToken.Instance;

        public IConfigurationSection GetSection(string sectionKey) => new TestConfigurationSection(sectionKey, null);
    }

    private sealed class NoopChangeToken : IChangeToken
    {
        public static NoopChangeToken Instance { get; } = new();

        public bool HasChanged => false;

        public bool ActiveChangeCallbacks => false;

        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => EmptyDisposable.Instance;
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static EmptyDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}

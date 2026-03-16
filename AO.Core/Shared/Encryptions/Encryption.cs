using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace AO.Core.Shared.Encryptions;

public sealed class Encryption
{
    public const string PassphraseConfigurationKey = "Encryption:Passphrase";
    private const int SaltSize = 16;
    private const int IvSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private readonly IConfiguration? _configuration;

    public Encryption()
    {
    }

    public Encryption(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Encrypt(string plainText)
    {
        return Encrypt(plainText, GetConfiguredPassphrase());
    }

    public string Encrypt(string plainText, string passphrase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);
        ArgumentException.ThrowIfNullOrWhiteSpace(passphrase);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = Encrypt(plainBytes, passphrase);

        return Convert.ToBase64String(encryptedBytes);
    }

    public string Decrypt(string cipherText)
    {
        return Decrypt(cipherText, GetConfiguredPassphrase());
    }

    public string Decrypt(string cipherText, string passphrase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherText);
        ArgumentException.ThrowIfNullOrWhiteSpace(passphrase);

        var cipherBytes = Convert.FromBase64String(cipherText);
        var decryptedBytes = Decrypt(cipherBytes, passphrase);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    public byte[] Encrypt(byte[] plainBytes)
    {
        return Encrypt(plainBytes, GetConfiguredPassphrase());
    }

    public byte[] Encrypt(byte[] plainBytes, string passphrase)
    {
        ArgumentNullException.ThrowIfNull(plainBytes);
        ArgumentException.ThrowIfNullOrWhiteSpace(passphrase);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var iv = RandomNumberGenerator.GetBytes(IvSize);
        var key = DeriveKey(passphrase, salt);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var memoryStream = new MemoryStream();
        memoryStream.Write(salt);
        memoryStream.Write(iv);

        using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
        {
            cryptoStream.Write(plainBytes);
            cryptoStream.FlushFinalBlock();
        }

        return memoryStream.ToArray();
    }

    public byte[] Decrypt(byte[] cipherBytes)
    {
        return Decrypt(cipherBytes, GetConfiguredPassphrase());
    }

    public byte[] Decrypt(byte[] cipherBytes, string passphrase)
    {
        ArgumentNullException.ThrowIfNull(cipherBytes);
        ArgumentException.ThrowIfNullOrWhiteSpace(passphrase);

        if (cipherBytes.Length <= SaltSize + IvSize)
        {
            throw new ArgumentException("Cipher text payload is invalid.", nameof(cipherBytes));
        }

        var salt = cipherBytes[..SaltSize];
        var iv = cipherBytes[SaltSize..(SaltSize + IvSize)];
        var payload = cipherBytes[(SaltSize + IvSize)..];
        var key = DeriveKey(passphrase, salt);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var inputStream = new MemoryStream(payload);
        using var cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var outputStream = new MemoryStream();
        cryptoStream.CopyTo(outputStream);

        return outputStream.ToArray();
    }

    private static byte[] DeriveKey(string passphrase, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
    }

    private string GetConfiguredPassphrase()
    {
        var passphrase = _configuration?[PassphraseConfigurationKey];

        if (string.IsNullOrWhiteSpace(passphrase))
        {
            throw new InvalidOperationException($"Encryption passphrase is not configured. Set '{PassphraseConfigurationKey}'.");
        }

        return passphrase;
    }
}

using System.Security.Cryptography;
using System.Text;

namespace DatabaseMcp.Core.Services
{
    /// <summary>
    /// Provides AES-256 encryption and decryption for connection strings.
    /// <para>
    /// <strong>Important security note:</strong> This encryption uses a hardcoded key compiled
    /// into the binary. It is NOT a cryptographic security measure — anyone with access to
    /// the source code or binary can derive the key and decrypt the config file.
    /// </para>
    /// <para>
    /// The purpose of this encryption is <em>accident prevention</em>:
    /// <list type="bullet">
    ///   <item>Prevents LLMs from reading plaintext credentials when accessing the config file</item>
    ///   <item>Prevents automated credential scanners from finding plaintext connection strings</item>
    ///   <item>Prevents casual file browsing from exposing database passwords</item>
    /// </list>
    /// This is a deliberate design trade-off: convenience over true security. For production
    /// secrets management, use a proper secrets vault (e.g. HashiCorp Vault, AWS Secrets Manager).
    /// </para>
    /// </summary>
    public static class EncryptionService
    {
        private static readonly byte[] Key = DeriveKey("DatabaseMcp-AccidentPrevention-v1");

        /// <summary>
        /// Encrypts plaintext using AES-256-CBC with a random IV.
        /// The IV is prepended to the ciphertext and the result is Base64-encoded.
        /// </summary>
        /// <param name="plaintext">The plaintext to encrypt.</param>
        /// <returns>The encrypted data as a Base64 string.</returns>
        public static string Encrypt(string plaintext)
        {
            using Aes aes = Aes.Create();
            aes.Key = Key;
            aes.GenerateIV();

            using ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] ciphertextBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

            byte[] result = new byte[aes.IV.Length + ciphertextBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(ciphertextBytes, 0, result, aes.IV.Length, ciphertextBytes.Length);

            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Decrypts a Base64-encoded ciphertext that was encrypted with <see cref="Encrypt"/>.
        /// </summary>
        /// <param name="encryptedBase64">The Base64-encoded encrypted data.</param>
        /// <returns>The decrypted plaintext.</returns>
        /// <exception cref="FormatException">Thrown if the input is not valid Base64.</exception>
        /// <exception cref="CryptographicException">Thrown if decryption fails.</exception>
        public static string Decrypt(string encryptedBase64)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);

            if (encryptedBytes.Length < 16)
            {
                throw new CryptographicException("Encrypted data is too short to contain a valid IV.");
            }

            byte[] iv = new byte[16];
            Buffer.BlockCopy(encryptedBytes, 0, iv, 0, 16);

            byte[] ciphertext = new byte[encryptedBytes.Length - 16];
            Buffer.BlockCopy(encryptedBytes, 16, ciphertext, 0, ciphertext.Length);

            using Aes aes = Aes.Create();
            aes.Key = Key;
            aes.IV = iv;

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] plaintextBytes = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

            return Encoding.UTF8.GetString(plaintextBytes);
        }

        private static byte[] DeriveKey(string passphrase)
        {
            byte[] password = Encoding.UTF8.GetBytes(passphrase);
            byte[] salt = Encoding.UTF8.GetBytes("DatabaseMcp-Salt-20260414");
            return Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        }
    }
}

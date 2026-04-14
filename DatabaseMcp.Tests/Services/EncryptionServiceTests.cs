using DatabaseMcp.Core.Services;
using System.Security.Cryptography;

namespace DatabaseMcp.Tests.Services
{
    public class EncryptionServiceTests
    {
        [Fact]
        public void Encrypt_ThenDecrypt_ReturnsOriginalString()
        {
            string original = "Host=localhost;Database=test;Username=user;Password=secret123";

            string encrypted = EncryptionService.Encrypt(original);
            string decrypted = EncryptionService.Decrypt(encrypted);

            Assert.Equal(original, decrypted);
        }

        [Fact]
        public void Encrypt_DifferentInputs_ProduceDifferentCiphertexts()
        {
            string encrypted1 = EncryptionService.Encrypt("input-one");
            string encrypted2 = EncryptionService.Encrypt("input-two");

            Assert.NotEqual(encrypted1, encrypted2);
        }

        [Fact]
        public void Encrypt_SameInput_ProducesDifferentCiphertexts_DueToRandomIV()
        {
            string input = "same-input";

            string encrypted1 = EncryptionService.Encrypt(input);
            string encrypted2 = EncryptionService.Encrypt(input);

            Assert.NotEqual(encrypted1, encrypted2);
        }

        [Fact]
        public void Encrypt_EmptyString_RoundTripsCorrectly()
        {
            string original = "";

            string encrypted = EncryptionService.Encrypt(original);
            string decrypted = EncryptionService.Decrypt(encrypted);

            Assert.Equal(original, decrypted);
        }

        [Fact]
        public void Encrypt_UnicodeString_RoundTripsCorrectly()
        {
            string original = "Password=p@ssw0rd!§£€¥¢";

            string encrypted = EncryptionService.Encrypt(original);
            string decrypted = EncryptionService.Decrypt(encrypted);

            Assert.Equal(original, decrypted);
        }

        [Fact]
        public void Decrypt_InvalidBase64_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(() => EncryptionService.Decrypt("not-valid-base64!!!"));
        }

        [Fact]
        public void Decrypt_TooShortData_ThrowsCryptographicException()
        {
            string tooShort = Convert.ToBase64String(new byte[8]);

            Assert.Throws<CryptographicException>(() => EncryptionService.Decrypt(tooShort));
        }

        [Fact]
        public void Decrypt_CorruptData_ThrowsCryptographicException()
        {
            byte[] garbage = new byte[48];
            Random.Shared.NextBytes(garbage);
            string corruptData = Convert.ToBase64String(garbage);

            Assert.Throws<CryptographicException>(() => EncryptionService.Decrypt(corruptData));
        }

        [Fact]
        public void Encrypt_LongConnectionString_RoundTripsCorrectly()
        {
            string original = "Host=very-long-hostname.database.example.com;Port=5432;Database=my_long_database_name;Username=application_user;Password=a-very-long-and-complex-password-with-special-chars!@#$%^&*();SSL Mode=Require;Application Name=DatabaseMcp Test";

            string encrypted = EncryptionService.Encrypt(original);
            string decrypted = EncryptionService.Decrypt(encrypted);

            Assert.Equal(original, decrypted);
        }
    }
}

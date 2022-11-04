// 
//  StringCipher.cs
//  
//  Author:
//       http://stackoverflow.com/questions/10168240/encrypting-decrypting-a-string-in-c-sharp
// 
using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace HomeGenie.Service
{
    public static class StringCipher
    {
        // This constant string is used as a "salt" value for the PasswordDeriveBytes function calls.
        // This size of the IV (in bytes) must = (keysize / 8).  Default keysize is 256, so the IV must be
        // 32 bytes long.  Using a 16 character string here gives us 32 bytes when converted to a byte array.
        private const string InitVector = "h7g3e4m3t5st5zjw";

        // This constant is used to determine the keysize of the encryption algorithm.
        private const int KeySize = 256;

        public static string Encrypt(string plainText, string passPhrase)
        {
            byte[] initVectorBytes = Encoding.UTF8.GetBytes(InitVector);
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            var password = new PasswordDeriveBytes(passPhrase, null);
            byte[] keyBytes = password.GetBytes(KeySize / 8);
            using (var symmetricKey = Aes.Create("AesManaged"))
            {
                symmetricKey.Mode = CipherMode.CBC;
                var encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes);
                var memoryStream = new MemoryStream();
                var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                cryptoStream.FlushFinalBlock();
                byte[] cipherTextBytes = memoryStream.ToArray();
                memoryStream.Close();
                cryptoStream.Close();
                return Convert.ToBase64String(cipherTextBytes);
            }
        }

        public static string Decrypt(string cipherText, string passPhrase)
        {
            byte[] initVectorBytes = Encoding.ASCII.GetBytes(InitVector);
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
            var password = new PasswordDeriveBytes(passPhrase, null);
            byte[] keyBytes = password.GetBytes(KeySize / 8);
            using (var symmetricKey = Aes.Create("AesManaged"))
            {
                symmetricKey.Mode = CipherMode.CBC;
                var decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);
                var memoryStream = new MemoryStream(cipherTextBytes);
                var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                using (var plainTextReader = new StreamReader(cryptoStream))
                {
                    return plainTextReader.ReadToEnd();
                }  
            }
        }
    }
}

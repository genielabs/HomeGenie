// http://www.codeproject.com/Tips/704372/How-to-use-Rijndael-ManagedEncryption-with-Csharp

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ZWaveLib.Devices
{
    public class AES_work
    {


        /*internal byte[] NetworkKey = new byte[] { 
            (byte)0x01,
            (byte)0x02,
            (byte)0x03,
            (byte)0x04,
            (byte)0x05,
            (byte)0x06,
            (byte)0x07,
            (byte)0x08,
            (byte)0x09,
            (byte)0x0A,
            (byte)0x0B,
            (byte)0x0C,
            (byte)0x0D,
            (byte)0x0E,
            (byte)0x0F,
            (byte)0x10};
        */


        internal byte[] zeroIV = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        //public byte[] EncryptKey = new byte[] { 0x85, 0x22, 0x71, 0x7d, 0x3a, 0xd1, 0xfb, 0xfe, 0xaf, 0xa1, 0xce, 0xaa, 0xfd, 0xf5, 0x65, 0x65 };
//        private byte[] EncryptKey = null;
//        private byte[] AuthKey = null;
        /*
        public byte[] getEncryptKey() {
            return EncryptKey;
        }

        public byte[] getAuthKey()
        {
            return AuthKey;
        }
        */
        public byte[] generateKey1(byte[] nc, byte[] plaintext) {
            byte[] tmp = new byte[zeroIV.Length];

            Array.Copy(EncryptMessage(nc, zeroIV, plaintext, CipherMode.CBC), tmp, 16);
            return tmp;
        }

        public byte[] OFB_EncryptMessage(byte[] nc, byte[] iv, byte[] plaintext)
        {
            byte[] processed = new byte[plaintext.Length];
            int len = (plaintext.Length % 16)*16;
//            int len = 2048;
            byte[] l_plaintext = new byte[len];

            byte[] encrypted = EncryptMessage(nc, iv, l_plaintext, CipherMode.CBC);

            for (int i = 0; i < plaintext.Length; i++)
            {
                processed[i] = (byte)(plaintext[i] ^ encrypted[i]);
            }

            return processed;
        }

        public byte[] ECB_EncryptMessage(byte[] nc, byte[] plaintext) {
            byte[] tmp = new byte[zeroIV.Length];

            Array.Copy(EncryptMessage(nc, zeroIV, plaintext, CipherMode.ECB), tmp, 16);

            return tmp;
        }

        private byte[] EncryptMessage(byte[] nc, byte[] iv, byte[] plaintext, CipherMode cm)
        {
            if (nc == null) {
                Console.WriteLine("The used key has not been generated.");
                return zeroIV;
            }

            RijndaelManaged rijndael = new RijndaelManaged();
            rijndael.Key = nc;
            rijndael.IV = iv;
            rijndael.Mode = cm;
//            rijndael.Padding = PaddingMode.Zeros;

            return EncryptBytes(rijndael, plaintext);
        }

        private static byte[] EncryptBytes(
            SymmetricAlgorithm alg,
            byte[] message
        )
        {
            if ((message == null) || (message.Length == 0))
            {
                return message;
            }

            if (alg == null)
            {
                throw new ArgumentNullException("alg");
            }

            using (var stream = new MemoryStream())
            using (var encryptor = alg.CreateEncryptor())
            using (var encrypt = new CryptoStream(stream, encryptor, CryptoStreamMode.Write))
            {
                encrypt.Write(message, 0, message.Length);
                encrypt.FlushFinalBlock();
                return stream.ToArray();
            }
        }
    }
}

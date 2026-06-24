using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SecretLetter.Core
{
    public static class SecretLetterCrypto
    {
        /// <summary>
        /// Converts a hex string (64 chars) into 32 raw bytes.
        /// </summary>
        private static byte[] HexToBytes(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have even length.");

            byte[] bytes = new byte[hex.Length / 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }

            return bytes;
        }

        /// <summary>
        /// Generates a 32-byte keystream block using HMAC-SHA256(key, code || counter).
        /// </summary>
        private static void GenerateKeystreamBlock(
            byte[] key,
            byte[] codeBytes,
            uint counter,
            byte[] output32)
        {
            if (output32.Length != 32)
                throw new ArgumentException("output32 must be 32 bytes.");

            // Build message = codeBytes + counter (big-endian)
            byte[] msg = new byte[codeBytes.Length + 4];
            Buffer.BlockCopy(codeBytes, 0, msg, 0, codeBytes.Length);

            msg[codeBytes.Length + 0] = (byte)((counter >> 24) & 0xFF);
            msg[codeBytes.Length + 1] = (byte)((counter >> 16) & 0xFF);
            msg[codeBytes.Length + 2] = (byte)((counter >> 8) & 0xFF);
            msg[codeBytes.Length + 3] = (byte)(counter & 0xFF);

            using var hmac = new HMACSHA256(key);
            byte[] block = hmac.ComputeHash(msg);

            Buffer.BlockCopy(block, 0, output32, 0, 32);
        }

        /// <summary>
        /// Encrypts or decrypts a file by XORing with a deterministic keystream.
        /// </summary>
        public static void TransformFile(
            string inputPath,
            string outputPath,
            string hexKey,
            string code4Digits)
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException("Input file not found", inputPath);

            if (hexKey.Length != 64)
                throw new ArgumentException("Key must be 64 hex characters (32 bytes).");

            if (code4Digits.Length != 4 || !code4Digits.All(char.IsDigit))
                throw new ArgumentException("Code must be exactly 4 digits.");

            byte[] key = HexToBytes(hexKey);
            byte[] codeBytes = Encoding.ASCII.GetBytes(code4Digits);

            using FileStream fin = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
            using FileStream fout = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

            byte[] block = new byte[32];
            uint counter = 0;
            int blockPos = 0;

            int b;
            while ((b = fin.ReadByte()) != -1)
            {
                if (blockPos == 0)
                {
                    GenerateKeystreamBlock(key, codeBytes, counter++, block);
                }

                byte ks = block[blockPos++];
                if (blockPos == 32)
                    blockPos = 0;

                byte outByte = (byte)(b ^ ks);
                fout.WriteByte(outByte);
            }
        }
    }
}

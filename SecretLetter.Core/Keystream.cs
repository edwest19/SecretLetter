using System;
using System.Security.Cryptography;
using System.Text;

namespace SecretLetter.Core
{
    public static class Keystream
    {
        /// <summary>
        /// Convert a hex string into raw bytes.
        /// </summary>
        public static byte[] HexToBytes(string hex)
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
        /// Generate a 32-byte keystream block using:
        /// HMAC-SHA256(key, code || counter)
        /// </summary>
        public static void GenerateBlock(
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

            // HMAC-SHA256(key, msg)
            using var hmac = new HMACSHA256(key);
            byte[] hash = hmac.ComputeHash(msg);

            // Copy first 32 bytes into output32
            Buffer.BlockCopy(hash, 0, output32, 0, 32);
        }

        /// <summary>
        /// Generate a keystream of arbitrary length using repeated blocks.
        /// </summary>
        public static byte[] GenerateKeystream(string hexKey, string code, int length)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            byte[] key = HexToBytes(hexKey);
            byte[] codeBytes = Encoding.UTF8.GetBytes(code);

            byte[] result = new byte[length];
            byte[] block = new byte[32];

            uint counter = 0;
            int offset = 0;

            while (offset < length)
            {
                GenerateBlock(key, codeBytes, counter, block);
                counter++;

                int toCopy = Math.Min(32, length - offset);
                Buffer.BlockCopy(block, 0, result, offset, toCopy);
                offset += toCopy;
            }

            return result;
        }
    }
}

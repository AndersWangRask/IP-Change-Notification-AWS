using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace IPChange.Core.Helpers
{
    public static class StringExtensions
    {
        /// <summary>
        /// Compresses a string and returns a deflate compressed, Base64 encoded string.
        /// </summary>
        /// <param name="uncompressedString">String to compress</param>
        /// <remarks>
        /// Adapted from these answers on StackOverflow: 
        /// https://stackoverflow.com/questions/7343465/compression-decompression-string-with-c-sharp
        /// </remarks>
        public static string Compress(this string uncompressedString)
        {
            using (MemoryStream compressedStream = new MemoryStream())
            {
                using (MemoryStream uncompressedStream = new MemoryStream(Encoding.UTF8.GetBytes(uncompressedString)))
                {
                    using (DeflateStream compressorStream = new DeflateStream(compressedStream, CompressionMode.Compress, true))
                    {
                        uncompressedStream.CopyTo(compressorStream);
                    }

                    return Convert.ToBase64String(compressedStream.ToArray());
                }
            }
        }

        /// <summary>
        /// Decompresses a deflate compressed, Base64 encoded string and returns an uncompressed string.
        /// </summary>
        /// <param name="compressedString">String to decompress.</param>
        /// <remarks>
        /// Adapted from these answers on StackOverflow: 
        /// https://stackoverflow.com/questions/7343465/compression-decompression-string-with-c-sharp
        /// </remarks>
        public static string Decompress(this string compressedString)
        {
            using (MemoryStream decompressedStream = new MemoryStream())
            {
                using (MemoryStream compressedStream = new MemoryStream(Convert.FromBase64String(compressedString)))
                {
                    using (DeflateStream decompressorStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
                    {
                        decompressorStream.CopyTo(decompressedStream);
                    }

                    return Encoding.UTF8.GetString(decompressedStream.ToArray());
                }
            }
        }

        /// <summary>
        /// Makes a simple password based encryption of a string and returns the result as a string
        /// <seealso cref="Decrypt(string, string)"/>
        /// </summary>
        /// <param name="unencryptedString">The string to encrypt</param>
        /// <param name="password">The password to encrypt with</param>
        /// <returns>The encrypted string</returns>
        /// <remarks>
        /// Please understand the security limitations of symmetrical encryption with a known clear-text password.
        /// </remarks>
        public static string Encrypt(this string unencryptedString, string password) => AESGCM.SimpleEncryptWithPassword(unencryptedString, password);

        /// <summary>
        /// Decrypts a simply encrypted string with a password: <seealso cref="Encrypt(string, string)"/>
        /// </summary>
        /// <param name="encryptedString">The string to decrypt.</param>
        /// <param name="password">The password to decrypt with (must be the same that was used for encrypting)</param>
        /// <returns>The decrypted string</returns>
        /// <remarks>
        /// Please understand the security limitations of symmetrical encryption with a known clear-text password.
        /// </remarks>
        public static string Decrypt(this string encryptedString, string password) => AESGCM.SimpleDecryptWithPassword(encryptedString, password);

        public static string CompressThenEncrypt(this string uncompressedString, string password)
        {
            using (MemoryStream compressedStream = new MemoryStream())
            {
                using (MemoryStream uncompressedStream = new MemoryStream(Encoding.UTF8.GetBytes(uncompressedString)))
                {
                    using (DeflateStream compressorStream = new DeflateStream(compressedStream, CompressionMode.Compress, true))
                    {
                        uncompressedStream.CopyTo(compressorStream);
                    }

                    byte[] encryptedBytes = AESGCM.SimpleEncryptWithPassword(compressedStream.ToArray(), password);

                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }

        public static string DecryptThenDecompress(this string inputString, string password)
        {
            using (MemoryStream decompressedStream = new MemoryStream())
            {
                using (MemoryStream compressedStream = new MemoryStream(AESGCM.SimpleDecryptWithPassword(Convert.FromBase64String(inputString), password)))
                {
                    using (DeflateStream decompressorStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
                    {
                        decompressorStream.CopyTo(decompressedStream);
                    }

                    return Encoding.UTF8.GetString(decompressedStream.ToArray());
                }
            }
        }
    }
}
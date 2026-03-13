using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDMUtils
{

    public static class SaveCompressor
    {
        public enum CompressionFormat
        {
            None,
            Base64,
            Byte,
            error
        }
        public class CompressedFile(string FileContent)
        {
            public string Uncompressed { get; } = FileContent;
            public byte[] Bytes { get; } = Compress(FileContent);

            public override string ToString()
            {
                return GetBytesAsString(Bytes);
            }
        }

        public static string Decompress(byte[] dataToDeCompress)
        {
            byte[] decompressedData = DecompressByte(dataToDeCompress);
            return Encoding.UTF8.GetString(decompressedData);
        }

        public static string Decompress(string String)
        {
            return Decompress(Convert.FromBase64String(String));
        }
#if NET6_0_OR_GREATER
        public const CompressionLevel BestCompression = CompressionLevel.SmallestSize;
#else
        public const CompressionLevel BestCompression = CompressionLevel.Optimal;
#endif
        private static byte[] CompressByte(byte[] bytes)
        {
            using var memoryStream = new MemoryStream(bytes.Length);
            using (var gzipStream = new GZipStream(memoryStream, BestCompression, leaveOpen: true))
            {
                gzipStream.Write(bytes, 0, bytes.Length);
            }
            return memoryStream.ToArray();
        }
        private static byte[] DecompressByte(byte[] bytes)
        {
            using var memoryStream = new MemoryStream(bytes);
            using var decompressStream = new GZipStream(memoryStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream(bytes.Length * 2);
            decompressStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }

        private static byte[] Compress(string str) => CompressByte(Encoding.UTF8.GetBytes(str));

        private static string GetBytesAsString(byte[] byteData)
        {
            return Convert.ToBase64String(byteData);
        }

        public static CompressionFormat GetFileCompressionFormat<T>(string filePath, bool prioritizeCompressed)
        {
            if (!File.Exists(filePath))
                return CompressionFormat.error;

            byte[] fileBytes = File.ReadAllBytes(filePath);

            if (prioritizeCompressed)
            {
                if (TestForByteFile<T>(fileBytes))
                    return CompressionFormat.Byte;
            }

            string content = Encoding.UTF8.GetString(fileBytes);

            if (TestForUncompressedFile<T>(content))
                return CompressionFormat.None;

            if (!prioritizeCompressed && TestForByteFile<T>(fileBytes))
                return CompressionFormat.Byte;

            if (TestForCompressedFile<T>(content))
                return CompressionFormat.Base64;

            return CompressionFormat.error;
        }

        private static bool TestForUncompressedFile<T>(string FileContent)
        {
            return DataFileUtilities.IsJsonTypeOf<T>(FileContent);
        }
        private static bool TestForCompressedFile<T>(string FileContent)
        {
            try
            {
                var DecompSave = Decompress(FileContent);
                return DataFileUtilities.IsJsonTypeOf<T>(DecompSave);
            }
            catch
            {
                return false;
            }
        }
        private static bool TestForByteFile<T>(byte[] FileContent)
        {
            try
            {
                var DecompSave = Decompress(FileContent);
                return DataFileUtilities.IsJsonTypeOf<T>(DecompSave);
            }
            catch
            {
                return false;
            }
        }

        public static string DecompressFile<T>(string FilePath, bool PrioritizeCompressed)
        {
            return GetFileCompressionFormat<T>(FilePath, PrioritizeCompressed) switch
            {
                CompressionFormat.None => File.ReadAllText(FilePath),
                CompressionFormat.Base64 => Decompress(File.ReadAllText(FilePath)),
                CompressionFormat.Byte => Decompress(File.ReadAllBytes(FilePath)),
                _ => string.Empty,
            };
        }
    }
}

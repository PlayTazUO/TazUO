using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ClassicUO.Utility
{
    public static class MapChecksumCalculator
    {
        private const int BUFFER_SIZE = 8192;

        public static string CalculateMapChecksum(int mapIndex, int[,] mapBlocksSize, int[,] mapsDefaultSize, string clientVersion)
        {
            using (var sha256 = SHA256.Create())
            {
                // Combine multiple sources of data that affect pathfinding
                var checksumData = new StringBuilder();

                // Map index
                checksumData.Append(mapIndex);
                checksumData.Append("|");

                // Map dimensions (affects pathfinding generation)
                if (mapIndex < mapBlocksSize.GetLength(0))
                {
                    checksumData.Append(mapBlocksSize[mapIndex, 0]);
                    checksumData.Append(",");
                    checksumData.Append(mapBlocksSize[mapIndex, 1]);
                }
                checksumData.Append("|");

                // Default map size (can change between UO versions)
                if (mapIndex < mapsDefaultSize.GetLength(0))
                {
                    checksumData.Append(mapsDefaultSize[mapIndex, 0]);
                    checksumData.Append(",");
                    checksumData.Append(mapsDefaultSize[mapIndex, 1]);
                }
                checksumData.Append("|");

                // Client version (affects pathfinding logic)
                checksumData.Append(clientVersion ?? "unknown");

                // Add file-specific checksums if map files exist
                string mapFileChecksum = CalculateMapFileChecksum(mapIndex);
                if (!string.IsNullOrEmpty(mapFileChecksum))
                {
                    checksumData.Append("|");
                    checksumData.Append(mapFileChecksum);
                }

                var dataBytes = Encoding.UTF8.GetBytes(checksumData.ToString());
                var hashBytes = sha256.ComputeHash(dataBytes);

                return Convert.ToBase64String(hashBytes);
            }
        }

        private static string CalculateMapFileChecksum(int mapIndex)
        {
            try
            {
                // Try to calculate checksum of actual map files
                string mapFilePath = GetMapFilePath(mapIndex);
                if (!string.IsNullOrEmpty(mapFilePath) && File.Exists(mapFilePath))
                {
                    return CalculateFileChecksum(mapFilePath);
                }
            }
            catch (Exception)
            {
                // If we can't access map files, that's ok - we'll use other data
            }

            return string.Empty;
        }

        private static string GetMapFilePath(int mapIndex)
        {
            // This is a simplified approach - in practice, we'd need to integrate
            // with UOFileManager to get the correct paths
            try
            {
                string[] possiblePaths = {
                    $"map{mapIndex}.mul",
                    $"map{mapIndex}LegacyMUL.uop"
                };

                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore file access errors
            }

            return null;
        }

        private static string CalculateFileChecksum(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // For large files, we'll sample the beginning, middle, and end
                // to create a representative checksum without reading the entire file
                var sampleData = new byte[BUFFER_SIZE * 3]; // 3 samples
                int totalRead = 0;

                // Read beginning
                int bytesRead = fileStream.Read(sampleData, totalRead, BUFFER_SIZE);
                totalRead += bytesRead;

                // Read middle
                if (fileStream.Length > BUFFER_SIZE * 2)
                {
                    fileStream.Seek(fileStream.Length / 2, SeekOrigin.Begin);
                    bytesRead = fileStream.Read(sampleData, totalRead, BUFFER_SIZE);
                    totalRead += bytesRead;
                }

                // Read end
                if (fileStream.Length > BUFFER_SIZE * 3)
                {
                    fileStream.Seek(-BUFFER_SIZE, SeekOrigin.End);
                    bytesRead = fileStream.Read(sampleData, totalRead, BUFFER_SIZE);
                    totalRead += bytesRead;
                }

                // Include file size and modification time in checksum
                var fileInfo = new FileInfo(filePath);
                var combinedData = new byte[totalRead + 16]; // 8 bytes for size + 8 bytes for time
                Array.Copy(sampleData, 0, combinedData, 0, totalRead);

                var sizeBytes = BitConverter.GetBytes(fileInfo.Length);
                var timeBytes = BitConverter.GetBytes(fileInfo.LastWriteTime.ToBinary());

                Array.Copy(sizeBytes, 0, combinedData, totalRead, 8);
                Array.Copy(timeBytes, 0, combinedData, totalRead + 8, 8);

                var hashBytes = sha256.ComputeHash(combinedData, 0, totalRead + 16);
                return Convert.ToBase64String(hashBytes);
            }
        }

        public static bool ValidateChecksum(string storedChecksum, string currentChecksum)
        {
            if (string.IsNullOrEmpty(storedChecksum) || string.IsNullOrEmpty(currentChecksum))
            {
                return false;
            }

            return string.Equals(storedChecksum, currentChecksum, StringComparison.Ordinal);
        }
    }
}

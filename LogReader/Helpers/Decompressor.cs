using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace LogReader.Helpers
{
    public class Decompressor
    {
        private MemoryStream decompressedMemoryStream = new MemoryStream();

        public MemoryStream GZipDecompress (MemoryStream memoryStream)
        {
            try
            {
                using (GZipStream decompressionStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    decompressionStream.CopyTo(decompressedMemoryStream);
                }
                return decompressedMemoryStream;
            }
            catch (Exception e)
            {

                return null;
            }
        }

        public MemoryStream ZipDecompress(MemoryStream memoryStream)
        {
            try
            {
                using (ZipArchive zip = new ZipArchive(memoryStream))
                {
                    var firstFileInArchive = zip.Entries.First();
                    firstFileInArchive.Open().CopyTo(decompressedMemoryStream);
                }
                return decompressedMemoryStream;
            }
            catch (Exception e)
            {

                return null;
            }

        }
    }
}

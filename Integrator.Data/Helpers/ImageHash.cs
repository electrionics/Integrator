using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Image = System.Drawing.Image;

namespace Integrator.Data.Helpers
{
    public class ImageHash
    {
        public string GetHash(string filePath)
        {
#pragma warning disable CA1416 // Validate platform compatibility
            using (var image = (Bitmap)Image.FromFile(filePath))
                return GetHash(image);
#pragma warning restore CA1416 // Validate platform compatibility
        }

        public string GetHash(Bitmap bitmap)
        {
            using (var memoryStream = new MemoryStream())
            {
                //var metafields = GetMetaFields(bitmap).ToArray();

                //if (metafields.Any())
                //    formatter.Serialize(memoryStream, metafields);

                var pixelBytes = GetPixelBytes(bitmap);
                memoryStream.Write(pixelBytes, 0, pixelBytes.Length);

                using (var hashAlgorithm = GetHashAlgorithm())
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    var hash = hashAlgorithm.ComputeHash(memoryStream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private static HashAlgorithm GetHashAlgorithm() => MD5.Create();

#pragma warning disable CA1416 // Validate platform compatibility
        private static byte[] GetPixelBytes(Bitmap bitmap, PixelFormat pixelFormat = PixelFormat.Format32bppRgb)
        {

            var lockedBits = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, pixelFormat);

            var bufferSize = lockedBits.Height * lockedBits.Stride;
            var buffer = new byte[bufferSize];
            Marshal.Copy(lockedBits.Scan0, buffer, 0, bufferSize);

            bitmap.UnlockBits(lockedBits);
#pragma warning restore CA1416 // Validate platform compatibility

            return buffer;
        }

        private static IEnumerable<KeyValuePair<string, string>> GetMetaFields(Image image)
        {
#pragma warning disable CA1416 // Validate platform compatibility
            string manufacturer = System.Text.Encoding.ASCII.GetString(image.PropertyItems[1].Value);

#pragma warning restore CA1416 // Validate platform compatibility
            yield return new KeyValuePair<string, string>("manufacturer", manufacturer);

            // return any other fields you may be interested in
        }
    }
}

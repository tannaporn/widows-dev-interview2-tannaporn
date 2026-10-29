using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;

namespace widows_dev_interview2.Services
{
    public class ImageService
    {
        private const int MaximumImageBytes = 20 * 1024 * 1024;
        private static readonly SemaphoreSlim ImageSlots = new SemaphoreSlim(4);
        private static readonly HttpClient ImageHttpClient = new HttpClient(
            new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        public static Uri? GetHttpsUri(string? url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || string.IsNullOrWhiteSpace(uri.Host)
                || !string.IsNullOrEmpty(uri.UserInfo))
            {
                return null;
            }

            return uri;
        }

        public async Task<BitmapSource?> LoadAsync(string? url, int decodePixelWidth,
            CancellationToken cancellationToken)
        {
            Uri? uri = GetHttpsUri(url);

            if (uri == null)
            {
                return null;
            }

            await ImageSlots.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                using CancellationTokenSource imageCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                imageCancellation.CancelAfter(TimeSpan.FromSeconds(20));

                using HttpResponseMessage response = await ImageHttpClient.GetAsync(uri,
                    HttpCompletionOption.ResponseHeadersRead, imageCancellation.Token).ConfigureAwait(false);

                long? contentLength = response.Content.Headers.ContentLength;

                if (!response.IsSuccessStatusCode
                    || (contentLength.HasValue && contentLength.Value > MaximumImageBytes))
                {
                    return null;
                }

                using Stream imageStream = await response.Content.ReadAsStreamAsync(imageCancellation.Token)
                    .ConfigureAwait(false);
                using MemoryStream imageBytes = new MemoryStream();
                byte[] buffer = new byte[8192];

                while (true)
                {
                    int bytesRead = await imageStream.ReadAsync(buffer.AsMemory(), imageCancellation.Token)
                        .ConfigureAwait(false);

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    if (imageBytes.Length + bytesRead > MaximumImageBytes)
                    {
                        return null;
                    }

                    imageBytes.Write(buffer, 0, bytesRead);
                }

                byte[] data = imageBytes.ToArray();
                BitmapSource image = await Task.Run(() => DecodeImage(data, decodePixelWidth),
                    imageCancellation.Token).ConfigureAwait(false);
                imageCancellation.Token.ThrowIfCancellationRequested();
                return image;
            }
            catch (Exception)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return null;
            }
            finally
            {
                ImageSlots.Release();
            }
        }

        private static BitmapSource DecodeImage(byte[] data, int decodePixelWidth)
        {
            using MemoryStream stream = new MemoryStream(data);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = decodePixelWidth;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
    }
}

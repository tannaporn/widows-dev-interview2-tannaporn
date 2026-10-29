using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using widows_dev_interview2.Models;

namespace widows_dev_interview2.Services
{
    public class ApiService
    {
        private static readonly HttpClient SharedHttpClient = new HttpClient(
            new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        private readonly HttpClient httpClient;
        private readonly TimeSpan requestTimeout;
        private readonly JsonParser responseParser = new JsonParser();

        public ApiService(HttpClient? httpClient = null, TimeSpan? requestTimeout = null)
        {
            this.httpClient = httpClient ?? SharedHttpClient;
            this.requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(30);

            if (this.requestTimeout <= TimeSpan.Zero
                || this.requestTimeout.TotalMilliseconds > uint.MaxValue - 1)
            {
                throw new Exception("ตั้งค่าเวลารอไม่ถูกต้อง");
            }
        }

        public async Task<List<MediaItem>> LoadDataAsync(string apiKey,
            DateRange dateRange, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (dateRange == null)
            {
                throw new Exception("ช่วงวันที่ไม่ถูกต้อง");
            }


          
            string startDate = dateRange.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string endDate = dateRange.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string requestUrl = "https://api.nasa.gov/planetary/apod"
                + "?api_key=" + Uri.EscapeDataString(apiKey)
                + "&start_date=" + startDate
                + "&end_date=" + endDate
                + "&thumbs=true";

            using CancellationTokenSource requestCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            requestCancellation.CancelAfter(requestTimeout);

            try
            {
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using HttpResponseMessage response = await httpClient.SendAsync(request,
                    HttpCompletionOption.ResponseHeadersRead, requestCancellation.Token)
                    .ConfigureAwait(false);

                requestCancellation.Token.ThrowIfCancellationRequested();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("โหลดข้อมูลไม่สำเร็จ กรุณาลองใหม่");
                }

                string json = await response.Content.ReadAsStringAsync(requestCancellation.Token)
                    .ConfigureAwait(false);

                List<MediaItem> entries = responseParser.Parse(json, dateRange,
                    requestCancellation.Token);

                requestCancellation.Token.ThrowIfCancellationRequested();
                return entries;
            }
            catch (Exception)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new Exception("โหลดข้อมูลไม่สำเร็จ กรุณาลองใหม่");
            }
        }
    }
}

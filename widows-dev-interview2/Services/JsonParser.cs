using System.Text.Json;
using widows_dev_interview2.Models;

namespace widows_dev_interview2.Services
{
    public class JsonParser
    {
        public List<MediaItem> Parse(string json, DateRange dateRange,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new Exception("ข้อมูลไม่ถูกต้อง");
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(json);

                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    throw new Exception("ข้อมูลไม่ถูกต้อง");
                }

                List<MediaItem> entries = new List<MediaItem>();
                HashSet<DateOnly> receivedDates = new HashSet<DateOnly>();
                JsonSerializerOptions jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                foreach (JsonElement item in document.RootElement.EnumerateArray())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        throw new Exception("ข้อมูลไม่ถูกต้อง");
                    }

                    MediaItem? entry = item.Deserialize<MediaItem>();

                    if (entry == null || string.IsNullOrWhiteSpace(entry.Title)
                        || string.IsNullOrWhiteSpace(entry.Explanation)
                        || string.IsNullOrWhiteSpace(entry.MediaType))
                    {
                        throw new Exception("ข้อมูลไม่ถูกต้อง");
                    }

                    if (entry.Date < dateRange.StartDate || entry.Date > dateRange.EndDate)
                    {
                        throw new Exception("ข้อมูลไม่ถูกต้อง");
                    }

                    if (!receivedDates.Add(entry.Date))
                    {
                        throw new Exception("ข้อมูลไม่ถูกต้อง");
                    }

                    entry.OriginalJson = JsonSerializer.Serialize(item, jsonOptions);
                    entries.Add(entry);
                }

                entries.Sort((first, second) => second.Date.CompareTo(first.Date));
                cancellationToken.ThrowIfCancellationRequested();
                return entries;
            }
            catch (Exception)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new Exception("อ่านข้อมูลไม่สำเร็จ");
            }
        }
    }
}

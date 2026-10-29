using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace widows_dev_interview2.Views
{
    public static class DisplayText
    {
        private static readonly CultureInfo ThaiGregorianCulture = CreateThaiCulture();

        public static string FormatDate(DateOnly date)
        {
            return date.ToString("d MMMM yyyy", ThaiGregorianCulture);
        }

        public static string RemoveHtml(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "";
            }

            try
            {
                string plainText = Regex.Replace(text, @"<br\s*/?>|</p\s*>", "\n",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
                plainText = Regex.Replace(plainText, "<[^>]*>", "",
                    RegexOptions.None, TimeSpan.FromMilliseconds(200));
                return WebUtility.HtmlDecode(plainText).Trim();
            }
            catch (Exception)
            {
                return WebUtility.HtmlDecode(text).Trim();
            }
        }

        private static CultureInfo CreateThaiCulture()
        {
            CultureInfo culture = new CultureInfo("th-TH");
            culture.DateTimeFormat.Calendar = new GregorianCalendar();
            return culture;
        }
    }
}

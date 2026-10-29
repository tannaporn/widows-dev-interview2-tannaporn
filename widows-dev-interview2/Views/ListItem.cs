using widows_dev_interview2.Models;

namespace widows_dev_interview2.Views
{
    public class ListItem
    {
        public MediaItem Entry { get; }
        public string Title { get; }
        public string DateText { get; }
        public string MediaLabel { get; }

        public ListItem(MediaItem entry)
        {
            Entry = entry;
            Title = DisplayText.RemoveHtml(entry.Title);
            DateText = DisplayText.FormatDate(entry.Date);

            if (entry.MediaType == "image")
            {
                MediaLabel = "ภาพ";
            }
            else if (entry.MediaType == "video" || entry.MediaType == "iframe")
            {
                MediaLabel = "วิดีโอ / สื่อภายนอก";
            }
            else
            {
                MediaLabel = "สื่อภายนอก";
            }
        }
    }
}

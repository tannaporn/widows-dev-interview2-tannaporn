using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using widows_dev_interview2.Models;
using widows_dev_interview2.Services;
using widows_dev_interview2.Views;

namespace widows_dev_interview2
{
    public partial class MainWindow : Window
    {
        private readonly SettingsService settingsService = new SettingsService();
        private readonly DateService dateService = new DateService();
        private readonly ApiService apiService = new ApiService();
        private readonly ImageService imageService = new ImageService();

        private CancellationTokenSource? loadCancellation;
        private CancellationTokenSource? detailImageCancellation;
        private string currentApiKey = "";
        private int requestVersion;
        private int selectionVersion;
        private bool settingsNeedRepair;
        private bool isReady;
        private bool isClosing;

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            SetDateOptions();
            isReady = true;
            UpdateControls();
            UpdateLayoutMode();
        }

        private void SetDateOptions()
        {
            MonthSelector.ItemsSource = new string[]
            {
                "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน",
                "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม",
                "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม"
            };

            try
            {
                DateOnly today = dateService.GetEasternToday(DateTimeOffset.UtcNow);
                List<int> years = new List<int>();

                for (int year = today.Year; year >= 1995; year--)
                {
                    years.Add(year);
                }

                YearSelector.ItemsSource = years;
                YearSelector.SelectedItem = today.Year;
                MonthSelector.SelectedIndex = today.Month - 1;
                ShowToday(today);
            }
            catch (Exception)
            {
                SetStatus("อ่านวันที่ไม่สำเร็จ", true);
            }
        }

        private void LoadSettings()
        {
            try
            {
                currentApiKey = settingsService.Load().ApiKey;

                if (currentApiKey.Length > 0)
                {
                    SetStatus("อ่าน key ที่บันทึกไว้แล้ว เลือกเดือนและกดโหลดข้อมูล");
                }
            }
            catch (Exception)
            {
                settingsNeedRepair = true;
                SetStatus("อ่านการตั้งค่าไม่สำเร็จ", true);
            }
        }

        private void EditKey_Click(object sender, RoutedEventArgs e)
        {
            bool wasLoading = loadCancellation != null;
            CancelLoad();
            CancelImage();

            if (wasLoading)
            {
                SetStatus("ยกเลิกแล้ว");
            }

            KeyWindow keyWindow = new KeyWindow(settingsService, currentApiKey);
            keyWindow.Owner = this;

            if (keyWindow.ShowDialog() == true)
            {
                currentApiKey = keyWindow.SavedApiKey;
                settingsNeedRepair = false;
                SetStatus("บันทึก key แล้ว กดโหลดข้อมูลเพื่อเรียก API");
            }

            UpdateControls();
        }

        private void DeleteKey_Click(object sender, RoutedEventArgs e)
        {
            CancelLoad();
            CancelImage();

            try
            {
                settingsService.DeleteKey();
                currentApiKey = "";
                settingsNeedRepair = false;
                SetStatus("ลบ key แล้ว ข้อมูลที่โหลดไว้ยังเปิดดูได้ เพิ่ม key ใหม่ก่อนโหลดครั้งถัดไป");
            }
            catch (Exception)
            {
                SetStatus("ลบ key ไม่สำเร็จ", true);
            }

            UpdateControls();
        }

        private void DateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isReady || isClosing)
            {
                return;
            }

            CancelLoad();

            try
            {
                GetDateRange();
                SetStatus(currentApiKey.Length == 0
                    ? "เพิ่ม API key ก่อนโหลดข้อมูล"
                    : "กดโหลดข้อมูลเพื่อดูเดือนที่เลือก ข้อมูลที่แสดงยังเป็นชุดเดิม");
            }
            catch (Exception)
            {
                SetStatus("วันที่ไม่ถูกต้อง", true);
            }

            UpdateControls();
        }

        private DateRange GetDateRange()
        {
            if (YearSelector.SelectedItem is not int year || MonthSelector.SelectedIndex < 0)
            {
                throw new Exception("กรุณาเลือกเดือนและปี");
            }

            DateOnly today = dateService.GetEasternToday(DateTimeOffset.UtcNow);
            ShowToday(today);
            return dateService.GetMonthRange(year, MonthSelector.SelectedIndex + 1, today);
        }

        private void ShowToday(DateOnly today)
        {
            EasternDateText.Text = "วันที่ปัจจุบันตาม US Eastern: " + DisplayText.FormatDate(today)
                + " · APOD เริ่ม 16 มิถุนายน ค.ศ. 1995";
        }

        private async void LoadData_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(currentApiKey))
            {
                SetStatus("กรุณากรอก key", true);
                return;
            }

            DateRange dateRange;

            try
            {
                dateRange = GetDateRange();
            }
            catch (Exception)
            {
                SetStatus("วันที่ไม่ถูกต้อง", true);
                UpdateControls();
                return;
            }

            CancelLoad();
            int currentVersion = requestVersion;
            CancellationTokenSource requestCancellation = new CancellationTokenSource();
            loadCancellation = requestCancellation;
            UpdateControls();
            SetStatus("กำลังโหลดข้อมูลจาก NASA สามารถกดยกเลิกหรือเลือกเดือนใหม่ได้");

            try
            {
                List<MediaItem> entries = await apiService.LoadDataAsync(
                    currentApiKey, dateRange, requestCancellation.Token);

                if (!IsLatestRequest(currentVersion, requestCancellation.Token))
                {
                    return;
                }

                List<ListItem> items = new List<ListItem>();

                foreach (MediaItem entry in entries)
                {
                    items.Add(new ListItem(entry));
                }

                EntriesList.ItemsSource = items;
                DisplayedPeriodText.Text = DisplayText.FormatDate(dateRange.StartDate)
                    + " – " + DisplayText.FormatDate(dateRange.EndDate)
                    + " · " + items.Count + " รายการ";

                if (items.Count == 0)
                {
                    ListEmptyTitle.Text = "ไม่พบข้อมูลในช่วงที่เลือก";
                    ListEmptyMessage.Text = "ลองเลือกเดือนอื่น แล้วกดโหลดข้อมูล";
                    ListEmptyPanel.Visibility = Visibility.Visible;
                    SetStatus("ไม่มีรายการในช่วงวันที่ที่เลือก");
                    return;
                }

                ListEmptyPanel.Visibility = Visibility.Collapsed;
                EntriesList.SelectedIndex = 0;
                SetStatus("โหลดสำเร็จ " + items.Count + " รายการ เรียงจากวันที่ใหม่สุด");
            }
            catch (Exception)
            {
                if (currentVersion == requestVersion && !isClosing)
                {
                    if (requestCancellation.IsCancellationRequested)
                    {
                        SetStatus("ยกเลิกแล้ว");
                    }
                    else
                    {
                        SetStatus("โหลดข้อมูลไม่สำเร็จ", true);
                    }
                }
            }
            finally
            {
                if (ReferenceEquals(loadCancellation, requestCancellation))
                {
                    loadCancellation = null;

                    if (!isClosing)
                    {
                        UpdateControls();
                    }
                }

                requestCancellation.Dispose();
            }
        }

        private bool IsLatestRequest(int version, CancellationToken token)
        {
            return !isClosing && version == requestVersion && !token.IsCancellationRequested;
        }

        private void CancelLoad_Click(object sender, RoutedEventArgs e)
        {
            CancelLoad();
            CancelImage();
            SetStatus("ยกเลิกแล้ว");
        }

        private void CancelLoad()
        {
            requestVersion++;
            loadCancellation?.Cancel();
            loadCancellation = null;

            if (isReady && !isClosing)
            {
                UpdateControls();
            }
        }

        private async void ItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (!isReady || isClosing)
            {
                return;
            }

            CancelImage();
            DetailImage.Source = null;
            DetailImage.Visibility = Visibility.Collapsed;

            if (EntriesList.SelectedItem is not ListItem item)
            {
                DetailsScroll.Visibility = Visibility.Collapsed;
                NoSelectionText.Visibility = Visibility.Visible;
                return;
            }

            MediaItem entry = item.Entry;
            NoSelectionText.Visibility = Visibility.Collapsed;
            DetailsScroll.Visibility = Visibility.Visible;
            DetailsScroll.ScrollToTop();
            DetailDateText.Text = item.DateText + " · " + item.MediaLabel;
            DetailTitleText.Text = item.Title;
            ExplanationText.Text = DisplayText.RemoveHtml(entry.Explanation);
            SetMediaLink(OriginalMediaButton, entry.Url);
            SetMediaLink(HighResolutionButton, entry.HighResolutionUrl);
            DetailImagePlaceholder.Text = "กำลังโหลดภาพตัวอย่าง";
            DetailImagePlaceholder.Visibility = Visibility.Visible;

            int currentSelection = selectionVersion;
            CancellationTokenSource imageCancellation = new CancellationTokenSource();
            detailImageCancellation = imageCancellation;
            UpdateControls();

            try
            {
                List<string?> previewUrls = new List<string?> { GetPreviewUrl(entry) };

                if (entry.MediaType == "image")
                {
                    previewUrls.Add(entry.HighResolutionUrl);
                }

                previewUrls.Add(entry.ThumbnailUrl);
                HashSet<string> triedUrls = new HashSet<string>();
                BitmapSource? image = null;

                foreach (string? url in previewUrls)
                {
                    if (string.IsNullOrWhiteSpace(url) || !triedUrls.Add(url))
                    {
                        continue;
                    }

                    image = await imageService.LoadAsync(url, 1200, imageCancellation.Token);

                    if (image != null)
                    {
                        break;
                    }
                }

                if (isClosing || currentSelection != selectionVersion || imageCancellation.IsCancellationRequested)
                {
                    return;
                }

                if (image == null)
                {
                    DetailImagePlaceholder.Text = "ไม่มีภาพตัวอย่าง";
                }
                else
                {
                    DetailImage.Source = image;
                    DetailImage.Visibility = Visibility.Visible;
                    DetailImagePlaceholder.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
                if (!isClosing && currentSelection == selectionVersion
                    && !imageCancellation.IsCancellationRequested)
                {
                    DetailImagePlaceholder.Text = "โหลดภาพไม่สำเร็จ";
                }
            }
            finally
            {
                if (ReferenceEquals(detailImageCancellation, imageCancellation))
                {
                    detailImageCancellation = null;

                    if (!isClosing)
                    {
                        UpdateControls();
                    }
                }

                imageCancellation.Dispose();
            }
        }

        private void CancelImage()
        {
            selectionVersion++;
            detailImageCancellation?.Cancel();
            detailImageCancellation = null;

           

            if (isReady && !isClosing)
            {
                UpdateControls();
            }
        }

        private static string? GetPreviewUrl(MediaItem entry)
        {
            if (entry.MediaType != "image")
            {
                return entry.ThumbnailUrl;
            }

            Uri? mediaUri = ImageService.GetHttpsUri(entry.Url);

            if (mediaUri != null)
            {
                string extension = Path.GetExtension(mediaUri.AbsolutePath).ToLowerInvariant();

                if (extension == ".jpg" || extension == ".jpeg" || extension == ".png"
                    || extension == ".gif" || extension == ".bmp" || extension == ".tif"
                    || extension == ".tiff" || extension == ".webp")
                {
                    return entry.Url;
                }
            }

            if (ImageService.GetHttpsUri(entry.HighResolutionUrl) != null)
            {
                return entry.HighResolutionUrl;
            }

            return entry.Url;
        }

        private static void SetMediaLink(Button button, string? url)
        {
            Uri? uri = ImageService.GetHttpsUri(url);
            button.Tag = uri;
            button.IsEnabled = uri != null;
            button.ToolTip = uri == null ? "ไม่ได้ส่งลิงก์ HTTPS ที่ใช้งานได้" : uri.AbsoluteUri;
        }

        private void OpenMedia_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not Uri uri
                || ImageService.GetHttpsUri(uri.AbsoluteUri) == null)
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception)
            {
                SetStatus("เปิดลิงก์ไม่สำเร็จ", true);
            }
        }

        private void UpdateControls()
        {
            bool hasKey = currentApiKey.Length > 0;
            bool validDate = false;

            try
            {
                GetDateRange();
                validDate = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                validDate = false;
            }

            if (settingsNeedRepair)
            {
                ApiKeyStatusText.Text = "อ่าน key ไม่สำเร็จ";
            }
            else if (hasKey)
            {
                ApiKeyStatusText.Text = "มี API key";
            }
            else
            {
                ApiKeyStatusText.Text = "ยังไม่มี API key";
            }
            EditApiKeyButton.Content = hasKey ? "แก้ไข key" : "เพิ่ม key";
            DeleteApiKeyButton.IsEnabled = hasKey || settingsNeedRepair;
            LoadButton.IsEnabled = hasKey && validDate;
            LoadButton.Content = loadCancellation == null ? "โหลดข้อมูล" : "โหลดใหม่";
            CancelButton.IsEnabled = loadCancellation != null || detailImageCancellation != null;
            LoadingProgress.Visibility = loadCancellation == null ? Visibility.Collapsed : Visibility.Visible;
        }

        private void SetStatus(string message, bool isError = false)
        {
            StatusText.Text = message;
            StatusText.Foreground = new SolidColorBrush(isError
                ? Color.FromRgb(153, 27, 27) : Color.FromRgb(23, 54, 93));
            StatusBorder.Background = new SolidColorBrush(isError
                ? Color.FromRgb(254, 242, 242) : Color.FromRgb(232, 240, 252));
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (isReady)
            {
                UpdateLayoutMode();
            }
        }

        private void UpdateLayoutMode()
        {
            if (ActualWidth >= 900)
            {
                ListColumn.Width = new GridLength(320);
                GapColumn.Width = new GridLength(16);
                DetailsColumn.Width = new GridLength(1, GridUnitType.Star);
                ListRow.Height = new GridLength(1, GridUnitType.Star);
                GapRow.Height = new GridLength(0);
                DetailsRow.Height = new GridLength(0);
                Grid.SetRow(DetailsPanel, 0);
                Grid.SetColumn(DetailsPanel, 2);
                DetailImageFrame.Height = 240;
            }
            else
            {
                ListColumn.Width = new GridLength(1, GridUnitType.Star);
                GapColumn.Width = new GridLength(0);
                DetailsColumn.Width = new GridLength(0);
                ListRow.Height = new GridLength(165);
                GapRow.Height = new GridLength(12);
                DetailsRow.Height = new GridLength(1, GridUnitType.Star);
                Grid.SetRow(DetailsPanel, 2);
                Grid.SetColumn(DetailsPanel, 0);
                DetailImageFrame.Height = 180;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            isClosing = true;
            CancelLoad();
            CancelImage();
            currentApiKey = "";
            base.OnClosed(e);
        }
    }
}

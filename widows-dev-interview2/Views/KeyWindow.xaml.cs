using System.Windows;
using System.Windows.Media;
using widows_dev_interview2.Services;

namespace widows_dev_interview2.Views
{
    public partial class KeyWindow : Window
    {
        private readonly SettingsService settingsService;

        public string SavedApiKey { get; private set; } = "";

        public KeyWindow(SettingsService settingsService, string currentApiKey)
        {
            InitializeComponent();
            this.settingsService = settingsService;
            ApiKeyInput.Password = currentApiKey;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApiKeyInput.Focus();
        }

        private void ImportKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string environmentKey = settingsService.ReadEnvironmentKey();

                if (string.IsNullOrWhiteSpace(environmentKey))
                {
                    ShowMessage("ไม่พบ key", true);
                    return;
                }

                ApiKeyInput.Password = environmentKey;
                ShowMessage("นำเข้าแล้ว กดบันทึก key เพื่อใช้งาน", false);
            }
            catch (Exception)
            {
                ShowMessage("นำเข้า key ไม่สำเร็จ", true);
            }
        }

        private void SaveKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                settingsService.SaveKey(ApiKeyInput.Password);
                SavedApiKey = ApiKeyInput.Password.Trim();
                ApiKeyInput.Clear();
                DialogResult = true;
            }
            catch (Exception)
            {
                ShowMessage("บันทึก key ไม่สำเร็จ", true);
            }
        }

        private void ShowMessage(string message, bool isError)
        {
            KeyMessageText.Text = message;
            KeyMessageText.Foreground = isError ? Brushes.Firebrick : Brushes.DarkSlateBlue;
            KeyMessageText.Visibility = Visibility.Visible;
        }

        protected override void OnClosed(EventArgs e)
        {
            ApiKeyInput.Clear();
            base.OnClosed(e);
        }
    }
}

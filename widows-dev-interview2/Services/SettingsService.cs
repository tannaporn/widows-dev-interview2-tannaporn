using System.IO;
using System.Text.Json;
using widows_dev_interview2.Configuration;

namespace widows_dev_interview2.Services
{
    public class SettingsService
    {
        public string SettingsFilePath { get; }

        public SettingsService(string? settingsFilePath = null)
        {
            if (settingsFilePath != null)
            {
                SettingsFilePath = Path.GetFullPath(settingsFilePath);
                return;
            }

            string localAppData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

            SettingsFilePath = Path.Combine(
                localAppData, "widows-dev-interview2", "appsettings.json");
        }

        public AppSettings Load()
        {
            if (!File.Exists(SettingsFilePath))
            {
                return new AppSettings();
            }

            string json = File.ReadAllText(SettingsFilePath);
            AppSettings? settings;

            try
            {
                settings = JsonSerializer.Deserialize<AppSettings>(json);
            }
            catch (Exception)
            {
                throw new Exception("อ่านการตั้งค่าไม่สำเร็จ");
            }

            settings.ApiKey = settings.ApiKey.Trim();



            return settings;
        }

        public void SaveKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception("กรุณากรอก key");
            }

            string trimmedKey = apiKey.Trim();

            try
            {
                Save(new AppSettings { ApiKey = trimmedKey });
            }
            catch (Exception e)
            {
                throw new Exception("บันทึกไม่สำเร็จ");
            }
        }

        public void DeleteKey()
        {
            Save(new AppSettings());
        }

        public string ReadEnvironmentKey()
        {
            string? apiKey = Environment.GetEnvironmentVariable("NASA_API_KEY");
            return apiKey?.Trim() ?? "";
        }

        private void Save(AppSettings settings)
        {
            string directory = Path.GetDirectoryName(SettingsFilePath)!;
            Directory.CreateDirectory(directory);

            string json = JsonSerializer.Serialize(settings,
                new JsonSerializerOptions { WriteIndented = true });

            string temporaryFilePath = SettingsFilePath + ".tmp";

            try
            {
                File.WriteAllText(temporaryFilePath, json);
                File.Move(temporaryFilePath, SettingsFilePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryFilePath))
                {
                    File.Delete(temporaryFilePath);
                }
            }
        }


    }
}

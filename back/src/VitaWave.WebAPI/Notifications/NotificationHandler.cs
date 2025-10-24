using Serilog;
using System.Text.Json;
using VitaWave.Common;
using VitaWave.Data;

namespace VitaWave.WebAPI.Notifications
{
    public class NotificationHandler
    {
        private static readonly string _folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "vitawave"
        );

        const string FILE_NAME = "text_settings.json";
        const int MAX_NUMBER_SENDS = 3;

        static int current_num_sends = 0;
        private TextSettings settings;

        public NotificationHandler(DataFacilitator dataProcessor)
        {
            dataProcessor.EventRaise += DataProcessor_EventRaise;

            if (!Directory.Exists(_folder))
            {
                Directory.CreateDirectory(_folder);
            }

            var filePath = Path.Combine(_folder, FILE_NAME);
            if (!File.Exists(filePath))
            {
                settings = new TextSettings();
                var json = JsonSerializer.Serialize(settings);
                File.WriteAllText(filePath, json);
                Log.Information($"Default file for text messaging saved at {filePath}. Add name, phone, and key. The service bool must be set to true as well. Service can be off.");
            }
            else
            {
                settings = JsonSerializer.Deserialize<TextSettings>(File.ReadAllText(filePath)) ?? new TextSettings();
            }
        }

        private void DataProcessor_EventRaise(object? sender, Common.ResultEvent e)
        {
            if(!e.IsWorthNotifing)
                return;

            Log.Debug("Notification event raised!");

            if (settings.ServiceOn &&
                !string.IsNullOrEmpty(settings.ApiKey) &&
                Interlocked.Increment(ref current_num_sends) <= MAX_NUMBER_SENDS)
            {
                SendMessage(e);
            }
        }

        private async void SendMessage(ResultEvent e)
        {
            if (!settings.ServiceOn || current_num_sends > MAX_NUMBER_SENDS)
                return;

            Log.Debug("Sending Critical Text Message");

            var message = $"FROM VITAWAVE: Hello {settings.Name}, we believe a severe event has taken place. Please check on your person(s).";

            try
            {
                using var http = new HttpClient();
                var values = new Dictionary<string, string>
                {
                    ["phone"] = settings.Phone,
                    ["message"] = message,
                    ["key"] = settings.ApiKey
                };
                var content = new FormUrlEncodedContent(values);
                var response = await http.PostAsync("https://textbelt.com/text", content);
                var result = await response.Content.ReadAsStringAsync();
                Log.Information($"Textbelt response: {result}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error trying to send text message.");
            }
        }
    }


    public class TextSettings
    {
        public bool ServiceOn = false;
        public string ApiKey = "";
        public string Name = "Ashton Esquivel";
        public string Phone = "10digitphonenumber";
    }
}

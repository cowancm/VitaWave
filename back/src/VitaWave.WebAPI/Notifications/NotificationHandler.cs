using Serilog;
using System.Text.Json;
using VitaWave.Common;
using VitaWave.Data;
using VitaWave.WebAPI.Settings;

namespace VitaWave.WebAPI.Notifications
{
    public class NotificationHandler
    {
        private static readonly string _folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "vitawave"
        );

        const int MAX_NUMBER_SENDS = 3;
        static int current_num_sends = 0;

        private TextSettings settings;

        public NotificationHandler(DataFacilitator dataProcessor)
        {
            dataProcessor.EventRaise += DataProcessor_EventRaise;
            settings = SettingsManager.GetSettings().TextSettings;
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
}

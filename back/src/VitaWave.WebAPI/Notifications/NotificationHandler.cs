using Serilog;
using System.Text.Json;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using VitaWave.Data;

namespace VitaWave.WebAPI.Notifications
{
    public class NotificationHandler
    {
        private static readonly string _folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "vitawave"
        );

        const string FILE_NAME = "twilio_settings.json";
        TwilioCredentials _creds = new();

        public NotificationHandler(DataProcessor dataProcessor)
        {
            dataProcessor.EventRaise += DataProcessor_EventRaise;

            if (!Directory.Exists(_folder))
            {
                Directory.CreateDirectory(_folder);
            }

            var filePath = Path.Combine(_folder, FILE_NAME);
            if (!File.Exists(filePath))
            {
                // Create example JSON file
                var example = new
                {
                    ServiceOn = false,
                    AccountSid = "YOUR_ACCOUNT_SID",
                    AuthToken = "YOUR_AUTH_TOKEN",
                    SenderPhoneNumber = "123456789"
                };

                File.WriteAllText(filePath, JsonSerializer.Serialize(example, new JsonSerializerOptions { WriteIndented = true }));

                Log.Information($"Empty auth JSON file created at: {filePath}\n" +
                                "Fill in your AccountSid and AuthToken, enable service then restart the application.\n" +
                                "Leave service off and messages will just be logged in the console window.");
            }

            // Read JSON
            var jsonText = File.ReadAllText(filePath);
            var creds = JsonSerializer.Deserialize<TwilioCredentials>(jsonText);

            if(creds == null || !creds.ServiceOn)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(creds.AccountSid) || string.IsNullOrWhiteSpace(creds.AuthToken) || string.IsNullOrWhiteSpace(creds.SenderPhoneNumber))
            {
                Log.Error("Twilio credentials are missing or invalid. Please check the JSON file.");
                //Environment.Exit(1);
            }

            TwilioClient.Init(creds.AccountSid, creds.AuthToken);
            _creds = creds;

        }

        private void DataProcessor_EventRaise(object? sender, Common.ResultEvent e)
        {
            Log.Debug("Notification event raised!");

            if (e.Severity > 4)
            {
                var message = new Message()
                {
                    Event = e
                };
                SendMessage(new Recipient(), message);
            }
        }

        private void SendMessage(Recipient rcpt, Message msg)
        {
            var messageBody = $"Hello {rcpt.FirstName} {rcpt.LastName}. The following severe event took place \"{msg.Event}\" at ${msg.Event.DateTimeString}.";

            Log.Debug("SendMessage() Call: \n" + messageBody);

            if (!_creds.ServiceOn)
            {
                return;
            }

            var message = MessageResource.Create(
                body: messageBody,
                from: new PhoneNumber(_creds.SenderPhoneNumber),
                to: new PhoneNumber(rcpt.PhoneNumber)
            );
        }
    }

    public class TwilioCredentials
    {
        public bool ServiceOn = false;
        public string AccountSid { get; set; } = "";
        public string AuthToken { get; set; } = "";
        public string SenderPhoneNumber { get; set; } = "";
    }
}

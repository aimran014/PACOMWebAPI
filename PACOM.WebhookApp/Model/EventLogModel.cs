using Newtonsoft.Json;
using PACOM.WebhookApp.Service;

namespace PACOM.WebhookApp.Model
{
    public class EventLogModel
    {
        public string Version { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string? ScopeName { get; set; }
        public string? OrganizationName { get; set; }
        public string? EventId { get; set; }
        public string? EventName { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? CredentialId { get; set; }
        public string? CredentialNumber { get; set; }
        public string? Value { get; set; }
        public string? AreaFromId { get; set; }
        public string? AreaToId { get; set; }
        public string CustomDataUDF { get; set; } = string.Empty;
        public string CustomDataString { get; set; } = string.Empty;
        public DateTime UtcTime { get; set; }
        public DateTime LocalTime
        {
            get
            {
                // Malaysia time zone (same as Singapore)
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(UtcTime, tz);
            }
        }
        public string ReaderName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CustomDataString))
                    return string.Empty;

                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(CustomDataString);

                // Check if there are at least 3 items
                if (dict != null && dict.Count >= 3)
                {
                    // Get the 3rd item (index 2)
                    //return dict.ElementAt(2).Value;

                    var readerNameKey = dict.ElementAt(2).Value;

                    // Find the matching reader by Id
                    var reader = DatasourcesService.GetColumnNameById(readerNameKey, Scope);

                    return reader ?? string.Empty;
                }

                return string.Empty;
            }
        }

        public string MykadNumber
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CustomDataUDF))
                    return string.Empty;

                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(CustomDataUDF);
                if (dict == null)
                    return string.Empty;

                const string mykadKey = "7c18b5f2-e234-4ab0-a57d-6803910e6683"; // MyKad key
                return dict.TryGetValue(mykadKey, out string value) ? value : string.Empty;
            }
        }


        // 🆕 Add this property:
        [JsonIgnore] // to prevent double serialization if you return it as JSON later
        public Dictionary<string, string> CustomDataEventType
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CustomDataString))
                    return new Dictionary<string, string>();

                try
                {
                    return JsonConvert.DeserializeObject<Dictionary<string, string>>(CustomDataString)
                           ?? new Dictionary<string, string>();
                }
                catch
                {
                    return new Dictionary<string, string>();
                }
            }
        }

        // 🆕 Add this property:
        [JsonIgnore] // to prevent double serialization if you return it as JSON later
        public Dictionary<string, string> CustomDataUDFType
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CustomDataUDF))
                    return new Dictionary<string, string>();

                try
                {
                    return JsonConvert.DeserializeObject<Dictionary<string, string>>(CustomDataUDF)
                           ?? new Dictionary<string, string>();
                }
                catch
                {
                    return new Dictionary<string, string>();
                }
            }
        }

    }
}

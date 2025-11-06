using Newtonsoft.Json;
using PACOM.WebApp.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PACOM.WebApp.Models
{
    public class EventLogModel
    {
        public string Id { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string? ScopeName { get; set; }
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
        public DateTime MalaysiaTime { get; set; }
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


using Newtonsoft.Json;
using PACOM.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace PacomLibrary.Models
{
    public class UsersModel
    {
        public string Id { get; set; }
        public string Scope { get; set; }
        public string ScopeName { get; set; }
        public string Username { get; set; }
        public int UserType { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ImageId { get; set; }
        public int isDelete { get; set; }
        public string CustomData { get; set; }

        // 🆕 Add this property:
        [JsonIgnore] // to prevent double serialization if you return it as JSON later
        public Dictionary<string, string> CustomDataUDF
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CustomData))
                    return new Dictionary<string, string>();

                try
                {
                    return JsonConvert.DeserializeObject<Dictionary<string, string>>(CustomData)
                           ?? new Dictionary<string, string>();
                }
                catch
                {
                    return new Dictionary<string, string>();
                }
            }
        }

        [JsonIgnore] // to prevent double serialization if you return it as JSON later
        public Dictionary<string, string> NewCustomDataUDF
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CustomData))
                    return new Dictionary<string, string>();

                Dictionary<string, string> dict;
                try
                {
                    dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(CustomData)
                           ?? new Dictionary<string, string>();
                }
                catch
                {
                    return new Dictionary<string, string>();
                }

                // Step 2: Convert GUID keys into readable column names
                // Step 2: Convert GUID keys into readable column names
                //if (dict == null || dict.Count == 0)
                //    return new();

                //// Fast projection using LINQ
                //return dict.ToDictionary(
                //    kvp => PacomIntegration.GetColumnNameById(kvp.Key, Scope) ?? kvp.Key,
                //    kvp => kvp.Value
                //);
                var newDict = new Dictionary<string, string>();

                foreach (var kvp in dict)
                {
                    // Get column name from integration
                    var columnName = PacomIntegration.GetColumnNameById(kvp.Key, Scope);

                    // Add readable name with original value
                    newDict[columnName ?? kvp.Key] = kvp.Value;
                }

                return newDict;
            }
        }


    }
}

using Microsoft.Identity.Client;
using Newtonsoft.Json;
using PACOM.Services;

namespace PacomLibrary.Models
{
    public class PacomModels
    {

        public class ObjectMetaData
        {
            public string Id { get; set; }
            public string? Scope { get; set; }
            public string? Name { get; set; }
            public string? ObjectTypeId { get; set; }
            public string? AliasTypeId { get; set; }
        }
    }
}

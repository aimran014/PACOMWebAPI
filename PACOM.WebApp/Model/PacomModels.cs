
namespace PACOM.WebApp.Models
{
    public class PacomModels
    {

        public class ObjectMetaData
        {
            public string Id { get; set; } = string.Empty;
            public string? Scope { get; set; }
            public string? Name { get; set; }
            public string? ObjectTypeId { get; set; }
            public string? AliasTypeId { get; set; }
        }
    }
}

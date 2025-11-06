namespace PACOM.WebApp.Models
{
    public class PacomResponse<T>
    {
        public int Error { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

    }
}

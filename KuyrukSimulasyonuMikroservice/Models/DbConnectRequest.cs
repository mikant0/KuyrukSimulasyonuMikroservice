namespace KuyrukSimulasyonuMikroservice.Models
{
    public class DbConnectRequest
    {
        public string Server { get; set; } = "localhost";
        public string Database { get; set; } = "KuyrukDB";
        public string UserId { get; set; } = "";
        public string Password { get; set; } = "";
        public string Table { get; set; } = "KuyrukKaydi"; 
    }
}

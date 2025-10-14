namespace KuyrukSimulasyonuMikroservice.Models
{
    public class QueueRecord
    {
        public string PointId { get; set; } = "BN01"; //Bekleme noktası
        public DateTime Timestamp { get; set; } //Tarih + Saat
        public int DurationMin { get; set; } // Dakika

    }
}

namespace KuyrukSimulasyonuMikroservice.Models
{
    public class AddRecordRequest : DbConnectRequest
    {
           public QueueRecord Record { get; set; } = new QueueRecord();
    }
}

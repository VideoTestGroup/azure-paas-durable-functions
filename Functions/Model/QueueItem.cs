namespace ImageIngest.Functions.Model;

[DataContract]
public class QueueItem
{
    [DataMember]
    public int deliveryCount { get; set; }
    [DataMember]
    public DateTime enqueuedTimeUtc { get; set; }
    [DataMember]
    public string messageId { get; set; }
}

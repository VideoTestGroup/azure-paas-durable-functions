namespace ImageIngest.Functions.Model;

public class QueueItem
{
    public int deliveryCount { get; set; }
    public DateTime enqueuedTimeUtc { get; set; }
    public string messageId { get; set; }
}

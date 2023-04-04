using Azure.Messaging.EventGrid;

namespace ImageIngest.Functions.Model;

[DataContract]
public class FileLog
{
    public string id { get; set; }
    public string name { get; set; }
    public string container { get; set; }
    public DateTime timestamp { get; set; }
    public int deliveryCount { get; set; }
    public EventGridEvent eventGrid { get; set; }
    public BlobProperties properties { get; set; }
    public QueueItem queueItem { get; set; } 
    public BlobTags tags { get; set; }

    public FileLog(string id, int deliveryCount)
    {
        this.id = id;
        name = id;
        timestamp = DateTime.UtcNow;
        this.deliveryCount = deliveryCount;
    }
}

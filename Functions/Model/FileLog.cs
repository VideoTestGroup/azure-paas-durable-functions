namespace ImageIngest.Functions.Model;

[DataContract]
public class FileLog
{
    [DataMember]
    public string id { get; set; }
    [DataMember]
    public string name { get; set; }
    [DataMember]
    public string container { get; set; }
    [DataMember]
    public DateTime timestamp { get; set; }
    [DataMember]
    public int deliveryCount { get; set; }
    [DataMember]
    public EventGridItem eventGrid { get; set; }
    [DataMember]
    public BlobProperties properties { get; set; }
    [DataMember]
    public QueueItem queueItem { get; set; } 
    [DataMember]
    public BlobTags tags { get; set; }

    public FileLog(string id, int deliveryCount)
    {
        this.id = id;
        name = DateTime.Today.ToString("yyyy-MM-dd");
        timestamp = DateTime.UtcNow;
        this.deliveryCount = deliveryCount;
    }
}

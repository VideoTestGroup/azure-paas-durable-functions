using System.Text.Json;
using Azure.Messaging.EventGrid;

namespace ImageIngest.Functions.Model;

[DataContract]
public class FileLog
{
    public string id { get; set; }
    public string name { get; set; }
    public string container { get; set; }
    public string eventGrid { get; set; }
    public string properties { get; set; }
    public string tags { get; set; }
    public string tags { get; set; }

    public FileLog(string id, int deliveryCount)
    {
        this.id = id;
        name = id;
        timestamp = DateTime.UtcNow;
        this.deliveryCount = deliveryCount;
    }
}

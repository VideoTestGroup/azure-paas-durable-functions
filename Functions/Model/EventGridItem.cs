namespace ImageIngest.Functions.Model;

[DataContract]
public class EventGridItem
{
    public string Id { get; set; }
    public string Topic { get; set; }
    public string Subject { get; set; }
    public string EventType { get; set; }
}

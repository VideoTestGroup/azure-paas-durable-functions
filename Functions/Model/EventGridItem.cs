namespace ImageIngest.Functions.Model;

[DataContract]
public class EventGridItem
{
    [DataMember(Name = "id")]
    public string Id { get; set; }
    [DataMember(Name = "topic")]
    public string Topic { get; set; }
    [DataMember(Name = "subject")]
    public string Subject { get; set; }
    [DataMember(Name = "eventType")]
    public string EventType { get; set; }
}

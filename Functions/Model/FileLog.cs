using System.Text.Json;
using Azure.Messaging.EventGrid;

namespace ImageIngest.Functions.Model;

public class FileLog
{
    public string id { get; set; }
    public string name { get; set; }
    public string container { get; set; }
    public DateTime timestamp { get; set; }
    public string eventGrid { get; set; }
    public string properties { get; set; }
    public string tags { get; set; }

    public FileLog(string id)
    {
        this.id = id;
        name = id;
        timestamp = DateTime.UtcNow;
    }

    public static string ConvertEvent(EventGridEvent blobEvent)
    {
        // Convert the EventGridEvent object into a JSON string            
        string eventGridEventJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            Id = blobEvent.Id,
            EventType = blobEvent.EventType,
            Subject = blobEvent.Subject,
            EventTime = blobEvent.EventTime,
            DataVersion = blobEvent.DataVersion
        }, new JsonSerializerOptions { WriteIndented = true });

        // Print the JSON string
        return eventGridEventJson;
    }

    public static string ConvertProperties(BlobProperties blobProperties)
    {
        // Convert the BlobProperties object into a JSON string            
        string propertiesJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            Size = blobProperties.ContentLength,
            ContentSettings = new
            {
                ContentType = blobProperties.ContentType,
                ContentEncoding = blobProperties.ContentEncoding,
                ContentLanguage = blobProperties.ContentLanguage,
                ContentMD5 = blobProperties.ContentHash,
                CacheControl = blobProperties.CacheControl,
                ContentDisposition = blobProperties.ContentDisposition
            },
            LastModified = blobProperties.LastModified,
            ETag = blobProperties.ETag.ToString(),
            LeaseStatus = blobProperties.LeaseStatus.ToString(),
            LeaseState = blobProperties.LeaseState.ToString(),
            LeaseDuration = blobProperties.LeaseDuration.ToString()
        }, new JsonSerializerOptions { WriteIndented = true });

        // Print the JSON string
        return propertiesJson;
    }

    public static string ConvertTags(BlobTags blobTags)
    {
        // Convert the BlobTags object into a JSON string            
        string tagsJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            Name = blobTags.Name,
            Created = blobTags.Created.ToString(),
            Modified = blobTags.Modified.ToString(),
            Status = blobTags.Status.ToString(),
            Container = blobTags.Container,
            Namespace = blobTags.Namespace,
            BatchId = blobTags.BatchId,
            Length = blobTags.Length.ToString(),
            Text = blobTags.Text,
            IsDuplicate = blobTags.IsDuplicate.ToString()
        }, new JsonSerializerOptions { WriteIndented = true });
        return tagsJson;
    }
}

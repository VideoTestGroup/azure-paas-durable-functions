namespace ImageIngest.Functions;

[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
public class DuplicateImages : IDuplicateImages
{
    [JsonProperty]
    public Dictionary<string, IngestImage> DuplicatesImages { get; set; } = new Dictionary<string, IngestImage>();
    public void Add(IngestImage image)
    {
        if (!DuplicatesImages.ContainsKey(image.ImageId))
        {
            DuplicatesImages.Add(image.ImageId, image);
        } 
    }

    public Task<Dictionary<string, IngestImage>> Get()
    {
        return Task.FromResult(DuplicatesImages);
    }

    public void Remove(DateTime timestamp)
    {
        //Duplicatesimages.RemoveAll(image => image.Timestamp.Minute > timestamp.Minute);
    }

    [FunctionName(nameof(DuplicateImages))]
    public static Task HandleEntityOperation([EntityTrigger] IDurableEntityContext context)
    {
        return context.DispatchAsync<DuplicateImages>();
    }
}

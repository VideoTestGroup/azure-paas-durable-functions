﻿namespace ImageIngest.Functions;

[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
public class DuplicateBlobs : IDuplicateBlobs
{
    [JsonProperty]
    public Dictionary<string, DateTime> DuplicateBlobsDic { get; set; } = new Dictionary<string, DateTime>();

    public void Add(string blobName, DateTime timestamp)
    {
        if (!DuplicateBlobsDic.ContainsKey(blobName))
        {
            DuplicateBlobsDic.Add(blobName, timestamp);
        } 
    }

    public Task<Dictionary<string, DateTime>> Get()
    {
        return Task.FromResult(DuplicateBlobsDic);
    }

    public void Remove(DateTime timestamp)
    {
        foreach (var blob in DuplicateBlobsDic.Where(blob => blob.Value < timestamp))
        {
            DuplicateBlobsDic.Remove(blob.Key);
        }
    }

    [FunctionName(nameof(DuplicateBlobs))]
    public static Task HandleEntityOperation([EntityTrigger] IDurableEntityContext context)
    {
        return context.DispatchAsync<DuplicateBlobs>();
    }
}
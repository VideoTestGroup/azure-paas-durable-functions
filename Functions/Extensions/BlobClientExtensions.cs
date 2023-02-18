namespace ImageIngest.Functions.Extensions;

public static class BlobClientExtensions
{
    public static async IAsyncEnumerable<BlobTags> QueryAsync(this BlobContainerClient client, Func<BlobTags, bool> predicate)
    {
        await foreach (BlobItem blob in client.GetBlobsAsync(BlobTraits.Tags))
        {
            BlobTags tags = new BlobTags(blob);
            if (predicate(tags))
                yield return tags;
        }
    }

    public static async IAsyncEnumerable<BlobTags> QueryByTagsAsync(this BlobContainerClient client, string query)
    {
        await foreach (var blob in client.FindBlobsByTagsAsync(query))
        {
            yield return new BlobTags(blob);
        }
    }

    public static async Task<long> DeleteByTagsAsync(this BlobContainerClient client, string query)
    {
        long deletedCount = 0;
        await foreach (TaggedBlobItem taggedBlobItem in client.FindBlobsByTagsAsync(query))
        {
            await client.DeleteBlobIfExistsAsync(taggedBlobItem.BlobName);
            deletedCount++;
        }

        return deletedCount;
    }

    public static string BuildTagsQuery(BlobStatus? status = null, string @namespace = null, string batchId = null, long? modifiedTime = null, bool? isDuplicate = null)
    {
        var queries = new List<string>();

        if (status.HasValue)
        {
            queries.Add($@"""{nameof(BlobTags.Status)}""='{status.Value}'");
        }

        if (!string.IsNullOrEmpty(@namespace))
        {
            queries.Add($@"""{nameof(BlobTags.Namespace)}""='{@namespace}'");
        }

        if (!string.IsNullOrEmpty(batchId))
        {
            queries.Add($@"""{nameof(BlobTags.BatchId)}""='{batchId}'");
        }

        if (modifiedTime.HasValue)
        {
            queries.Add($@"""{nameof(BlobTags.Modified)}""<'{modifiedTime}'");
        }

        if (isDuplicate.HasValue)
        {
            queries.Add($@"""{nameof(BlobTags.IsDuplicate)}""='{(isDuplicate.Value ? "1" : "0")}'");
        }

        return string.Join(" AND ", queries);
    }

    public static async IAsyncEnumerable<BlobTags> ReadTagsAsync(this BlobClient blobClient)
    {
        if (blobClient.Exists())
        {
            Response<GetBlobTagResult> tags = await blobClient.GetTagsAsync();
            var props = await blobClient.GetPropertiesAsync();
            yield return new BlobTags(tags.Value.Tags);
        }

        yield return new BlobTags();
    }

    public static async Task<Response> WriteTagsAsync(this BlobClient blobClient, BlobTags tags, string leaseId = null)
    {
        tags.Modified = DateTime.Now.ToFileTimeUtc();
        return await blobClient.SetTagsAsync(tags.Tags, string.IsNullOrWhiteSpace(leaseId) ? null : new BlobRequestConditions { LeaseId = leaseId });
    }

    public static async Task<Response> WriteTagsAsync(this BlobClient blobClient, BlobTags tags, Action<BlobTags> update, string leaseId = null)
    {
        update(tags);
        return await WriteTagsAsync(blobClient, tags, leaseId);
    }
}
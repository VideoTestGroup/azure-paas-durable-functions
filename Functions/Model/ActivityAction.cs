namespace ImageIngest.Functions.Model;

public class ActivityAction
{
    // IMPORTANT: 
    // When changing ContainerName make sure to change EventGrid's topic filter
    // Subject Begins With: blobServices/default/containers/files
    public const string ContainerName = "files";

    public string Namespace { get; set; } = "default";
    public string BatchId { get; set; }

    public static string CreateBatchId(string @namespace)
    {
        // TODO - Check if the zip format name is ok like that.
        return $"{@namespace}_{DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")}";
    }

    public static ActivityAction ExtractBatchIdAndNamespace(string batchZipFilename)
    {
        int idx = batchZipFilename.LastIndexOf('_');

        if (idx < 0)
            throw new ArgumentException($"batchZipFilename does not contains Namespace, looking for last delimiter '_'", "batchZipFilename");

        string batchId = Path.GetFileNameWithoutExtension(batchZipFilename);
        ActivityAction activity = new ActivityAction
        {
            BatchId = batchId,
            Namespace = batchId[..idx]
        };

        return activity;
    }

    public override string ToString()
    {
        return $"Namespace: {Namespace}, BatchId: {BatchId}";
    }
}

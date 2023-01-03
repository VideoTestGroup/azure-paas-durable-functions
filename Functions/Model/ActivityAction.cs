namespace ImageIngest.Functions.Model;

public class ActivityAction
{
    public string Namespace { get; set; } = "default";
    public string BatchId { get; set; }

    public static string CreateBatchId(string @namespace, int blobsCount)
    {
        return $"{@namespace}_{blobsCount}_{DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")}";
    }

    public override string ToString()
    {
        return $"Namespace: {Namespace}, BatchId: {BatchId}";
    }
}

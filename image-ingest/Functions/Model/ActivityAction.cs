namespace ImageIngest.Functions.Model;

public class ActivityAction
{
    public static TimeSpan Threshold =
        TimeSpan.TryParse(System.Environment.GetEnvironmentVariable("ScavengerOutdatedThreshold"), out TimeSpan span) ? span : TimeSpan.FromMinutes(5);

    public ActivityAction() { }

    public ActivityAction(BlobTags tags)
    {
        QueryStatus = tags.Status;
        Namespace = tags.Namespace;
    }

    public long Total { get; set; }
    public string Namespace { get; set; } = "default";
    public string QueryBatchId { get; set; }
    public string OverrideBatchId { get; set; }
    public BlobStatus QueryStatus { get; set; }
    public BlobStatus OverrideStatus { get; set; }

    //activity.OverrideBatchId = $"{activity.Namespace}-{DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")}";
    public static string EnlistBatchId(string @namespace)
    {
        return $"{@namespace}-{DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")}";
        //TODO: use durable entity
        // EntityId entityId = new EntityId(nameof(DurableBatchCounter), activity.Namespace);
        // var batchCounter = await context.CallEntityAsync<IDurableBatchCounter>(entityId, nameof(IDurableBatchCounter.Enlist));
    }

    public static ActivityAction ExtractBatchIdAndNamespace(string batchZipFilename)
    {
        //string s = "My. name. is Bond._James Bond!";
        int idx = batchZipFilename.LastIndexOf('-');

        if (idx < 0)
            throw new ArgumentException($"batchZipFilename does not contains Namespace, looking for last delimiter '-'", "batchZipFilename");

        string batchId = Path.GetFileNameWithoutExtension(batchZipFilename);
        ActivityAction activity = new ActivityAction
        {
            QueryBatchId = batchId,
            Namespace = batchId[..idx] // "My. name. is Bond"
        };

        return activity;
    }



    public string QueryStatusAndNamespace =>
        $@"""Status""='{QueryStatus.ToString()}' AND ""Namespace""= '{Namespace}'";

    public string QueryStatusAndThreshold =>
        $@"""Status""='{QueryStatus.ToString()}' AND ""Modified"" < '{DateTime.UtcNow.Add(Threshold).ToFileTimeUtc()}'";

    public override string ToString()
    {
        return $"Total: {Total}, QueryBatchId: {QueryBatchId}, OverrideBatchId: {OverrideBatchId}, QueryStatus: {QueryStatus}, OverrideStatus: {OverrideStatus}, Namespace: {Namespace}";
    }
}

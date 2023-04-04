namespace ImageIngest.Functions.Model;

public class BatchJob
{
    private static string AzureWebJobsFTPStorage => Environment.GetEnvironmentVariable("AzureWebJobsFTPStorage");
    private static TimeSpan LeaseDuration => TimeSpan.Parse(Environment.GetEnvironmentVariable("LeaseDuration"));
    
    private readonly Lazy<BlobClient> blobClient;
    //private readonly Lazy<BlobLeaseClient> leaseClient;
    private readonly Lazy<MemoryStream> stream;
    public string Name => Tags.Name;
    public BlobClient BlobClient => blobClient.Value;
    public Stream Stream => stream.Value;
    public BlobTags Tags { get; set; }

    public BatchJob(BlobTags tags)
    {
        Tags = tags;
        blobClient = new Lazy<BlobClient>(() => new BlobClient(AzureWebJobsFTPStorage, Tags.Container, Tags.Name));
        stream = new Lazy<MemoryStream>(() => new MemoryStream());
    }

    public override string ToString() =>
        $"Name: {Name}, Length: {Stream.Length} Tags: {Tags}";
}
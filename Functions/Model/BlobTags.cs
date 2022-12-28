namespace ImageIngest.Functions.Model;
public class BlobTags
{
    private IDictionary<string, string> tags = new Dictionary<string, string>();
    public IDictionary<string, string> Tags => tags;

    public long Created
    {
        get => tags.GetValue<long>(nameof(Created));
        set => tags[nameof(Created)] = value.ToString();
    }

    public long Modified
    {
        get => tags.GetValue<long>(nameof(Modified));
        set => tags[nameof(Modified)] = value.ToString();
    }

    public BlobStatus Status
    {
        get => (Enum.TryParse<BlobStatus>(tags[nameof(Status)], true, out BlobStatus status) ? status : BlobStatus.New);
        set => tags[nameof(Status)] = value.ToString();
    }

    public string Name
    {
        get => tags.GetValue<string>(nameof(Name));
        set => tags[nameof(Name)] = value;
    }

    public string Container
    {
        get => tags.GetValue<string>(nameof(Container));
        set => tags[nameof(Container)] = value;
    }

    public string Namespace
    {
        get => tags.GetValue<string>(nameof(Namespace));
        set => tags[nameof(Namespace)] = value;
    }

    public string BatchId
    {
        get => tags.TryGetValue(nameof(BatchId), out string batchId) ? batchId : string.Empty;
        set => tags[nameof(BatchId)] = value;
    }

    public long Length
    {
        get => tags.GetValue<long>(nameof(Length));
        set => tags[nameof(Length)] = value.ToString();
    }

    public string Text
    {
        get => tags.GetValue<string>(nameof(Text));
        set => tags[nameof(Text)] = value;
    }

    public void Initialize()
    {
        tags[nameof(Container)] = string.Empty;
        tags[nameof(Text)] = string.Empty;
        tags[nameof(Status)] = BlobStatus.Pending.ToString();
        tags[nameof(Name)] = string.Empty;
        tags[nameof(BatchId)] = string.Empty;
        tags[nameof(Namespace)] = "default"; ;
        tags[nameof(Length)] = "0";
        tags[nameof(Created)] = DateTime.Now.ToFileTimeUtc().ToString();
        tags[nameof(Modified)] = DateTime.Now.ToFileTimeUtc().ToString();
    }

    public BlobTags() { }

    public BlobTags(IDictionary<string, string> origin)
    {
        Initialize();
        origin?.ToList()?.ForEach(x => tags[x.Key] = x.Value);
    }

    public BlobTags(BlobItem blobItem)
    {
        Initialize();
        blobItem.Tags?.ToList()?.ForEach(x => tags[x.Key] = x.Value);
        Name = blobItem.Name;
    }

    public BlobTags(BlobProperties props, BlobClient client)
    {
        Initialize();
        Status = BlobStatus.Pending;
        Length = props.ContentLength;
        Name = client.Name;
        Container = client.BlobContainerName;
    }

    public BlobTags(TaggedBlobItem item)
    {
        Initialize();
        item.Tags.ToList().ForEach(x => tags[x.Key] = x.Value);
        Name = item.BlobName;
        Container = item.BlobContainerName;
    }

    public override string ToString() =>
        $"Status: {Status}, Length: {Length}, Namespace: {Namespace}, BatchId: {BatchId}, Created: {Created}, Modified: {Modified}";
}
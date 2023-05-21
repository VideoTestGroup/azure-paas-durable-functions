namespace ImageIngest.Functions.Model;
[DataContract]
public class BlobTags
{
    public string Name { get; set; }

    private IDictionary<string, string> tags = new Dictionary<string, string>();
    public IDictionary<string, string> Tags => tags;

    [DataMember]
    public long Created
    {
        get => tags.GetValue<long>(nameof(Created));
        set => tags[nameof(Created)] = value.ToString();
    }

    [DataMember]
    public long Modified
    {
        get => tags.GetValue<long>(nameof(Modified));
        set => tags[nameof(Modified)] = value.ToString();
    }

    [DataMember]
    public BlobStatus Status
    {
        get => (Enum.TryParse<BlobStatus>(tags[nameof(Status)], true, out BlobStatus status) ? status : BlobStatus.New);
        set => tags[nameof(Status)] = value.ToString();
    }

    [DataMember]
    public string Container
    {
        get => tags.GetValue<string>(nameof(Container));
        set => tags[nameof(Container)] = value;
    }

    [DataMember]
    public string Namespace
    {
        get => tags.GetValue<string>(nameof(Namespace));
        set => tags[nameof(Namespace)] = value;
    }

    [DataMember]
    public string BatchId
    {
        get => tags.TryGetValue(nameof(BatchId), out string batchId) ? batchId : string.Empty;
        set => tags[nameof(BatchId)] = value;
    }

    [DataMember]
    public long Length
    {
        get => tags.GetValue<long>(nameof(Length));
        set => tags[nameof(Length)] = value.ToString();
    }

    [DataMember]
    public string Text
    {
        get => tags.GetValue<string>(nameof(Text));
        set => tags[nameof(Text)] = value;
    }

    [DataMember]
    public bool IsDuplicate
    {
        get => tags.GetValue<string>(nameof(IsDuplicate)) == "1";
        set => tags[nameof(IsDuplicate)] = value ? "1" : "0";
    }

    public void Initialize()
    {
        tags[nameof(Container)] = string.Empty;
        tags[nameof(Text)] = string.Empty;
        tags[nameof(Status)] = BlobStatus.Pending.ToString();
        tags[nameof(BatchId)] = string.Empty;
        tags[nameof(Length)] = "0";
        tags[nameof(Created)] = DateTime.Now.ToFileTimeUtc().ToString();
        tags[nameof(Modified)] = DateTime.Now.ToFileTimeUtc().ToString();
        tags[nameof(IsDuplicate)] = "0";
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

    //achi
    public BlobTags(IDictionary<string,string> Tags, string BlobName)
    {
        Initialize();
        Tags?.ToList()?.ForEach(x => tags[x.Key] = x.Value);
        Name = BlobName;
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
        item.Tags?.ToList()?.ForEach(x => tags[x.Key] = x.Value);
        Name = item.BlobName;
        Container = item.BlobContainerName;
    }

    public override string ToString() =>
        $"Name: {Name}, Status: {Status}, Length: {Length}, Namespace: {Namespace}, BatchId: {BatchId}, Created: {Created}, Modified: {Modified}";
}

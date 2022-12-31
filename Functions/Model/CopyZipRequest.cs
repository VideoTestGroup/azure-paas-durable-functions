namespace ImageIngest.Functions.Model;

public class CopyZipRequest
{
    public DistributionTarget DistributionTarget { get; set; }
    public string ContainerName { get; set; }
    public Uri SourceBlobSasToken { get; set; }
    public string BlobName { get; set; }
}

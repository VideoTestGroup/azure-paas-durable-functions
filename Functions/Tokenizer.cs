using Azure.Storage.Sas;

namespace ImageIngest.Functions;
public class Tokenizer
{
    private static TimeSpan SasDuration => TimeSpan.Parse(Environment.GetEnvironmentVariable("SasDuration"));
    private static string ContainerWritePolicy => Environment.GetEnvironmentVariable("ContainerWritePolicy");

    [FunctionName(nameof(Tokenizer))]
    public static async Task<ActivityAction> Run(
        [ActivityTrigger] ActivityAction activity,
        [Blob(ActivityAction.ContainerName, Connection = "AzureWebJobsFTPStorage")] BlobContainerClient containerClient,
        [DurableClient] IDurableEntityClient entity,
        ILogger log)
    {
        log.LogInformation($"[Tokenizer] ActivityTrigger trigger function Processed blob\n activity:{activity}");
        // Check whether this BlobContainerClient object has been authorized with Shared Key.
        //if (!containerClient.CanGenerateSasUri)
        //    throw new UnauthorizedAccessException(@"[Tokenizer] BlobContainerClient must be authorized with Shared Key credentials to create a service SAS.");


        //EntityId entityId = new EntityId(nameof(IDurableSasToken), containerClient.Name);
        //EntityStateResponse<IDurableSasToken> response = await entity.ReadEntityStateAsync<IDurableSasToken>(entityId);
        //if (
        //    response.EntityExists &&
        //    null != response.EntityState &&
        //    response.EntityState.Value.ExpiresOn > DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(1)))
        //{
        //    activity.Token = response.EntityState.Value;
        //    return activity;
        //}

        //// Create a SAS token that's valid for one hour.
        //var expiresOn = DateTimeOffset.UtcNow.Add(SasDuration);
        //BlobSasBuilder sasBuilder = new BlobSasBuilder
        //{
        //    BlobContainerName = containerClient.Name,
        //    //  Specify "c" if the shared resource is a blob container. This grants
        //    //  access to the content and metadata of any blob in the container, and to the list
        //    //  of blobs in the container
        //    Resource = "c", //b = blob, c = container
        //    StartsOn = DateTimeOffset.UtcNow,
        //    ExpiresOn = expiresOn,
        //    Identifier = ContainerWritePolicy
        //};
        //if (string.IsNullOrWhiteSpace(ContainerWritePolicy))
        //    sasBuilder.SetPermissions(
        //        BlobContainerSasPermissions.Read |
        //        BlobContainerSasPermissions.Write |
        //        BlobContainerSasPermissions.Delete |
        //        BlobContainerSasPermissions.Tag |
        //        BlobContainerSasPermissions.Filter |
        //        BlobContainerSasPermissions.List);

        //Uri sasUri = containerClient.GenerateSasUri(sasBuilder);
        //SasToken token = new SasToken {
        //    Url = containerClient.GenerateSasUri(sasBuilder),
        //    ExpiresOn = expiresOn
        //};

        //await entity.SignalEntityAsync<IDurableSasToken>(entityId, e => e.Set(token));
        //log.LogInformation("[Tokenizer] SAS URI for blob container is: {0}", sasUri);

        //activity.Token = token;
        return activity;
    }
}
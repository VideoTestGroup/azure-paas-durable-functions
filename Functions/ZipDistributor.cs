using Azure.Storage.Sas;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json.Linq;

namespace ImageIngest.Functions;

public class ZipDistributor
{
    private readonly TargetSourceOptions _targetSources;

    public ZipDistributor(TargetSourceOptions targetSources)
    {
        _targetSources = targetSources;
    }

    [FunctionName(nameof(ZipDistributor))]
    public async Task Run(
        [BlobTrigger("zip/{name}.zip", Connection = "AzureWebJobsZipStorage")] BlobClient blobClient,
        [DurableClient] IDurableEntityClient entity,
        ILogger log)
    {
        log.LogInformation($"[ZipDistributor] Function triggered on blob {blobClient.Name}");
        try
        {
            var sourceBlobSasToken = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.Now.AddMinutes(5));
            List<Task> tasks = new List<Task>();

            foreach (var targetSource in _targetSources.TargetSources)
            {
                tasks.Add(Task.Run(async () =>
                {
                    log.LogInformation($"[ZipDistributor] Start target destination - {targetSource.TargetName}");
                    string containerName = targetSource.ContainerName;
                    if (targetSource.IsRoundRobin.HasValue &&
                        targetSource.IsRoundRobin.Value &&
                        targetSource.ContainersCount.HasValue)
                    {
                        var entityId = new EntityId(nameof(DurableTargetState), targetSource.TargetName);
                        var response = await entity.ReadEntityStateAsync<DurableTargetState>(entityId);
                        if (response.EntityExists && response.EntityState != null)    
                        {
                            containerName += response.EntityState.CurrentValue.ToString();
                        }

                        await entity.SignalEntityAsync<IDurableTargetState>(entityId, e => e.ChangeValue(targetSource.ContainersCount.Value));
                    }

                    // TODO - Check if the best option.

                    log.LogInformation($"[ZipDistributor] Start copy {blobClient.Name} to destination - {targetSource.TargetName}, containerName - {containerName}");

                    var destClient = new BlobClient(targetSource.ConnectionString, containerName, blobClient.Name);
                    var copyProcess = await destClient.StartCopyFromUriAsync(sourceBlobSasToken);
                    await copyProcess.WaitForCompletionAsync();

                    log.LogInformation($"[ZipDistributor] Finish copy {blobClient.Name} to destination - {targetSource.TargetName}");

                    var destProps = destClient.GetProperties().Value;
                    if (destProps.BlobCopyStatus != CopyStatus.Success)
                    {
                        log.LogError($"[ZipDistributor] Unsuccessfull copy {blobClient.Name} to destination - {targetSource.TargetName}, description: {destProps.CopyStatusDescription}");
                        // TODO - Log and mark the zip not successfull.
                    }
                }));
            }

            await Task.WhenAll(tasks);
            // TODO - Delete zip if all successfull.
            // TODO - Handle errors
        }
        catch (Exception ex)
        {
            log.LogError(ex, $"[ZipDistributor] execption message: {ex.Message}, stacktarce: {ex.StackTrace}");
        }
    } 
}

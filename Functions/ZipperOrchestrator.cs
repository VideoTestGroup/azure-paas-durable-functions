namespace ImageIngest.Functions;
public class ZipperOrchestrator
{
    [FunctionName(nameof(ZipperOrchestrator))]
    public static async Task Run(
        [ServiceBusTrigger("batches", Connection = "ServiceBusConnection", AutoCompleteMessages=true)]
            string myQueueItem,
            Int32 deliveryCount,
            DateTime enqueuedTimeUtc,
            string messageId,
            [DurableClient] IDurableOrchestrationClient starter,
        ILogger log)
    {
        log.LogInformation($"[ZipperOrchestrator] OrchestrationTrigger triggered Function for InstanceId ");
        var activity = new ActivityAction() { BatchId = myQueueItem };

        log.LogInformation($"[ZipperOrchestrator] Zipping files for activity: {activity}");
     //   bool? isSuccessfull = await context.CallActivityAsync<bool?>(nameof(Zipper), activity);
        await starter.StartNewAsync(nameof(Zipper), activity);
     //   if (isSuccessfull.HasValue)
     //   {
     //       log.LogInformation($"[ZipperOrchestrator] Finish zipping files for acitivty: {activity}. result: {isSuccessfull}");
     //   }
    }
}

using Microsoft.Azure.WebJobs; 
using Microsoft.Extensions.Logging; 
using System.Threading.Tasks; 
namespace ImageIngest.Functions { 
public static class BatchProcessorFunction { 
[FunctionName("BatchProcessorFunction")] 
public static async Task RunAsync( 
[ServiceBusTrigger("camsftpfr", Connection = "ServiceBusConnection", AutoCompleteMessages=true)] string message,
ILogger logger) { 
logger.LogInformation($"Received batch message: {message}");
// Process the batch message asynchronously here 
await Task.Delay(1000); // Example processing delay // You can also use await to call other async methods for processing logger.LogInformation("Batch message processed."); 
} } }

using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;

namespace ImageIngest.Functions
{
    //  AccessRights.Listen
    public static class TestQueue
    {
        [FunctionName("TestQueue")]
        public static async Task Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [ServiceBus("camsftpfr", Connection = "ServiceBusConnection")] IAsyncCollector<string> output,
            ILogger log)
        {
            string name = req.Query["name"];
            log.LogInformation($"C# HTTP trigger function processed a request. name: {name}");
            await output.AddAsync(name);



            //string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //dynamic data = JsonConvert.DeserializeObject(requestBody);
            //name = name ?? data?.name;

            //string responseMessage = string.IsNullOrEmpty(name)
            //    ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
            //    : $"Hello, {name}. This HTTP triggered function executed successfully.";

            //return new OkObjectResult(responseMessage);
        }
    }
}

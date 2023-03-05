using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json.Linq;

namespace ImageIngest.Functions;

public class DuplicateBlobWatcher
{
    [FunctionName("DuplicateBlobWatcher")]
    public static async Task<HttpResponseMessage> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest request,
        [DurableClient] IDurableEntityClient client,
        ILogger log)
        {
          string name = request.Query["name"];
          string key = request.Query["key"];
          string op = request.Query["op"];
          log.LogInformation($"name: {name}, key: {key}, op: {op}");
          var entityId = new EntityId(name, key); 
          switch(op.ToLower())
          {
            case "reset":
                await client.SignalEntityAsync(entityId, "Reset");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("RESET")
                };          
            default:
                EntityStateResponse<JObject> stateResponse = await client.ReadEntityStateAsync<JObject>(entityId);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(stateResponse.EntityState), Encoding.UTF8, "application/json")
                };          
          }
        }
}

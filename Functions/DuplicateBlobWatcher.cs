namespace ImageIngest.Functions;

public class DuplicateBlobWatcher
{
    [FunctionName("DuplicateBlobWatcher")]
    public static async Task<HttpResponseMessage> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest request,
        [DurableClient] IDurableEntityClient client,
        ILogger log)
        {
          string name = req.Query["name"];
          string key = req.Query["key"];
          string op = req.Query["op"] ?? "defalt";
          log.LogInformation($"name: {name}, key: {key}, op: {op}");
          var entityId = new EntityId(name, key); 
          switch(op.ToLower())
          {
            case "reset":
                client.SignalEntityAsync(entityId, "Reset");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(stateResponse.EntityState), Encoding.UTF8, "application/json")
                };          
                break;
            default:
                EntityStateResponse<JObject> stateResponse = await client.ReadEntityStateAsync<JObject>(entityId);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(stateResponse.EntityState), Encoding.UTF8, "application/json")
                };          
          }
        }
}

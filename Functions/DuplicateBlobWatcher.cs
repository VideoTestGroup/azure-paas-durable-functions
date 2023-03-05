using System.Web;
using Microsoft.AspNetCore.Mvc;

namespace ImageIngest.Functions;

[FunctionName("DuplicateBlobWatcher")]
public static async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
    [DurableClient] IDurableEntityClient client,
    ILogger log)
    {
      string name = req.Query["name"];
      string key = req.Query["key"];
      string op = req.Query["op"] ?? "defalt";
      var entityId = new EntityId(name, key); 
      switch(op.ToLower())
      {
        case "reset":
          client.SignalEntityAsync(entityId, "Reset");
          return req.CreateResponse(HttpStatusCode.OK, stateResponse.EntityState);
          break;
        default:
          EntityStateResponse<JObject> stateResponse = await client.ReadEntityStateAsync<JObject>(entityId);
          return req.CreateResponse(HttpStatusCode.OK, stateResponse.EntityState);
      }
    }

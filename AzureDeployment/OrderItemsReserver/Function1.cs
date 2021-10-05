using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OrderItemsReserver
{
    public static class Function1
    {
        [Function("postorder")]
        public static async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("postorder");
            logger.LogInformation("Processing an order");

            var content = await new StreamReader(req.Body).ReadToEndAsync();

            Order order = JsonConvert.DeserializeObject<Order>(content);

            logger.LogInformation("The order details:");
            logger.LogInformation(content);

            BlobServiceClient blobServiceClient = new BlobServiceClient(
                "replace");
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("orders");
            await containerClient.CreateIfNotExistsAsync();
            
            logger.LogInformation("Uploading to Blob container starting");
            BlobClient blobClient = containerClient.GetBlobClient($"{Guid.NewGuid().ToString()}.json");
            req.Body.Position = 0;
            await blobClient.UploadAsync(req.Body);
            logger.LogInformation("Uploading to Blob container finished");


            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString($"The order with identifier - {order.Id} stored to the Blob storage!");

            return response;
        }
    }
}

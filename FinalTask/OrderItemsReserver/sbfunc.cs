using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OrderItemsReserver
{
    public static class sbfunc
    {
        [FunctionName("sbfunc")]
        public static async Task Run(
            [ServiceBusTrigger("orders", Connection = "sbconnection")]string myQueueItem, ILogger logger)
        {
            logger.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");

            logger.LogInformation("Processing an order");

            Order order = JsonConvert.DeserializeObject<Order>(myQueueItem);

            logger.LogInformation("The order details:");
            logger.LogInformation(myQueueItem);

            BlobServiceClient blobServiceClient = new BlobServiceClient(
                "DefaultEndpointsProtocol=https;AccountName=ordersstra;AccountKey=Ca04b0heAb+tOOxW9EH+EJHvKSbOcmie00lfPiI5GKF06wZXu9dloPcuJcP/h1Y7ZAdv5EgazVqE4PjskG7nFA==;EndpointSuffix=core.windows.net");
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("orders");
            await containerClient.CreateIfNotExistsAsync();

            logger.LogInformation("Uploading to Blob container starting");
            BlobClient blobClient = containerClient.GetBlobClient($"{Guid.NewGuid().ToString()}.json");
            await blobClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(myQueueItem)));
            logger.LogInformation("Uploading to Blob container finished");
        }
    }
}

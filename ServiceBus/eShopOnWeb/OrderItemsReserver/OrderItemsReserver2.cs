using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OrderItemsReserver
{
    public static class OrderItemsReserver2
    {
        [Function("OrderItemsReserver2")]
        public static async Task Run([ServiceBusTrigger("orderreservation", 
            Connection = "ServiceBusConnection")] string myQueueItem, FunctionContext context)
        {
            var logger = context.GetLogger("OrderItemsReserver2");
            logger.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");

			try
			{
				logger.LogInformation("Processing an order");

				//var content = await new StreamReader(req.Body).ReadToEndAsync();

				Order order = JsonConvert.DeserializeObject<Order>(myQueueItem);

				logger.LogInformation("The order details:");
				logger.LogInformation(myQueueItem);

				BlobServiceClient blobServiceClient = new BlobServiceClient(
					"DefaultEndpointsProtocol=https;AccountName=storageaccounttorema94f;AccountKey=1ehBBieUWsNtOjGX1FN6XQEujQRAB0DTTF4lssuSbzIrRDwa+W4sHQEISK7EPduYBfKiLF7Xb5ocgzHb0fGApQ==;EndpointSuffix=core.windows.net");
				BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("orders");
				await containerClient.CreateIfNotExistsAsync();

				logger.LogInformation("Uploading to Blob container starting");
				BlobClient blobClient = containerClient.GetBlobClient($"{Guid.NewGuid().ToString()}.json");
				await blobClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(myQueueItem)));
				logger.LogInformation("Uploading to Blob container finished");
			}
			catch (Exception e)
			{
				using (var client = new HttpClient())
				{
					await client.PostAsync("https://fvoicesendmail.azurewebsites.net:443/api/sendmail/triggers/manual/invoke?api-version=2020-05-01-preview&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=FAgVKDHpECqyP0CPu1r-cj1hG8GcckTDBEVOsusNqCE", 
						new StringContent(e.ToString()));
				}
				throw;
			}
        }
    }
}

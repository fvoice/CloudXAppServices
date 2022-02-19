using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Container = System.ComponentModel.Container;

namespace OrderItemsReserver
{
	public static class ProcessOrder2
    {
	    private static readonly string EndpointUri = "https://fvoiceaccount.documents.azure.com:443/";

	    // The primary key for the Azure Cosmos account.
	    private static readonly string PrimaryKey = "1obzifQVikbqreUMIefHHujEjHeOmDQIAHdaYjnher2GzEdZnSu4ly9KgxCWYEwGiPBsbUHqvouT83NSoqpA5g==";

        [FunctionName("ProcessOrder2")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger logger)
        {
	        logger.LogInformation("Processing an order");

			try
			{
				var content = await new StreamReader(req.Body).ReadToEndAsync();

				var order = JsonConvert.DeserializeObject<Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate.Order>(content);

				logger.LogInformation("The order details:");
				logger.LogInformation(content);

				var cosmosClient = new CosmosClient(EndpointUri, PrimaryKey, new CosmosClientOptions()
				{
					ApplicationName = "DeliveryOrderProcessor"
				});

				Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync("eShopOnline");
				var container = (await database.CreateContainerIfNotExistsAsync("Orders", "/BuyerId")).Container;

				var dbOrder = new Order()
				{
					id = Guid.NewGuid().ToString(),
					Address = order.ShipToAddress.ToString(),
					BuyerId = order.BuyerId,
					OrderDate = order.OrderDate.Date,
					OrderItems = order.OrderItems,
					Price = order.Total(),
				};

				ItemResponse<Order> cosmosDbResponse = await container.CreateItemAsync<Order>(dbOrder,
					new PartitionKey(order.BuyerId));

				logger.LogInformation("Order has been stored to CosmosDB");
			}
			catch (Exception e)
			{
				logger.LogError(e, "error");
			}

            return new OkObjectResult("ok");
        }

        public class Order
        {
	        public string id { get; set; }
	        public DateTime OrderDate { get; set; }
	        public string BuyerId { get; set; }
	        public string Address { get; set; }
	        public List<OrderItem> OrderItems { get; set; }
	        public decimal Price { get; set; }
        }
    }
}

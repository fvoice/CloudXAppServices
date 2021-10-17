using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DeliveryOrderProcessor
{
    // The Azure Cosmos DB endpoint for running this sample.

    public static class ProcessOrder
    {
        private static readonly string EndpointUri = "https://fvoiceaccount.documents.azure.com:443/";

        // The primary key for the Azure Cosmos account.
        private static readonly string PrimaryKey = "vSOICwOWEzLCeKWpSlJx1K3PrgSCClrPJr9QnXkpX1FD5kz8fBgxNjiuccwJ6jlvONhnctyX6uezyW0e6JEaog==";

        [Function("ProcessOrder")]
        public static async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("postorder");
            logger.LogInformation("Processing an order");

            var content = await new StreamReader(req.Body).ReadToEndAsync();

            var order = JsonConvert.DeserializeObject<Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate.Order>(content);

            logger.LogInformation("The order details:");
            logger.LogInformation(content);

            var cosmosClient = new CosmosClient(EndpointUri, PrimaryKey, new CosmosClientOptions()
            {
                ApplicationName = "DeliveryOrderProcessor"
            });

            Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync("eShopOnline");
            Container container = await database.CreateContainerIfNotExistsAsync("Orders", "/BuyerId");

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

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions!");

            return response;
        }
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

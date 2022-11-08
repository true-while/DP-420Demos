using System;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Cosmos;
using System.Reflection.Metadata;
using Azure;

namespace TheCloudShopsLoader
{
    public class Program
    {
        // The Azure Cosmos DB endpoint for running this sample.
        private static readonly string EndpointUri = ConfigurationManager.AppSettings["EndpointUri"];
        // The primary key for the Azure Cosmos account.
        private static readonly string PrimaryKey = ConfigurationManager.AppSettings["PrimaryKey"];

        // The Cosmos client instance
        private CosmosClient cosmosClient;

        // The database we will create
        private Database database;

        // The container we will create.
        private Container container1;
        private static readonly string container1id = ConfigurationManager.AppSettings["container1"];
        // The container we will create.
        private Container container2;
        private static readonly string container2id = ConfigurationManager.AppSettings["container2"];

        // The name of the database and container we will create
        private string databaseId = "DP420Demo";
        private string containerId = "TheCloudShops";

        //rand
        private Random rand = new Random(DateTime.Now.Second);

        public static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Beginning operations...\n");
                Program p = new Program();
                await p.GetStartedDemoAsync();

            }
            catch (CosmosException de)
            {
                Exception baseException = de.GetBaseException();
                Console.WriteLine("{0} error occurred: {1}", de.StatusCode, de);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e);
            }
            finally
            {
                Console.WriteLine("End of demo, press any key to exit.");
                Console.ReadKey();
            }
        }

        public async Task GetStartedDemoAsync()
        {
            // Create a new instance of the Cosmos Client
            this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            //create db and containers
            await this.CreateDatabaseAsync();
            await this.CreateContainerAsync();

            Console.WriteLine($"\r\n--------------------- {container1id} test ---------------------");
            await this.OpearationTestAsync(container1, (o) => o.OrderAddress.City);

            Console.WriteLine($"\r\n--------------------- {container2id} test ---------------------");
            await this.OpearationTestAsync(container2, (o) => o.id);
        }

        /// <summary>
        /// Create the container if it does not exist. 
        /// Specify "/OrderAddress/City" as the partition key since we're storing different types of documents.
        /// </summary>
        /// <returns></returns>
        private async Task CreateContainerAsync()
        {
            // Create a new container
            this.container1 = (Container)await this.database.CreateContainerIfNotExistsAsync(container1id, "/OrderAddress/City");
            Console.WriteLine("Created Container: {0}\n", container1id);

            // Create a new container
            this.container2 = (Container)await this.database.CreateContainerIfNotExistsAsync(container2id, "/id");
            Console.WriteLine("Created Container: {0}\n", container2id);
        }

        /// <summary>
        /// Create the database if it does not exist
        /// </summary>
        private async Task CreateDatabaseAsync()
        {
            // Create a new database
            this.database = (Database)await this.cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            Console.WriteLine("Created Database: {0}\n", this.database.Id);
        }

        private async Task<Order> ReadDocument(Container container, string orderid, string partition)
        {
            ItemResponse<Order> readResponse = await container.ReadItemAsync<Order>(orderid, new PartitionKey(partition));
            Console.WriteLine("Read item from database with id: {0} Operation consumed {1} RUs.\n", readResponse.Resource.id, readResponse.RequestCharge);
            return readResponse;
        }

        private async Task DeleteDocument(Container container, string orderid, string partition)
        {
            ItemResponse<Order> deleteResponse = await container.DeleteItemAsync<Order>(orderid, new PartitionKey(partition));

            Console.WriteLine("Delete item in database with id: {0} Operation consumed {1} RUs.\n", orderid, deleteResponse.RequestCharge);
        }

        private async Task CreateDocumentsIfNotExists(Container container, Order order, Func<Order, string> partition)
        {
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Order> readResponse = await container.ReadItemAsync<Order>(order.id, new PartitionKey(partition.Invoke(order)));
                Console.WriteLine("Item in database with id: {0} already exists, Operation consumed {1} RUs\n", readResponse.Resource.id, readResponse.RequestCharge);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Create an item in the container.
                ItemResponse<Order> createResponse = await container.CreateItemAsync<Order>(order, new PartitionKey(partition.Invoke(order)));

                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine("Created item in database with id: {0}, Operation consumed {1} RUs.\n", createResponse.Resource.id, createResponse.RequestCharge);
            }
        }

        private async Task UpdateDoc(Order order, Container container)
        {
            // You can modify the saddle variable we defined earlier.
            order.Description = "updated description";

            // We can persist the change invoking the asynchronous UpsertItemAsync<> method passing in only the update item.
            ItemResponse<Order> updatedResponse = await container.UpsertItemAsync<Order>(order);

            Console.WriteLine("Update item in database with id: {0} Operation consumed {1} RUs.\n", updatedResponse.Resource.id, updatedResponse.RequestCharge);

        }



        /// <summary>
        /// Add Family items to the container
        /// </summary>
        private async Task OpearationTestAsync(Container container, Func<Order, string> partition)
        {
            // Create a objects
            var customers = new List<Customer>() { new Customer() { IsActive = true, Name = "Level4you" }, new Customer() { IsActive = true, Name = "UpperLevel" }, new Customer() { IsActive = false, Name = "Channel-9" } };
            var products = new List<Product>() { new Product() { ProductName = "Food" }, new Product() { ProductName = "Book" }, new Product() { ProductName = "Coffee" } };

            List<string> states = new List<string> { "WA", "NY", "FL", "OH" };
            List<string> county = new List<string> { "King", "Brooklyn", "Miami-Dade", "Hamilton" };
            List<string> city = new List<string> { "Seattle", "NYC", "Miami", "Cincinnati" };


            var orderNum = 0;
            Order order;
            var index = 0;

            index = rand.Next(states.Count - 1);
            orderNum = rand.Next(1000);

            order = new Order()
            {
                id = orderNum.ToString(),
                OrderNumber = $"NL-{orderNum}",
                Description = "created order",
                OrderCustomer = customers[rand.Next(customers.Count - 1)],
                OrderAddress = new Address { State = states[index], County = county[index], City = city[index] },
                OrderItems = new[] {
                    new OrderItem() { ProductItem  = products[rand.Next(products.Count - 1)], Count = rand.Next(10) },
                    new OrderItem() { ProductItem  = products[rand.Next(products.Count - 1)], Count = rand.Next(10) }
                    }
            };
            //create orders
            await CreateDocumentsIfNotExists(container, order, partition);
            await UpdateDoc(order, container);


            Order oneOrder = await ReadDocument(container, orderNum.ToString(), partition.Invoke(order));
            if (oneOrder != null)
                await DeleteDocument(container, oneOrder.id, partition.Invoke(order));

            await QueryItemsAsync(container);
        }

        private async Task QueryItemsAsync(Container container)
        {

            var sqlQueryText = @"SELECT * FROM orders o 
                        WHERE o.TotalPrice > 400 
                        AND startsWith(o.OrderCustomer.Name, 'Le') 
                        AND o.OrderAddress.City='NYC'";

            Console.WriteLine("Running query: {0}\n", sqlQueryText);

            QueryRequestOptions options = new()
            {
                PopulateIndexMetrics = true
            };

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            FeedIterator<Order> queryResultSetIterator = container.GetItemQueryIterator<Order>(queryDefinition, requestOptions: options); 

            List<Order> families = new List<Order>();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Order> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (Order order in currentResultSet)
                {
                    families.Add(order);
                }
                Console.WriteLine("Select orders. Operation consumed {0} RUs.\n", currentResultSet.RequestCharge);
                Console.WriteLine(currentResultSet.IndexMetrics);
            }
            Console.WriteLine($"{families.Count} orders found");
        }
    }

}

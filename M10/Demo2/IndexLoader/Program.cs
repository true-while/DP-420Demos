using System;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Cosmos;
using System.Reflection.Metadata;
using System.Text;
using System.IO;
using System.ComponentModel;
using Container = Microsoft.Azure.Cosmos.Container;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

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
        private Container container2;

        private string description;

        // The name of the database and container we will create
        private string databaseId = "DP420Demo";
        private string containerId1 = "IndexDemo1";
        private string containerId2 = "IndexDemo2";

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
            CosmosClientOptions options = new CosmosClientOptions() { AllowBulkExecution = true };
            // Create a new instance of the Cosmos Client
            this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey, options);
            //create db and containers
            await this.CreateDatabaseAsync();
            await this.CreateContainerAsync();

            //run seed method to load initial data
            await this.AddItemsToContainerAsync(container1, (ord)=>ord.OrderAddress.City);
            await this.AddItemsToContainerAsync(container2,(ord)=>ord.id.ToString());
        }

        /// <summary>
        /// Create the container if it does not exist. 
        /// Specify "/OrderAddress/City" as the partition key since we're storing different types of documents.
        /// </summary>
        /// <returns></returns>
        private async Task CreateContainerAsync()
        {
              // Create a first container
            this.container1 = (Container)await this.database.CreateContainerIfNotExistsAsync(containerId1, "/OrderAddress/City",1000);
            Console.WriteLine("Created Container: {0}\n", this.container1.Id);

            // Create a second container
            this.container2 = (Container)await this.database.CreateContainerIfNotExistsAsync(containerId2, "/id", 1000);
            Console.WriteLine("Created Container: {0}\n", this.container2.Id);
        }

        /// <summary>
        /// Create the database if it does not exist
        /// </summary>
        private async Task CreateDatabaseAsync()
        {
            // Create a new database
            this.database = (Database) await this.cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            Console.WriteLine("Created Database: {0}\n", this.database.Id);
        }

        private async Task<Order> ReadDocument(string orderid, string partition, Container container)
        {
            ItemResponse<Order> readResponse = await container.ReadItemAsync<Order>(orderid, new PartitionKey( partition));
            Console.WriteLine("Read item from database with id: {0} Operation consumed {1} RUs.\n", readResponse.Resource.id, readResponse.RequestCharge);
            return readResponse;
        }

        private async Task DeleteDocument(string orderid, string partition, Container container)
        {
            ItemResponse<Order> deleteResponse = await container.DeleteItemAsync<Order>(orderid, new PartitionKey(partition));

            Console.WriteLine("Delete item in database with id: {0} Operation consumed {1} RUs.\n", orderid, deleteResponse.RequestCharge);
        }

        private async Task CreateDocumentsIfNotExists(Order order, Container container, string partition) 
        {
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Order> readResponse = await container.ReadItemAsync<Order>(order.id, new PartitionKey(partition));
                Console.WriteLine("Item in database with id: {0} already exists\n", readResponse.Resource.id);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {


                try
                {
                    // Create an item in the container.
                    ItemResponse<Order> createResponse = await container.CreateItemAsync<Order>(order, new PartitionKey(partition));
                    // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                    Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", createResponse.Resource.id, createResponse.RequestCharge);

                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error: {order.id}");
                }

            }
        }

     
        /// <summary>
        /// Add Family items to the container
        /// </summary>
        private async Task AddItemsToContainerAsync(Container container, Func<Order,string> partition)
        {
            // Create a objects
            var customers = new List<Customer>() { new Customer() { IsActive = true, Name = "Level4you" }, new Customer() { IsActive = true, Name = "UpperLevel" }, new Customer() { IsActive = false, Name = "Channel-9" } };
            var products = new List<Product>() { new Product() { ProductName = "Food" }, new Product() { ProductName = "Book" }, new Product() { ProductName = "Coffee" } };

            List<string> states = new List<string> { "WA", "NY", "FL", "OH" };
            List<string> county = new List<string> { "King", "Brooklyn", "Miami-Dade", "Hamilton" };
            List<string> city = new List<string> { "Seattle", "NYC", "Miami", "Cincinnati" };

            Regex rg = new Regex("![A-z1-9,.]");

            this.description = rg.Replace( File.ReadAllText(@".\leo.txt")," ");

            var orderNum = 0;
            var index = 0;

            List<Task> concurrentTasks = new List<Task>();

            for (var id = 0; id < 1000; id++)
            {
                index = rand.Next(states.Count - 1);
                orderNum = rand.Next(1000000);

                Order order = new Order()
                {
                    id = orderNum.ToString(),
                    TotalPrice = rand.Next(500),
                    OrderNumber = $"NL-{orderNum}",
                    Description = (this.description.Substring(rand.Next(description.Length - 1000), 1000)).Trim(),
                    OrderCustomer = customers[rand.Next(customers.Count - 1)],
                    OrderAddress = new Address { State = states[index], County = county[index], City = city[index] },
                    OrderItems = new[] {
                    new OrderItem() { ProductItem  = products[rand.Next(products.Count - 1)], Count = rand.Next(10) },
                    new OrderItem() { ProductItem  = products[rand.Next(products.Count - 1)], Count = rand.Next(10) }
                    }
                };
                //create orders
                concurrentTasks.Add(CreateDocumentsIfNotExists(order, container,partition.Invoke(order))); 
            }
            await Task.WhenAll(concurrentTasks);
        }
    }
}

using System;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Cosmos;
using System.Reflection.Metadata;

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
        private Container container;

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
            //bulkInsertDemo
            CosmosClientOptions options = new()
            {
                AllowBulkExecution = true
            };

            // Create a new instance of the Cosmos Client
            this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey, options);
            //create db and containers
            await this.CreateDatabaseAsync();
            await this.CreateContainerAsync();

            //run seed method to load initial data
            await this.PrepOrdersAsync();
        }

        /// <summary>
        /// Create the container if it does not exist. 
        /// Specify "/OrderAddress/City" as the partition key since we're storing different types of documents.
        /// </summary>
        /// <returns></returns>
        private async Task CreateContainerAsync()
        {
            // Create a new container
            this.container = (Container)await this.database.CreateContainerIfNotExistsAsync(containerId, "/OrderAddress/City");
            Console.WriteLine("Created Container: {0}\n", this.container.Id);
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

       

        private async Task BulkInsertAsync(List<Order> list)
        {
            List<Task> concurrentTasks = new List<Task>();

            foreach (Order order in list)
            {
                concurrentTasks.Add(
                    container.CreateItemAsync<Order>(
                        order,
                        new PartitionKey(order.OrderAddress.City)
                    ).ContinueWith(result=> Console.WriteLine("Create items in DB with id: {0} Operation consumed {1} RUs.\n", result.Result.Resource.id, result.Result.RequestCharge))
                );
            }

            await Task.WhenAll(concurrentTasks);            

        }



        /// <summary>
        /// Add Family items to the container
        /// </summary>
        private async Task PrepOrdersAsync()
        {
            // Create a objects
            var customers = new List<Customer>() { new Customer() { IsActive = true, Name = "Level4you" }, new Customer() { IsActive = true, Name = "UpperLevel" }, new Customer() { IsActive = false, Name = "Channel-9" } };
            var products = new List<Product>() { new Product() { ProductName = "Food" }, new Product() { ProductName = "Book" }, new Product() { ProductName = "Coffee" } };

            List<string> states = new List<string> { "WA", "NY", "FL", "OH" };
            List<string> county = new List<string> { "King", "Brooklyn", "Miami-Dade", "Hamilton" };
            List<string> city = new List<string> { "Seattle", "NYC", "Miami", "Cincinnati" };


            var bulkList = new List<Order>();

            for (var id = 0; id < 5; id++)
            {
                var index = rand.Next(states.Count - 1);
                var orderNum = rand.Next(1000);

                Order order = new Order()
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
                bulkList.Add(order);
            }

            await BulkInsertAsync(bulkList);





        }
    }
}

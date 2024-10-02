using System;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Cosmos;
using Azure;
using Microsoft.Azure.Cosmos.Scripts;
using System.IO;
using Microsoft.Azure.Cosmos.Fluent;
using System.Data;
using System.ComponentModel;
using Container = Microsoft.Azure.Cosmos.Container;

namespace TheCloudShopsLoader
{
    public class Program
    {
        // The Azure Cosmos DB endpoint for running this sample.
        private static readonly string EndpointUri = ConfigurationManager.AppSettings["EndpointUri"];
        // The primary key for the Azure Cosmos account.
        private static readonly string PrimaryKey = ConfigurationManager.AppSettings["PrimaryKey"];

        // The name of the database and container we will create
        private string databaseId = "DP420Demo";
        private string containerId = "Conflict";

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
            //imit database
            await PrepItem();

            //run updates to make colision
            await Task.WhenAll(
                    UpdateItemInRegionAsync(ConfigurationManager.AppSettings["Region1"]),
                    UpdateItemInRegionAsync(ConfigurationManager.AppSettings["Region2"]),
                    UpdateItemInRegionAsync(ConfigurationManager.AppSettings["Region1"]),
                    UpdateItemInRegionAsync(ConfigurationManager.AppSettings["Region2"]),
                    UpdateItemInRegionAsync(ConfigurationManager.AppSettings["Region1"]),
                    UpdateItemInRegionAsync(ConfigurationManager.AppSettings["Region1"]),
                    UpdateItemInRegionAsync(ConfigurationManager.AppSettings["Region1"]),
                    UpdateItemInRegionAsync(ConfigurationManager.AppSettings["Region2"]),
                    UpdateItemInRegionAsync(ConfigurationManager.AppSettings["Region1"]),
                    UpdateItemInRegionAsync(ConfigurationManager.AppSettings["Region1"]),
                    UpdateItemInRegionAsync(ConfigurationManager.AppSettings["Region1"]),
                    UpdateItemInRegionAsync(ConfigurationManager.AppSettings["Region2"]),
                    UpdateItemInRegionAsync(ConfigurationManager.AppSettings["Region1"]),
                    UpdateItemInRegionAsync(ConfigurationManager.AppSettings["Region2"]),
                    UpdateItemInRegionAsync(ConfigurationManager.AppSettings["Region1"]),
                    UpdateItemInRegionAsync(ConfigurationManager.AppSettings["Region1"]),
                    UpdateItemInRegionAsync(ConfigurationManager.AppSettings["Region1"]),
                    UpdateItemInRegionAsync(ConfigurationManager.AppSettings["Region2"]),
                    UpdateItemInRegionAsync(ConfigurationManager.AppSettings["Region1"]),
                    UpdateItemInRegionAsync(ConfigurationManager.AppSettings["Region1"])
               );

        }

        private async Task PrepItem()
        {
            CosmosClient cosmosClient = new CosmosClientBuilder(EndpointUri, PrimaryKey)
                          .WithApplicationName("DP420demo-app")
                          .Build();

            Database database = await CreateDatabaseAsync(cosmosClient);

            //var container = await CreateContainerDetaultPolicyAsync(database);
            var container = await CreateContainerCustomPolicyAsync(database);

            await CreateAndReadItemAsync(container, "");
        }

        private async Task<bool> UpdateItemInRegionAsync(string region)
        {
            CosmosClient cosmosClient = new CosmosClientBuilder(EndpointUri, PrimaryKey)
                          .WithApplicationRegion(region)
                          .WithApplicationName("DP420demo-app")
                          .Build();


            Database database = cosmosClient.GetDatabase(databaseId);

            Container container = database.GetContainer(containerId);

            await UpdateItemAsync(container, region);

            return true;
        }



        private async Task<Database> CreateDatabaseAsync(CosmosClient client)
        {
            // Create a new database
            var database = (Database)await client.CreateDatabaseIfNotExistsAsync(databaseId);
            Console.WriteLine("Created Database: {0}\n", database.Id);
            return database;
        }


        private async Task<Container> CreateContainerDetaultPolicyAsync(Database database)
        {
            try
            {
                await database.GetContainer(containerId).DeleteContainerAsync();

            }
            catch
            {
                Console.WriteLine("Container does not exists. Buildign new");
            }

            // Define a default custom conflict resolution path as /metadata/sortableTimestamp.
            ContainerProperties properties = new(containerId, "/OrderAddress/City")
            {
                ConflictResolutionPolicy = new ConflictResolutionPolicy()
                {
                    Mode = ConflictResolutionMode.LastWriterWins,
                    ResolutionPath = "/sortableTimestamp"
                }
            };

            // Note: You can only set a conflict resolution policy on newly created containers.
            var container = (Container)await database.CreateContainerAsync(properties);
            Console.WriteLine("Created Container: {0}\n", container.Id);

            return container;
        }

        private async Task<Container> CreateContainerCustomPolicyAsync(Database database)
        {
            var container = database.GetContainer(containerId);
            if (container != null) await container.DeleteContainerAsync();

            ContainerProperties properties = new(containerId, "/OrderAddress/City")
            {
                ConflictResolutionPolicy = new ConflictResolutionPolicy()
                {
                    Mode = ConflictResolutionMode.Custom,
                    ResolutionProcedure = string.Format("dbs/{0}/colls/{1}/sprocs/{2}",
                                                            this.databaseId,
                                                            this.containerId,
                                                            "resolver")

                }
            };

            // Note: You can only set a conflict resolution policy on newly created containers.
            container = (Container)await database.CreateContainerIfNotExistsAsync(properties);

            await container.Scripts.CreateStoredProcedureAsync(
                       new StoredProcedureProperties("resolver", File.ReadAllText(@"function.js"))
            );

            return container;
        }



        private async Task CreateItemAsync(Container container, Order order)
        {

            try
            {
                await container.DeleteItemAsync<Order>(order.id, new PartitionKey(order.OrderAddress.City));

            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("Item {0} not exists\n", order.id);
            }

            ItemResponse<Order> createResponse = await container.CreateItemAsync<Order>(order, new PartitionKey(order.OrderAddress.City));

            Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", createResponse.Resource.id, createResponse.RequestCharge);
        }


        private async Task CreateAndReadItemAsync(Container container, string regionSrc)
        {
            // Create a objects
            Customer customer1 = new Customer() { IsActive = true, Name = "Level4you" };

            Product product1 = new Product() { ProductName = "Book" };
            Product product3 = new Product() { ProductName = "Coffee" };

            Order order1 = new Order()
            {
                id = "001",
                OrderNumber = "NL-001",
                OrderCustomer = customer1,
                OrderAddress = new Address { State = "WA", County = "King", City = "Seattle" },
                OrderItems = new[] {
                    new OrderItem() { ProductItem  = product1, Count = 7 },
                    new OrderItem() { ProductItem  = product3, Count = 1 }
                },
                sortableTimestamp = DateTime.Now.Ticks,
                regionSrc = regionSrc
            };

            await CreateItemAsync(container, order1);
        }

        private async Task UpdateItemAsync(Container container, string regionSrc)
        {
            Order item = await container.ReadItemAsync<Order>("001", new PartitionKey("Seattle"));

            Console.WriteLine($"Item found {item.id}, region {regionSrc}");

            item.regionSrc = regionSrc;
            item.sortableTimestamp = DateTime.Now.Ticks;

            ItemRequestOptions options = new()
            {
                ConsistencyLevel = ConsistencyLevel.Eventual
            };

            var response = await container.UpsertItemAsync<Order>(item, requestOptions: options);

            Console.WriteLine($"Item updated {item.sortableTimestamp}, region {regionSrc}, {response.RequestCharge} RUs");
        }
    }
}

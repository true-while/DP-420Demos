using System;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Cosmos;
using System.Reflection.Metadata;
using System.Diagnostics;
using static Microsoft.Azure.Cosmos.Container;
using System.Threading;

namespace TheCloudShopsLoader
{
    public class Program
    {
        private static readonly string EndpointUri = ConfigurationManager.AppSettings["EndpointUri"];
        // The primary key for the Azure Cosmos account.
        private static readonly string PrimaryKey = ConfigurationManager.AppSettings["PrimaryKey"];

        // The Cosmos client instance
        private CosmosClient cosmosClient;

        // The database we will create
        private Database database;

        // The container we will create.
        private Container container;
        private Container leasecontainer;

        // The name of the database and container we will create
        private string databaseId = "DP420Demo";
        private string containerId = "TheCloudShops";
        private string containerLeaseId = "TheCloudShops-lease";

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

            await this.ProcessFeed();

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

            // Create a lease container
            this.leasecontainer =  await this.database.CreateContainerIfNotExistsAsync(containerLeaseId, "/id");
            Console.WriteLine("Created Container: {0}\n", this.leasecontainer.Id);
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

        private async Task ProcessFeed()
        {
            static async Task HandleChangesAsync(
                ChangeFeedProcessorContext context,
                IReadOnlyCollection<Order> changes,
                CancellationToken cancellationToken)
            {
                Console.WriteLine($"Started handling changes for lease {context.LeaseToken}...");
                Console.WriteLine($"Change Feed request consumed {context.Headers.RequestCharge} RU.");
                // SessionToken if needed to enforce Session consistency on another client instance
                Console.WriteLine($"SessionToken ${context.Headers.Session}");

                // We may want to track any operation's Diagnostics that took longer than some threshold
                if (context.Diagnostics.GetClientElapsedTime() > TimeSpan.FromSeconds(1))
                {
                    Console.WriteLine($"Change Feed request took longer than expected. Diagnostics:" + context.Diagnostics.ToString());
                }

                foreach (Order item in changes)
                {
                    Console.WriteLine($"Detected operation for item with id {item.id}.");
                    // Simulate some asynchronous operation
                    await Task.Delay(10);
                }

                Console.WriteLine("Finished handling changes.");
            }

            // Create an instance for both the source and lease container.
            Container sourceContainer = cosmosClient.GetContainer(databaseId, containerId);
            Container leaseContainer = cosmosClient.GetContainer(databaseId, containerLeaseId);

            // Use the GetChangeFeedProcessorBuilder method from the source container instance to create a builder.
            var procBuilder = sourceContainer.GetChangeFeedProcessorBuilder<Order>(
                processorName: "orderItemProcessor",
                onChangesDelegate: HandleChangesAsync // Delegate defined in previous slide 
            );

            // Build the change feed processor with the defined builder and lease container
            ChangeFeedProcessor processor = procBuilder
                .WithInstanceName("TheCloudShopLoader")
                .WithLeaseContainer(leaseContainer)
                .Build();

            // Run processor asynchronously.
            await processor.StartAsync();

            // Wait while processor handles items 

            Console.ReadKey();

            // Terminate processor asynchronously.
            await processor.StopAsync();


        }

    }
}

using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.CosmosDB;

namespace CosmosDBWithEventualConsistency
{

    public class Program
    {
        public static void Main(string[] args)
        {
            ArmClient client = new ArmClient(new DefaultAzureCredential());

            string resourceGroupName = "DP420";

            SubscriptionResource subscription = client.GetDefaultSubscription();
            Console.WriteLine($"SubID:{subscription.Id}");

            ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();
            ResourceGroupResource resourceGroup = resourceGroups.Get(resourceGroupName);

  
            foreach (var account in resourceGroup.GetCosmosDBAccounts())
            {
                foreach (var db in account.GetCosmosDBSqlDatabases())
                {
                    foreach (var container in db.GetCosmosDBSqlContainers())
                    {
                        Console.WriteLine($"Account: {account.Data.Name}, DB: {db.Data.Name}, Container: {container.Data.Name}");
                    }
                }
            }

        }

    }
}
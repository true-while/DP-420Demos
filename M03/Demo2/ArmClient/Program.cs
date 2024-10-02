using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.CosmosDB;
using System.Configuration;

namespace CosmosDBWithEventualConsistency
{

    public class Program
    {

        //private static readonly string SubscriptionID = ConfigurationManager.AppSettings["subscriptionid"];
        //private static readonly string TenantID = ConfigurationManager.AppSettings["tenantid"];
        private static readonly string resourceGroupName = "DP-420";

        public static void Main(string[] args)
        {
            ArmClient client = new ArmClient(new DefaultAzureCredential();

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
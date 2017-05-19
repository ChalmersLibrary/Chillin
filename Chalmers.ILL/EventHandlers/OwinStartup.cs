using Chalmers.ILL.MediaItems;
using Owin;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.Linq;

namespace Chalmers.ILL.EventHandlers
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // If we are using the Azure Storage Emulator we need to make sure it is installed and started.
            if (ConfigurationManager.AppSettings["BlobStorageConnectionString"] == "UseDevelopmentStorage=true" ||
                ConfigurationManager.AppSettings["BlobStorageConnectionString"] == "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1;TableEndpoint=http://localhost:10002/devstoreaccount1;QueueEndpoint=http://localhost:10001/devstoreaccount1;")
            {
                //AzureStorageEmulatorManager.Start(); // We start Azure Storage Emulator manually instead.
            }

            // Should we check for pending database migrations and apply them?
            if (bool.Parse(ConfigurationManager.AppSettings["checkForPendingDatabaseMigrations"]))
            {
                var configuration = new Migrations.Configuration();
                var migrator = new DbMigrator(configuration);
                if (migrator.GetPendingMigrations().Count() > 0)
                {
                    migrator.Update();
                }
            }

            Bootstrapper.Initialise();

            // Any connection or hub wire up and configuration should go here
            app.MapSignalR();
        }
    }
}
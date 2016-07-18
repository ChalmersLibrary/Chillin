using Chalmers.ILL.MediaItems;
using Chalmers.ILL.Utilities;
using Microsoft.Owin;
using Owin;
using System.Configuration;
using Umbraco.Core.Logging;

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
                AzureStorageEmulatorManager.Start();
            }

            Bootstrapper.Initialise();

            // Any connection or hub wire up and configuration should go here
            app.MapSignalR();
        }
    }
}
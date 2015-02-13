using Chalmers.ILL.Utilities;
using Microsoft.Owin;
using Owin;
using Umbraco.Core.Logging;

namespace Chalmers.ILL.EventHandlers
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            Bootstrapper.Initialise();

            Helpers.PopulateCacheWithDataTypePreValues();

            // Any connection or hub wire up and configuration should go here
            app.MapSignalR();
        }
    }
}
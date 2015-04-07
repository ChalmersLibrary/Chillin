using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chalmers.ILL.Providers
{
    public interface IProviderService
    {
        List<String> FetchAndCreateListOfUsedProviders();
        int GetSuggestedDeliveryTimeInHoursForProvider(string providerName);
    }
}

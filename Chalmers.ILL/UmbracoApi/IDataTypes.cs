using Chalmers.ILL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chalmers.ILL.UmbracoApi
{
    public interface IDataTypes
    {
        List<UmbracoDropdownListNtextDataType> GetAvailableTypes();
        List<UmbracoDropdownListNtextDataType> GetAvailableStatuses();
        List<UmbracoDropdownListNtextDataType> GetAvailableDeliveryLibraries();
        List<UmbracoDropdownListNtextDataType> GetAvailableCancellationReasons();
        List<UmbracoDropdownListNtextDataType> GetAvailablePurchasedMaterials();

        void PopulateModelWithAvailableValues(OrderItemPageModelBase model);
    }
}

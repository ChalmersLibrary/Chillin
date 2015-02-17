using Chalmers.ILL.Models;
using Chalmers.ILL.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using umbraco.cms.businesslogic.datatype;

namespace Chalmers.ILL.UmbracoApi
{
    public class DataTypes : IDataTypes
    {
        public List<UmbracoDropdownListNtextDataType> GetAvailableTypes()
        {
            return GetAvailableValues(ConfigurationManager.AppSettings["umbracoOrderTypeDataTypeDefinitionName"]);
        }

        public List<UmbracoDropdownListNtextDataType> GetAvailableStatuses()
        {
            return GetAvailableValues(ConfigurationManager.AppSettings["umbracoOrderStatusDataTypeDefinitionName"]);
        }

        public List<UmbracoDropdownListNtextDataType> GetAvailableDeliveryLibraries()
        {
            return GetAvailableValues(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"]);
        }

        public List<UmbracoDropdownListNtextDataType> GetAvailableCancellationReasons()
        {
            return GetAvailableValues(ConfigurationManager.AppSettings["umbracoOrderCancellationReasonDataTypeDefinitionName"]);
        }

        public List<UmbracoDropdownListNtextDataType> GetAvailablePurchasedMaterials()
        {
            return GetAvailableValues(ConfigurationManager.AppSettings["umbracoOrderPurchasedMaterialDataTypeDefinitionName"]);
        }

        private List<UmbracoDropdownListNtextDataType> GetAvailableValues(string dataTypeName)
        {
            // Get a sorted list of all prevalues
            SortedList typeTypes = Helpers.GetPreValues(dataTypeName);

            // Get the datatype enumerator (to sort as in Backoffice)
            IDictionaryEnumerator i = typeTypes.GetEnumerator();

            // Create the list which will hold the values
            var ret = new List<UmbracoDropdownListNtextDataType>();

            // Counter for sort order in return list
            int sortOrder = 0;

            // Move trough the enumerator
            while (i.MoveNext())
            {
                // Get the prevalue (text) using umbraco.cms.businesslogic.datatype
                PreValue statusType = (PreValue)i.Value;
                var r = new UmbracoDropdownListNtextDataType();

                // Add to the instanced model OrderItemStatusModel
                r.Id = statusType.Id;
                r.Order = sortOrder;

                // If we have "08:Something" then just return last part
                r.Value = statusType.Value.Split(':').Last();

                // Add to our statusList
                ret.Add(r);
                sortOrder++;
            }

            return ret;
        }
    }
}
using Chalmers.ILL.Models;
using Chalmers.ILL.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using umbraco.cms.businesslogic.datatype;
using umbraco.cms.businesslogic.member;
using umbraco.cms.businesslogic.relation;
using Umbraco.Core.Logging;
using Umbraco.Web;

namespace Chalmers.ILL.UmbracoApi
{
    public class UmbracoWrapper : IUmbracoWrapper
    {
        UmbracoHelper _umbraco = new UmbracoHelper(UmbracoContext.Current);

        public UmbracoWrapper()
        {
            PopulateCacheWithDataTypePreValues();
        }

        public RelationType GetRelationTypeByAlias(string relationTypeStr)
        {
            return RelationType.GetByAlias(relationTypeStr);
        }

        public List<Relation> GetRelationsAsList(int nodeId)
        {
            return Relation.GetRelationsAsList(nodeId);
        }

        public Relation MakeNewRelation(int parentId, int childId, RelationType relationType, string comment)
        {
            return Relation.MakeNew(parentId, childId, relationType, comment);
        }

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

        /// <summary>
        /// Get all the prevalues for a given data type.
        /// </summary>
        /// <param name="dataTypeName">The name of the data type.</param>
        /// <returns>A sorted list with the prevalues.</returns>
        public SortedList GetPreValues(string dataTypeName)
        {
            // Get a sorted list of all prevalues from the cache
            var c = System.Web.HttpContext.Current.Cache;
            SortedList statusTypes = c.Get(dataTypeName) as SortedList;

            if (statusTypes == null)
            {
                // Connect to Umbraco DataTypeService
                var ds = new Umbraco.Core.Services.DataTypeService();

                // Get the Definition Id
                int dataTypeDefinitionId = ds.GetAllDataTypeDefinitions().First(x => x.Name == dataTypeName).Id;

                // Get a sorted list of all prevalues and store it in the cache
                statusTypes = PreValues.GetPreValues(dataTypeDefinitionId);
                c.Insert(dataTypeName, statusTypes);
            }

            return statusTypes;
        }

        public void PopulateModelWithAvailableValues(OrderItemPageModelBase model)
        {
            model.AvailableCancellationReasons = GetAvailableCancellationReasons();
            model.AvailableDeliveryLibraries = GetAvailableDeliveryLibraries();
            model.AvailablePurchasedMaterials = GetAvailablePurchasedMaterials();
            model.AvailableStatuses = GetAvailableStatuses();
            model.AvailableTypes = GetAvailableTypes();
        }

        public IEnumerable<Umbraco.Core.Models.IPublishedContent> TypedContentAtXPath(string xpath)
        {
            return _umbraco.TypedContentAtXPath(xpath);
        }

        public Member GetMember(int id)
        {
            return new Member(id);
        }

        public void LogError<T>(string msg, Exception e)
        {
            LogHelper.Error<T>(msg, e);
        }

        public void LogWarn<T>(string msg)
        {
            LogHelper.Warn<T>(msg);
        }

        public void LogInfo<T>(string msg)
        {
            LogHelper.Info<T>(msg);
        }

        public void LogDebug<T>(string msg)
        {
            LogHelper.Debug<T>(msg);
        }

        #region Private methods

        private void PopulateCacheWithDataTypePreValues()
        {
            var c = System.Web.HttpContext.Current.Cache;
            var ds = new Umbraco.Core.Services.DataTypeService();

            foreach (var dtd in ds.GetAllDataTypeDefinitions())
            {
                c.Insert(dtd.Name, PreValues.GetPreValues(dtd.Id));
            }
        }

        private List<UmbracoDropdownListNtextDataType> GetAvailableValues(string dataTypeName)
        {
            // Get a sorted list of all prevalues
            SortedList typeTypes = GetPreValues(dataTypeName);

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

        #endregion
    }
}
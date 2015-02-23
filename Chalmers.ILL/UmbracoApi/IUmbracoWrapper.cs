using Chalmers.ILL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using umbraco.cms.businesslogic.relation;

namespace Chalmers.ILL.UmbracoApi
{
    public interface IUmbracoWrapper
    {
        RelationType GetRelationTypeByAlias(string relationTypeStr);
        List<Relation> GetRelationsAsList(int nodeId);
        Relation MakeNewRelation(int parentId, int childId, RelationType relationType, string comment);

        List<UmbracoDropdownListNtextDataType> GetAvailableTypes();
        List<UmbracoDropdownListNtextDataType> GetAvailableStatuses();
        List<UmbracoDropdownListNtextDataType> GetAvailableDeliveryLibraries();
        List<UmbracoDropdownListNtextDataType> GetAvailableCancellationReasons();
        List<UmbracoDropdownListNtextDataType> GetAvailablePurchasedMaterials();
        void PopulateModelWithAvailableValues(OrderItemPageModelBase model);

        IEnumerable<Umbraco.Core.Models.IPublishedContent> TypedContentAtXPath(string xpath);

        void LogWarn<T>(string msg);
    }
}

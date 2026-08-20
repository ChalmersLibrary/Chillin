using Chalmers.ILL.Models;
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

        public int GetPropertyValueAsInteger(object property)
        {
            int returnValue = -1;

            if (property != null)
            {
                if (!Int32.TryParse(property.ToString(), out returnValue))
                {
                    returnValue = -1;
                }
            }

            return returnValue;
        }

        public int DataTypePrevalueId(string dataTypeName, string prevalue)
        {
            int ret = -1;

            SortedList statusTypes = GetPreValues(dataTypeName);

            IDictionaryEnumerator i = statusTypes.GetEnumerator();

            while (i.MoveNext())
            {
                PreValue statusType = (PreValue)i.Value;

                if (statusType.Value == prevalue)
                {
                    ret = statusType.Id;
                }
            }

            return ret;
        }

        public SortedList GetPreValues(string dataTypeName)
        {
            var c = HttpContext.Current.Cache;
            SortedList statusTypes = c.Get(dataTypeName) as SortedList;

            if (statusTypes == null)
            {
                var ds = new Umbraco.Core.Services.DataTypeService();
                int dataTypeDefinitionId = ds.GetAllDataTypeDefinitions().First(x => x.Name == dataTypeName).Id;
                statusTypes = PreValues.GetPreValues(dataTypeDefinitionId);
                c.Insert(dataTypeName, statusTypes);
            }

            return statusTypes;
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
    }
}

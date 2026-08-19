using Chalmers.ILL.Models.Page;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Chalmers.ILL.Members
{
    public interface IMemberInfoManager
    {
        int GetCurrentMemberId(HttpRequestBase request, HttpResponseBase response);

        string GetCurrentMemberText(HttpRequestBase request, HttpResponseBase response);

        string GetCurrentMemberLoginName(HttpRequestBase request, HttpResponseBase response);

        void PopulateModelWithMemberData(HttpRequestBase request, HttpResponseBase response, ChalmersILLModel model);

        void AddMemberToCache(HttpResponseBase response, int memberId, string memberText, string memberLoginName);

        void ClearMemberCache(HttpResponseBase response);
    }
}

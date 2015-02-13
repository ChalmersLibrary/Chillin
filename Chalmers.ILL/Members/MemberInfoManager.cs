using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using umbraco.cms.businesslogic.member;
using Umbraco.Core.Logging;

namespace Chalmers.ILL.Members
{
    public class MemberInfoManager : IMemberInfoManager
    {
        public const string cookieKey = "ChalmersILL";
        public const string memberIdKey = "memberId";
        public const string memberTextKey = "memberText";
        public const string memberLoginNameKey = "memberLoginName";

        public int GetCurrentMemberId(HttpRequestBase request, HttpResponseBase response)
        {
            string memberIdObject = null;
            int memberId = 0;

            if (request != null && request.Cookies[MemberInfoManager.cookieKey] != null)
            {
                memberIdObject = request.Cookies[MemberInfoManager.cookieKey][MemberInfoManager.memberIdKey];
            }

            if (memberIdObject != null)
            {
                memberId = Convert.ToInt32(Uri.UnescapeDataString(memberIdObject.ToString()));
            }
            else
            {
                memberId = GetCurrentMember(response).Id;
            }

            return memberId;
        }

        public string GetCurrentMemberText(HttpRequestBase request, HttpResponseBase response)
        {
            string memberTextObject = null;
            string memberText = "";

            if (request != null && request.Cookies[MemberInfoManager.cookieKey] != null)
            {
                memberTextObject = request.Cookies[MemberInfoManager.cookieKey][MemberInfoManager.memberTextKey];
            }

            if (memberTextObject != null)
            {
                memberText = Uri.UnescapeDataString(memberTextObject.ToString());
            }
            else
            {
                memberText = GetCurrentMember(response).Text;
            }

            return memberText;
        }

        public string GetCurrentMemberLoginName(HttpRequestBase request, HttpResponseBase response)
        {
            string memberLoginNameObject = null;
            string memberLoginName = "";

            if (request != null && request.Cookies[MemberInfoManager.cookieKey] != null)
            {
                memberLoginNameObject = request.Cookies[MemberInfoManager.cookieKey][MemberInfoManager.memberLoginNameKey];
            }

            if (memberLoginNameObject != null)
            {
                memberLoginName = Uri.UnescapeDataString(memberLoginNameObject.ToString());
            }
            else
            {
                memberLoginName = GetCurrentMember(response).LoginName;
            }

            return memberLoginName;
        }

        public void AddMemberToCache(HttpResponseBase response, Member member)
        {
            response.Cookies[MemberInfoManager.cookieKey][MemberInfoManager.memberIdKey] = Uri.EscapeUriString(Convert.ToString(member.Id));
            response.Cookies[MemberInfoManager.cookieKey][MemberInfoManager.memberTextKey] = Uri.EscapeUriString(member.Text);
            response.Cookies[MemberInfoManager.cookieKey][MemberInfoManager.memberLoginNameKey] = Uri.EscapeUriString(member.LoginName);
            response.Cookies[MemberInfoManager.cookieKey].Expires = DateTime.Now.AddDays(1);
        }

        public void ClearMemberCache(HttpResponseBase response)
        {
            response.Cookies[MemberInfoManager.cookieKey].Expires = DateTime.Now.AddDays(-1);
        }

        #region Private methods

        private Member GetCurrentMember(HttpResponseBase response)
        {
            var member = Member.GetCurrentMember();
            response.Cookies[MemberInfoManager.cookieKey][MemberInfoManager.memberIdKey] = Uri.EscapeUriString(Convert.ToString(member.Id));
            response.Cookies[MemberInfoManager.cookieKey][MemberInfoManager.memberTextKey] = Uri.EscapeUriString(member.Text);
            response.Cookies[MemberInfoManager.cookieKey][MemberInfoManager.memberLoginNameKey] = Uri.EscapeUriString(member.LoginName);
            return member;
        }
    }
}

        #endregion
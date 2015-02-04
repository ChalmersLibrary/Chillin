using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using umbraco.cms.businesslogic.member;
using Umbraco.Core.Logging;

namespace Chalmers.ILL.Wrappers
{
    public class MemberWrapper
    {
        public const string cookieKey = "ChalmersILL";
        public const string memberIdKey = "memberId";
        public const string memberTextKey = "memberText";
        public const string memberLoginNameKey = "memberLoginName";

        /// <summary>
        /// Get the member id of the current logged on member.
        /// </summary>
        /// <remarks>Adds fetched current member to cache on cache miss.</remarks>
        /// <param name="response">The current HTTP response.</param>
        /// <returns>The member id of the current logged on member.</returns>
        public static int GetCurrentMemberId(HttpRequestBase request, HttpResponseBase response)
        {
            string memberIdObject = null;
            int memberId = 0;

            if (request != null && request.Cookies[MemberWrapper.cookieKey] != null)
            {
                memberIdObject = request.Cookies[MemberWrapper.cookieKey][MemberWrapper.memberIdKey];
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

        /// <summary>
        /// Get the member text of the current logged on member.
        /// </summary>
        /// <remarks>Adds fetched current member to cache on cache miss.</remarks>
        /// <param name="response">The current HTTP response.</param>
        /// <returns>The member text of the current logged on member.</returns>
        public static string GetCurrentMemberText(HttpRequestBase request, HttpResponseBase response)
        {
            string memberTextObject = null;
            string memberText = "";

            if (request != null && request.Cookies[MemberWrapper.cookieKey] != null)
            {
                memberTextObject = request.Cookies[MemberWrapper.cookieKey][MemberWrapper.memberTextKey];
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

        /// <summary>
        /// Get the member login name of the current logged on member.
        /// </summary>
        /// <remarks>Adds fetched current member to cache on cache miss.</remarks>
        /// <param name="response">The current HTTP response.</param>
        /// <returns>The member login name of the current logged on member.</returns>
        public static string GetCurrentMemberLoginName(HttpRequestBase request, HttpResponseBase response)
        {
            string memberLoginNameObject = null;
            string memberLoginName = "";

            if (request != null && request.Cookies[MemberWrapper.cookieKey] != null)
            {
                memberLoginNameObject = request.Cookies[MemberWrapper.cookieKey][MemberWrapper.memberLoginNameKey];
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

        /// <summary>
        /// Adds the members Id, Text and LoginName to the cache.
        /// </summary>
        /// <remarks>The cache only holds one member.</remarks>
        /// <param name="response">The current HTTP response.</param>
        /// <param name="member">The member whos information should be added to the cache.</param>
        public static void AddMemberToCache(HttpResponseBase response, Member member)
        {
            response.Cookies[MemberWrapper.cookieKey][MemberWrapper.memberIdKey] = Uri.EscapeUriString(Convert.ToString(member.Id));
            response.Cookies[MemberWrapper.cookieKey][MemberWrapper.memberTextKey] = Uri.EscapeUriString(member.Text);
            response.Cookies[MemberWrapper.cookieKey][MemberWrapper.memberLoginNameKey] = Uri.EscapeUriString(member.LoginName);
            response.Cookies[MemberWrapper.cookieKey].Expires = DateTime.Now.AddDays(1);
        }

        /// <summary>
        /// Removes the single member cache.
        /// </summary>
        /// <param name="response">The current HTTP response.</param>
        public static void ClearMemberCache(HttpResponseBase response)
        {
            response.Cookies[MemberWrapper.cookieKey].Expires = DateTime.Now.AddDays(-1);
        }


        private static Member GetCurrentMember(HttpResponseBase response)
        {
            var member = Member.GetCurrentMember();
            response.Cookies[MemberWrapper.cookieKey][MemberWrapper.memberIdKey] = Uri.EscapeUriString(Convert.ToString(member.Id));
            response.Cookies[MemberWrapper.cookieKey][MemberWrapper.memberTextKey] = Uri.EscapeUriString(member.Text);
            response.Cookies[MemberWrapper.cookieKey][MemberWrapper.memberLoginNameKey] = Uri.EscapeUriString(member.LoginName);
            return member;
        }
    }
}
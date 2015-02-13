using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using umbraco.cms.businesslogic.member;

namespace Chalmers.ILL.Members
{
    public interface IMemberInfoManager
    {
        /// <summary>
        /// Get the member id of the current logged on member.
        /// </summary>
        /// <remarks>Adds fetched current member to cache on cache miss.</remarks>
        /// <param name="response">The current HTTP response.</param>
        /// <returns>The member id of the current logged on member.</returns>
        int GetCurrentMemberId(HttpRequestBase request, HttpResponseBase response);

        /// <summary>
        /// Get the member text of the current logged on member.
        /// </summary>
        /// <remarks>Adds fetched current member to cache on cache miss.</remarks>
        /// <param name="response">The current HTTP response.</param>
        /// <returns>The member text of the current logged on member.</returns>
        string GetCurrentMemberText(HttpRequestBase request, HttpResponseBase response);

        /// <summary>
        /// Get the member login name of the current logged on member.
        /// </summary>
        /// <remarks>Adds fetched current member to cache on cache miss.</remarks>
        /// <param name="response">The current HTTP response.</param>
        /// <returns>The member login name of the current logged on member.</returns>
        string GetCurrentMemberLoginName(HttpRequestBase request, HttpResponseBase response);

        /// <summary>
        /// Adds the members Id, Text and LoginName to the cache.
        /// </summary>
        /// <remarks>The cache only holds one member.</remarks>
        /// <param name="response">The current HTTP response.</param>
        /// <param name="member">The member whos information should be added to the cache.</param>
        void AddMemberToCache(HttpResponseBase response, Member member);

        /// <summary>
        /// Removes the single member cache.
        /// </summary>
        /// <param name="response">The current HTTP response.</param>
        void ClearMemberCache(HttpResponseBase response);
    }
}

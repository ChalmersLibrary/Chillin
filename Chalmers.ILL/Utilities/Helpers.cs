using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using umbraco.cms.businesslogic.datatype;

namespace Chalmers.ILL.Utilities
{

    /// <summary>
    /// This class contains different helpers that we can use to make life easier.
    /// </summary>
    /// 
    public class Helpers
    {
        // const string CONST_DATATYPE_STATUS_GUID = "6a9841ff-39f0-480c-9e13-5612b4b59093";

        /// <summary>
        /// Helper for converting Umbraco property to positive integer, returns -1 if unable to parse
        /// </summary>
        /// <param name="property">Umbraco property object</param>
        /// <returns>Parsed integer of property</returns>
        public static int GetPropertyValueAsInteger(object property)
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

        /// <summary>
        /// Get the DataTypePrevalueId for a DataTypePrevalue
        /// </summary>
        /// <param name="prevalue">Prevalue string</param>
        /// <returns>Prevalue Id</returns>
        public static int DataTypePrevalueId(string dataTypeName, string prevalue)
        {
            // Connect to Umbraco DataTypeService
            var ds = new Umbraco.Core.Services.DataTypeService();

            // Get the Definition Id
            int dataTypeDefinitionId = ds.GetAllDataTypeDefinitions().First(x => x.Name == dataTypeName).Id;

            // Get a sorted list of all prevalues
            SortedList statusTypes = PreValues.GetPreValues(dataTypeDefinitionId);

            // Get the datatype enumerator (to sort as in Backoffice)
            IDictionaryEnumerator i = statusTypes.GetEnumerator();

            // Move trough the enumerator
            while (i.MoveNext())
            {
                // Get the prevalue (text) using umbraco.cms.businesslogic.datatype
                PreValue statusType = (PreValue)i.Value;

                // Check if it's the prevalue we want the id for
                if (statusType.Value == prevalue)
                {
                    return statusType.Id;
                }
            }

            return -1;
        }

        public static string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString();
        }

        public static string HtmlToPlainText(string html)
        {
            const string tagWhiteSpace = @"(\r\n)+";
            const string initialLineBreak = @"^(\n)+";
            const string emailFormatStart = @"(&lt;\n)";
            const string emailFormatEnd = @"(\n&gt;)";
            var tagWhiteSpaceRegex = new Regex(tagWhiteSpace, RegexOptions.Multiline);
            var initialLineBreakRegex = new Regex(initialLineBreak, RegexOptions.Multiline);
            var emailFormatStartRegex = new Regex(emailFormatStart, RegexOptions.Multiline);
            var emailFormatEndRegex = new Regex(emailFormatEnd, RegexOptions.Multiline);

            var text = html;

            //Decode html specific characters
            // text = System.Net.WebUtility.HtmlDecode(text);

            //Remove tag whitespace/line breaks/other shit
            text = tagWhiteSpaceRegex.Replace(text, "\n");
            text = initialLineBreakRegex.Replace(text, "");
            text = emailFormatStartRegex.Replace(text, "&lt;");
            text = emailFormatEndRegex.Replace(text, "&gt;");

            return text;
        }
    }
}
using Examine;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using Chalmers.ILL.Extensions;
using System.Globalization;
using Chalmers.ILL.OrderItems;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    public class PublicDataSurfaceController : SurfaceController
    {
        IBulkDataManager _bulkDataManager;

        public PublicDataSurfaceController(IBulkDataManager bulkDataManager)
        {
            _bulkDataManager = bulkDataManager;
        }

        [HttpGet]
        public ActionResult GetChillinDataForSierraPatron(int recordId, string lang)
        {
            var res = new PublicChillinDataConnectedToPatron();

            Response.AddHeader("Access-Control-Allow-Origin", "*");

            if (lang == null)
            {
                lang = "en";
            }

            try
            {
                res.Items = _bulkDataManager.GetChillinDataForSierraPatron(recordId, lang);
                res.Success = true;
                res.Message = "Successfully fetched data.";
            }
            catch (Exception)
            {
                res.Success = false;
                res.Message = "Failed to get data.";
            }

            return Json(res, JsonRequestBehavior.AllowGet);
        }

        #region Private methods

        private Boolean DueDateIsValid(DateTime dueDate)
        {
            return dueDate != null && dueDate.Year != 1970;
        }

        #endregion

        private class PublicChillinDataConnectedToPatron
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public List<SimplifiedOrderItem> Items { get; set; }
        }
    }
}

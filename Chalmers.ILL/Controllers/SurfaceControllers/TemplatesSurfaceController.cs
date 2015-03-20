using Chalmers.ILL.Models;
using Chalmers.ILL.Models.PartialPage;
using Chalmers.ILL.Templates;
using Examine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Umbraco.Core.Services;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    [MemberAuthorize(AllowType = "Standard")]
    public class TemplatesSurfaceController : SurfaceController
    {
        ITemplateService _templateService;

        public TemplatesSurfaceController(ITemplateService templateService)
        {
            _templateService = templateService;
        }

        [HttpGet]
        public ActionResult RenderEditTemplatesAction()
        {
            var pageModel = new ChalmersILLEditTemplatesModel();

            _templateService.PopulateTemplateList(pageModel.Templates);
            PopulateAvailableOrderItemProperties(pageModel.AvailableOrderItemProperties);

            return PartialView("Chalmers.ILL.EditTemplates", pageModel);
        }

        [HttpGet]
        public ActionResult GetTemplateData(int nodeId)
        {
            var json = new ResultResponseWithStringData();

            try
            {
                json.Success = true;
                json.Message = "Lyckades med att hitta mall.";
                json.Data = _templateService.GetTemplateData(nodeId);
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Misslyckades med att hitta malldata: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult SetTemplateData(int nodeId, string data)
        {
            var json = new ResultResponse();

            try
            {
                _templateService.SetTemplateData(nodeId, data);

                json.Success = true;
                json.Message = "Sparade ny malldata.";
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Error: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        #region Private methods.

        private List<string> PopulateAvailableOrderItemProperties(List<string> list)
        {
            foreach (var property in typeof(OrderItemModel).GetProperties())
            {
                list.Add(property.Name);
            }

            return list;
        }

        #endregion
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Web.Mvc;
using umbraco.cms.businesslogic.member;
using umbraco.cms.businesslogic.datatype;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
using Chalmers.ILL.Models;
using Chalmers.ILL.Utilities;
using System.Web.Security;
using System.IO;
using System.Net;
using System.Threading;
using Umbraco.Core.Events;
using Umbraco.Core.Services;
using Umbraco.Core.Models;
using System.Text.RegularExpressions;
using System.Configuration;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    [MemberAuthorize(AllowType = "Standard")]
    public class ImportDocumentSurfaceController : SurfaceController
    {
        /// <summary>
        /// Import a document from a given URL and store it as a media item bound to the given order item.
        /// </summary>
        /// <param name="orderItemNodeId">The id of the order item which the document should be bound to.</param>
        /// <param name="url">The url from which we should fetch the document.</param>
        /// <returns>Returns a result indicating how the request went.</returns>
        [HttpPost]
        public ActionResult ImportFromUrl(int orderItemNodeId, string url)
        {
            // Json response
            var json = new ResultResponse();

            Stream stream = null;
            int bytesToRead = 10000;
            byte[] buffer = new Byte[bytesToRead];

            try
            {
                HttpWebRequest fileReq = (HttpWebRequest)HttpWebRequest.Create(url);
                fileReq.CookieContainer = new CookieContainer();
                fileReq.AllowAutoRedirect = true;
                HttpWebResponse fileResp = (HttpWebResponse)fileReq.GetResponse();
                stream = fileResp.GetResponseStream();

                var ms = Services.MediaService;
                var cs = Services.ContentService;
                var mainFolder = ms.GetChildren(-1).First(m => m.Name == ConfigurationManager.AppSettings["umbracoOrderItemAttachmentsMediaFolderName"]);
                using (Semaphore semLock = new Semaphore(0, 1))
                {
                    TypedEventHandler<IMediaService, SaveEventArgs<IMedia>> handler = (sender, e) => { semLock.Release(e.SavedEntities.Count()); };
                    try
                    {
                        MediaService.Saved += handler;

                        var orderItem = cs.GetById(orderItemNodeId);
                        var cdPattern = new Regex("filename=\"?(.*)\"?(?:;|$)");
                        var fileEndingPattern = new Regex("(?i)\\.(?:pdf|txt|tif|tiff)$");
                        string name = "";
                        if (fileEndingPattern.IsMatch(url))
                        {
                            name = fileResp.ResponseUri.AbsoluteUri.Substring(fileResp.ResponseUri.AbsoluteUri.LastIndexOf("/") + 1);
                        }
                        else
                        {
                            name = cdPattern.Match(fileResp.Headers["Content-Disposition"]).Groups[1].Value;
                        }

                        if (String.IsNullOrEmpty(name))
                        {
                            json.Success = false;
                            json.Message = "Not a valid document.";
                        }
                        else
                        {
                            var media = ms.CreateMedia(orderItem.GetValue("orderId").ToString() + "-" + name, mainFolder, "OrderItemAttachment");
                            media.SetValue("file", name, stream);
                            media.SetValue("orderItemNodeId", orderItem.Id);

                            // Save the media and wait until it is finished so that we can retrieve the link to the item.
                            ms.Save(media);
                            semLock.WaitOne();

                            // cleanup, memory stream not needed any longer
                            stream.Dispose();

                            OrderItemAttachments.AddOrderItemAttachment(orderItem.Id, media.Id, name, media.GetValue("file").ToString());

                            json.Success = true;
                            json.Message = "Document imported successfully.";
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        MediaService.Saved -= handler;
                    }
                }
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Failed to import document: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Import a document from data and store it as a media item bound to the given order item.
        /// </summary>
        /// <param name="orderItemNodeId">The id of the order item which the document should be bound to.</param>
        /// <param name="filename">The filename of the original file, which we use as name for the document.</param>
        /// <param name="data">The base 64 encoded data which we should use to create the document.</param>
        /// <returns>Returns a result indicating how the request went.</returns>
        [HttpPost]
        public ActionResult ImportFromData(int orderItemNodeId, string filename, string data)
        {
            // Json response
            var json = new ResultResponse();

            try
            {
                var ms = Services.MediaService;
                var cs = Services.ContentService;
                var mainFolder = ms.GetChildren(-1).First(m => m.Name == ConfigurationManager.AppSettings["umbracoOrderItemAttachmentsMediaFolderName"]);
                using (Semaphore semLock = new Semaphore(0, 1))
                {
                    TypedEventHandler<IMediaService, SaveEventArgs<IMedia>> handler = (sender, e) => { semLock.Release(e.SavedEntities.Count()); };
                    try
                    {
                        MediaService.Saved += handler;

                        var orderItem = cs.GetById(orderItemNodeId);
                        if (String.IsNullOrEmpty(filename))
                        {
                            json.Success = false;
                            json.Message = "Not a valid document.";
                        }
                        else
                        {
                            var media = ms.CreateMedia(orderItem.GetValue("orderId").ToString() + "-" + filename, mainFolder, "OrderItemAttachment");
                            var stream = new MemoryStream(Convert.FromBase64String(Regex.Split(data, "base64[;,]")[1]));
                            media.SetValue("file", filename, stream);
                            media.SetValue("orderItemNodeId", orderItem.Id);

                            // Save the media and wait until it is finished so that we can retrieve the link to the item.
                            ms.Save(media);
                            semLock.WaitOne();

                            // cleanup, memory stream not needed any longer
                            stream.Dispose();

                            OrderItemAttachments.AddOrderItemAttachment(orderItem.Id, media.Id, filename, media.GetValue("file").ToString());

                            json.Success = true;
                            json.Message = media.Id.ToString() + ";" + media.GetValue("file").ToString();
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        MediaService.Saved -= handler;
                    }
                }
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Failed to import document from data: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }
    }
}
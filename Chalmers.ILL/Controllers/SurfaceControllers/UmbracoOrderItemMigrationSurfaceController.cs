using Chalmers.ILL.MediaItems;
using Chalmers.ILL.Models;
using Chalmers.ILL.OrderItems;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    public class UmbracoOrderItemMigrationSurfaceController : SurfaceController
    {
        private IOrderItemManager _legacyOrderItemManager;
        private IMediaItemManager _legacyMediaItemManager;
        private IOrderItemManager _orderItemManager;
        private IMediaItemManager _mediaItemManager;

        public UmbracoOrderItemMigrationSurfaceController(
            [Dependency("Legacy")] IOrderItemManager legacyOrderItemManager,
            [Dependency("Legacy")] IMediaItemManager legacyMediaItemManager, 
            IOrderItemManager orderItemManager,
            IMediaItemManager mediaItemManager)
        {
            _legacyOrderItemManager = legacyOrderItemManager;
            _legacyMediaItemManager = legacyMediaItemManager;
            _orderItemManager = orderItemManager;
            _mediaItemManager = mediaItemManager;
        }

        [HttpPost]
        public ActionResult MigrateOrderItem(int nodeId)
        {
            var json = new ResultResponse();

            try
            {
                var orderItemToBeMoved = _legacyOrderItemManager.GetOrderItem(nodeId);

                // There is an item to move.
                if (orderItemToBeMoved != null)
                {
                    var existingOrderItem = _orderItemManager.GetOrderItem(orderItemToBeMoved.OrderId);

                    // There is no item with the same order id already in the store.
                    if (existingOrderItem == null)
                    {
                        // Copy and clear attachments and create the order item without attachments in the storage.
                        var attachmentsToCopy = new List<OrderAttachment>();
                        foreach (var attachmentToBeMoved in orderItemToBeMoved.AttachmentList)
                        {
                            attachmentsToCopy.Add(attachmentToBeMoved);
                        }
                        orderItemToBeMoved.AttachmentList.Clear();
                        orderItemToBeMoved.Attachments = JsonConvert.SerializeObject(orderItemToBeMoved.AttachmentList);

                        var newOrderItemNodeId = _orderItemManager.CreateOrderItemInDbFromOrderItemModel(orderItemToBeMoved);
                        var newOrderItem = _orderItemManager.GetOrderItem(newOrderItemNodeId);

                        if (newOrderItem != null)
                        {
                            // Create all the connected media items in the media storage.
                            var newMediaItems = new List<MediaItemModel>();
                            bool successfullyAddedAllMediaItems = true;
                            string failedMediaItemId = "";
                            string failedMediaItemName = "";
                            foreach (var attachmentToCopy in attachmentsToCopy)
                            {
                                var mediaItemToBeMoved = _legacyMediaItemManager.GetOne(attachmentToCopy.MediaItemNodeId);

                                // Calculate the content type.
                                string contentType = "";
                                if (mediaItemToBeMoved.Name.EndsWith(".pdf"))
                                {
                                    contentType = "application/pdf";
                                }
                                else if (mediaItemToBeMoved.Name.EndsWith(".txt"))
                                {
                                    contentType = "text/plain";
                                }
                                else if (mediaItemToBeMoved.Name.EndsWith(".tif"))
                                {
                                    contentType = "image/tiff";
                                }
                                else if (mediaItemToBeMoved.Name.EndsWith(".tiff"))
                                {
                                    contentType = "image/tiff";
                                }

                                if (!String.IsNullOrEmpty(contentType))
                                {
                                    var newMediaItem = _mediaItemManager.CreateMediaItem(mediaItemToBeMoved.Name, newOrderItem.NodeId, newOrderItem.OrderId, mediaItemToBeMoved.Data, contentType);
                                    newMediaItems.Add(newMediaItem);
                                }
                                else
                                {
                                    successfullyAddedAllMediaItems = false;
                                    failedMediaItemId = mediaItemToBeMoved.Id;
                                    failedMediaItemName = mediaItemToBeMoved.Name;
                                    break;
                                }
                            }

                            // Connect all the newly created media items.
                            if (successfullyAddedAllMediaItems)
                            {
                                foreach (var newMediaItem in newMediaItems)
                                {
                                    _orderItemManager.AddExistingMediaItemAsAnAttachmentWithoutLogging(newOrderItemNodeId, newMediaItem.Id, newMediaItem.Name, newMediaItem.Url, false, false);
                                }
                            }
                            else
                            {
                                throw new Exception("Failed to create media items connected to order item with ID = " + orderItemToBeMoved.NodeId +
                                    ". The troublesome media item has ID = " + failedMediaItemId + " and name '" + failedMediaItemName + "'.");
                            }
                        }
                        else
                        {
                            throw new Exception("Failed to read back order item with ID = " + newOrderItem.NodeId + " after creating it in the current storage.");
                        }
                    }
                    else
                    {
                        throw new Exception("There is already an order item with order ID = " + orderItemToBeMoved.OrderId + " in the current storage. Skipping move.");
                    }
                }
                else
                {
                    throw new Exception("There was no order item with node ID = " + nodeId + ". Nothing to move.");
                }

                json.Success = true;
                json.Message = "Successfully moved order with node ID = " + nodeId + ".";
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Something went horribly wrong: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using Chalmers.ILL.Models;
using Chalmers.ILL.Utilities;
using Chalmers.ILL.Extensions;
using Chalmers.ILL.OrderItems;
using umbraco.cms.businesslogic.member;
using umbraco.cms.businesslogic.relation;
using Umbraco.Core.Logging;
using Chalmers.ILL.Members;
using Newtonsoft.Json;
using System.Configuration;
using umbraco.cms.businesslogic.datatype;
using Chalmers.ILL.SignalR;
using Chalmers.ILL.UmbracoApi;
using Chalmers.ILL.Models.PartialPage;
using Umbraco.Core.Services;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{

    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemSurfaceController : SurfaceController
    {
        IMemberInfoManager _memberInfoManager;
        IOrderItemManager _orderItemManager;
        INotifier _notifier;
        IUmbracoWrapper _umbraco;
        IContentService _contentService;

        public OrderItemSurfaceController(IMemberInfoManager memberInfoManager, IOrderItemManager orderItemManager, 
            INotifier notifier, IUmbracoWrapper umbraco, IContentService contentService)
        {
            _memberInfoManager = memberInfoManager;
            _orderItemManager = orderItemManager;
            _notifier = notifier;
            _umbraco = umbraco;
            _contentService = contentService;
        }

        public const string lockRelationType = "memberLocked";

        /// <summary>
        /// Get a partial view for an OrderItem, used when opening an OrderItem for editing.
        /// Fetches the data for the order item and also fills in meta data for which member that has the lock on the order item.
        /// </summary>
        /// <param name="nodeId">The OrderItem Node Id</param>
        /// <returns>Partial View html data</returns>
        [HttpGet]
        public ActionResult RenderOrderItem(int nodeId)
        {
            // Get current member.
            int memberId = _memberInfoManager.GetCurrentMemberId(Request, Response);

            // Get a new OrderItem populated with values for this node
            var pageModel = new ChalmersILLOrderItemModel(_orderItemManager.GetOrderItem(nodeId));

            // Get all relations with the given node ID and the correct relations type.
            var relType = _umbraco.GetRelationTypeByAlias(lockRelationType);
            var relations = _umbraco.GetRelationsAsList(nodeId).Where(rel => rel.Child.Id == nodeId && rel.RelType.Id == relType.Id);
            var relationsCount = relations.Count();

            // Is node locked?
            if (relationsCount == 1)
            {
                // Is node locked by us?
                if (relations.First().Parent.Id == memberId)
                {
                    pageModel.OrderItem.EditedBy = memberId.ToString();
                    pageModel.OrderItem.EditedByMemberName = _memberInfoManager.GetCurrentMemberLoginName(Request, Response);
                    pageModel.OrderItem.UpdateDate = relations.First().CreateDate;
                    pageModel.OrderItem.EditedByCurrentMember = true;
                }
                else
                {
                    int userId = relations.First().Parent.Id;
                    pageModel.OrderItem.EditedBy = userId.ToString();
                    pageModel.OrderItem.UpdateDate = relations.First().CreateDate;
                    pageModel.OrderItem.EditedByMemberName = _umbraco.GetMember(userId).Text;
                    pageModel.OrderItem.EditedByCurrentMember = false;
                }
            }

            _umbraco.PopulateModelWithAvailableValues(pageModel);

            // Return Partial View to the client
            return PartialView("Chalmers.ILL.OrderItem", pageModel);
        }

        /// <summary>
        /// Get JSON about an OrderItem, useful for updating the list with latest data
        /// </summary>
        /// <param name="nodeId">The OrderItem Node Id</param>
        /// <returns>JSON result</returns>
        [HttpGet]
        public ActionResult GetOrderItem(int nodeId)
        {
            // Get current member.
            int memberId = _memberInfoManager.GetCurrentMemberId(Request, Response);

            // Get a new OrderItem populated with values for this node
            var orderItem = _orderItemManager.GetOrderItem(nodeId);

            // Get all relations with the given node ID and the correct relations type.
            var relType = _umbraco.GetRelationTypeByAlias(lockRelationType);
            var relations = _umbraco.GetRelationsAsList(nodeId).Where(rel => rel.Child.Id == nodeId && rel.RelType.Id == relType.Id);
            var relationsCount = relations.Count();

            // Is node locked?
            if (relationsCount == 1)
            {
                // Is node locked by us?
                if (relations.First().Parent.Id == memberId)
                {
                    orderItem.EditedBy = memberId.ToString();
                    orderItem.EditedByMemberName = _memberInfoManager.GetCurrentMemberLoginName(Request, Response);
                    //orderItem.EditedByMemberName = Member.GetCurrentMember().LoginName;
                    orderItem.EditedByCurrentMember = true;
                }
                else
                {
                    int userId = relations.First().Parent.Id;
                    orderItem.EditedBy = userId.ToString();
                    orderItem.EditedByMemberName = _umbraco.GetMember(userId).Text;
                }
            }

            // Return JSON object to the client to handle
            return Json(orderItem, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult GetOrderItemVersions()
        {
            // Response JSON
            var json = new OrderItemVersions();
            json.List = new List<OrderItemVersion>();

            // Get a new OrderItem populated with values for this node
            var orderListNode = _umbraco.TypedContentAtXPath("//" + ConfigurationManager.AppSettings["umbracoOrderListContentDocumentType"]).First();

            // Give counter for versions for each node found
            foreach (var node in _contentService.GetChildren(orderListNode.Id))
	        {
                var orderItemVersion = new OrderItemVersion();
                orderItemVersion.NodeId = node.Id;
                orderItemVersion.VersionCount = _contentService.GetVersions(node.Id).Count();
                json.List.Add(orderItemVersion);
	        }

            // Return JSON object to the client to handle
            return Json(json, JsonRequestBehavior.AllowGet);
        }


        /// <summary>
        /// Take over lock for OrderItem
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <returns>JSON result</returns>
        public ActionResult TakeOverLockedOrderItem(int nodeId)
        {
            // Response JSON
            var json = new ResultResponse();

            try
            {
                // Get current member
                int memberId = _memberInfoManager.GetCurrentMemberId(Request, Response);

                // Get all relations with the given node ID and the correct relations type.
                var relType = _umbraco.GetRelationTypeByAlias(lockRelationType);
                var relations = _umbraco.GetRelationsAsList(nodeId).Where(rel => rel.Child.Id == nodeId && rel.RelType.Id == relType.Id);
                var relationsCount = relations.Count();

                // Remove relations
                foreach (var rel in relations)
                {
                    rel.Delete();
                }

                // Lock node for current user
                _umbraco.MakeNewRelation(memberId, nodeId, relType, "Node with ID: " + nodeId + " has been locked by member with ID: " + memberId);

                // Return JSON to client
                json.Success = true;
                json.Message = "Took over lock.";

                // Notify SignalR clients of the update
                _notifier.UpdateOrderItemUpdate(nodeId, memberId.ToString(), new Member(memberId).Text);

            }
            catch (Exception e)
            {
                // Return JSON to client
                json.Success = false;
                json.Message = "Error taking lock: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Locks OrderItem to current MemberId
        /// </summary>
        /// <param name="nodeId">OrderItem node id</param>
        /// <returns>JSON result</returns>
        public ActionResult LockOrderItem(int nodeId)
        {
            // Response JSON
            var json = new ResultResponse();

            try
            {
                // Get current member.
                int memberId = _memberInfoManager.GetCurrentMemberId(Request, Response);

                // Get all relations with the given node ID and the correct relations type.
                var relType = _umbraco.GetRelationTypeByAlias(lockRelationType);
                var relations = _umbraco.GetRelationsAsList(nodeId).Where(rel => rel.Child.Id == nodeId && rel.RelType.Id == relType.Id);
                var relationsCount = relations.Count();

                if (relationsCount == 0)
                {
                    // Node is unlocked, free for us to take it.
                    _umbraco.MakeNewRelation(memberId, nodeId, relType, "Node with ID: " + nodeId + " has been locked by member with ID: " + memberId);
                    json.Success = true;
                    json.Message = "Order item locked by current member.";
                }
                else if (relationsCount == 1)
                {
                    // Node is already locked.

                    if (relations.First().Parent.Id == memberId)
                    {
                        // Node is already locked by us.
                        json.Success = true;
                        json.Message = "Order item locked by current member.";
                    }
                    else
                    {
                        // Node locked by someone else.
                        json.Success = false;
                        json.Message = "OrderItem is already locked by MemberId " + memberId;
                    }
                }
                else if (relationsCount > 1)
                {
                    // Should never happen.

                    _umbraco.LogWarn<OrderItemSurfaceController>("Found more than one lock for the same content.");
                    
                    foreach (var rel in relations)
                    {
                        _umbraco.LogWarn<OrderItemSurfaceController>("LOCK - Node ID: " + nodeId + ", Member ID: " + memberId);
                    }
                }

                // Notify SignalR clients of the update
                _notifier.UpdateOrderItemUpdate(nodeId, memberId.ToString(), new Member(memberId).Text);

            }
            catch (Exception e)
            {
                // Return JSON to client
                json.Success = false;
                json.Message = "Error locking OrderItem: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Unlocks OrderItem if locked by current MemberId
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public ActionResult UnlockOrderItem(int nodeId)
        {
            // Response JSON
            var json = new ResultResponse();

            try
            {
                // Get current member.
                int memberId = _memberInfoManager.GetCurrentMemberId(Request, Response);

                // Get all relations with the given node ID and the correct relations type.
                var relType = _umbraco.GetRelationTypeByAlias(lockRelationType);
                var relations = _umbraco.GetRelationsAsList(nodeId).Where(rel => rel.Child.Id == nodeId && rel.RelType.Id == relType.Id);
                var relationsCount = relations.Count();
                
                if (relationsCount == 0)
                {
                    // Nothing to unlock, wp!
                    json.Success = true;
                    json.Message = "OrderItem unlocked by current member.";
                }
                else if (relationsCount > 0)
                {
                    // There are locks, unlock them only if they are locked by current user.
                    var removeCount = 0;
                    foreach (var rel in relations.Where(x => x.Parent.Id == memberId))
                    {
                        rel.Delete();
                        removeCount++;
                    }

                    if (removeCount == relationsCount)
                    {
                        // All locks have been unlocked.
                        json.Success = true;
                        json.Message = "OrderItem unlocked by Current Member.";
                    }
                    else
                    {
                        // There were some locks that couldn't be unlocked.
                        json.Success = false;
                        json.Message = "OrderItem is not locked by logged in MemberId.";
                    }
                }

                // Notify SignalR clients of the update
                _notifier.UpdateOrderItemUpdate(nodeId, memberId.ToString(), new Member(memberId).Text);

            }
            catch (Exception e)
            {
                // Return JSON to client
                json.Success = false;
                json.Message = "Error unlocking OrderItem: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Get all locked OrderItems for Current Member
        /// </summary>
        /// <returns>JSON result</returns>
        public ActionResult GetLocksForCurrentMember()
        {
            // Response JSON
            var json = new LockResponse();

            try
            {
                // Get current member
                int memberId = _memberInfoManager.GetCurrentMemberId(Request, Response);

                // Get all lock relations.
                var relType = _umbraco.GetRelationTypeByAlias(lockRelationType);
                var relations = _umbraco.GetRelationsAsList(memberId).Where(rel => rel.Parent.Id == memberId && rel.RelType.Id == relType.Id);

                // Set response metadata.
                json.MemberId = memberId;
                json.MemberName = _memberInfoManager.GetCurrentMemberText(Request, Response);
                json.List = new List<Lock>();

                // Lambda statement found any locked OrderItem nodes.
                if (relations.Any())
                {
                    foreach (var rel in relations)
                    {
                        var lockItem = new Lock();
                        lockItem.NodeId = rel.Child.Id;
                        // Missing data for the Lock model, but this data is currently not used.
                        json.List.Add(lockItem);
                    }

                    json.Message = String.Format("Found {0} locked OrderItems for Current Member.", json.List.Count);
                }

                // Return JSON to client.
                json.Success = true;
            }
            catch (Exception e)
            {
                // Return JSON to client.
                json.Success = false;
                json.Message = "Error reading locked OrderItems: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }
    }
}
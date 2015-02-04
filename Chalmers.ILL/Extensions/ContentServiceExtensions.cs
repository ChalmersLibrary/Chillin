using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core.Services;
using Umbraco.Core.Models;
using Examine;
using Examine.LuceneEngine;
using UmbracoExamine;
using Umbraco.Core.Logging;
using System.Threading;

namespace Chalmers.ILL.Extensions
{
    public static class ContentServiceExtensions
    {
        /// <summary>
        /// Saves content without triggering events in Umbraco, then triggers redindexing of the 
        /// content in Lucene and waits for its completion. After the reindexing is done it signals
        /// the ChalmersILL clients using SignalR.
        /// </summary>
        /// <remarks>
        /// This method will wait until all indexing jobs in the indexer queue are finished before exiting.
        /// This should work fine with a few simultaneous users, but if there would be a lot of users
        /// constantly requesting indexing operations the system could appear extremely slow and 
        /// unresponsive.
        /// </remarks>
        /// <param name="cs">The ContentService which this method is called on as an extension.</param>
        /// <param name="content">The content which should be saved.</param>
        /// <param name="doReindex">Indicates if we should do a reindex operation of the saved node.</param>
        /// <param name="doSignal">Indicates if we should emit Signal R events or not.</param>
        public static void SaveWithoutEventsAndWithSynchronousReindexing(this IContentService cs, IContent content, bool doReindex=true, bool doSignal=true)
        {
            try
            {
                cs.Save(content, 0, false);

                if (doReindex)
                {
                    // Get the order items indexer from Lucene so that we can listen for the IndexOperationComplete event.
                    var orderItemsIndexer = ExamineManager.Instance.IndexProviderCollection["ChalmersILLOrderItemsIndexer"];
                    UmbracoContentIndexer umbracoOrderItemsIndexer = null;

                    // Do some downcasting so that we have access to the IndexOperationComplete event.
                    if (orderItemsIndexer is UmbracoContentIndexer)
                    {
                        umbracoOrderItemsIndexer = (UmbracoContentIndexer)orderItemsIndexer;
                    }

                    Semaphore semLock = null;
                    EventHandler handler = null;

                    try
                    {
                        if (umbracoOrderItemsIndexer != null)
                        {
                            // Create a semaphore which we use for the synchronization between Lucenes indexing thread and our thread.
                            semLock = new Semaphore(0, 1);

                            // The event handler which will be called when the reindexing is complete.
                            handler = (sender, e) => IndexOperationComplete(sender, e, semLock);

                            // Register the event handler to the IndexOperationComplete event.
                            umbracoOrderItemsIndexer.IndexOperationComplete += handler;
                        }

                        // Start the actual reindexing of the content.
                        ExamineManager.Instance.ReIndexNode(content.ToXml(), "content");

                        if (umbracoOrderItemsIndexer != null)
                        {
                            // Wait for the indexing operations to complete.
                            semLock.WaitOne();

                            // Cleanup
                            semLock.Dispose();
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        if (umbracoOrderItemsIndexer != null && handler != null)
                        {
                            // Unregister the handler.
                            umbracoOrderItemsIndexer.IndexOperationComplete -= handler;
                        }
                    }
                }

                if (doSignal)
                {
                    // Signal our ChalmersILL clients.
                    SignalR.Notifier.ReportNewOrderItemUpdate(content);
                }
            }
            catch (Exception e)
            {
                LogHelper.Error<ContentService>("SaveWithoutEventsAndWithSynchronousReindexing: Error when saving content.", e);
            }
        }

        static void IndexOperationComplete(object sender, EventArgs e, Semaphore semLock)
        {
            // Indicate to the waiting thread that the index operation has completed.
            semLock.Release();
        }
    }
}
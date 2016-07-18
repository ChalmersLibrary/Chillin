using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Chalmers.ILL.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Chalmers.ILL.Configuration;
using System.Text;

namespace Chalmers.ILL.MediaItems
{
    public class BlobStorageMediaItemManager : IMediaItemManager
    {
        private const string containerName = "chillinmedia";

        private IConfiguration _configuration;

        public BlobStorageMediaItemManager(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public MediaItemModel CreateMediaItem(string name, int orderItemNodeId, string orderId, Stream data, string contentType)
        {
            // Generate a UUID which we will use as identifier for the stored object.
            var id = Guid.NewGuid();

            // Parse the connection string and return a reference to the storage account.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                _configuration.StorageConnectionString);

            // Get a reference to the container that we use for storage.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExists();
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(id.ToString());
            
            // Store the object.
            blockBlob.UploadFromStream(data);

            // Store the metadata.
            var createDate = DateTime.Now;
            blockBlob.FetchAttributes();
            blockBlob.Metadata["name"] = name;
            blockBlob.Metadata["orderItemNodeId"] = orderItemNodeId.ToString();
            blockBlob.Metadata["createDate"] = createDate.ToString();
            blockBlob.SetMetadata();

            blockBlob.Properties.ContentType = contentType;
            blockBlob.SetProperties();

            // Create the stored object which we will return.
            var storedMediaItem = new MediaItemModel();
            PopulateStoredMediaItemFromCloudBlockBlob(storedMediaItem, blockBlob);

            return storedMediaItem;
        }

        public IList<MediaItemIdAndOrderItemId> DeleteOlderThan(DateTime date)
        {
            // Parse the connection string and return a reference to the storage account.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                _configuration.StorageConnectionString);

            // Get a reference to the container that we use for storage.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExists();

            var ret = new List<MediaItemIdAndOrderItemId>();
            foreach (var oldMediaItem in container.ListBlobs())
            {
                var oldBlob = (CloudBlockBlob)oldMediaItem;
                oldBlob.FetchAttributes();
                
                if (Convert.ToDateTime(oldBlob.Metadata["createDate"]) < date)
                {
                    ret.Add(new MediaItemIdAndOrderItemId(oldBlob.Name, Convert.ToInt32(oldBlob.Metadata["orderItemNodeId"])));
                    oldBlob.Delete();
                }
            }

            return ret;
        }

        public MediaItemModel GetOne(string id)
        {
            // Parse the connection string and return a reference to the storage account.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                _configuration.StorageConnectionString);

            // Get a reference to the container that we use for storage.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExists();
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(id.ToString());

            blockBlob.FetchAttributes();

            MemoryStream blobContents = new MemoryStream();
            blockBlob.DownloadToStream(blobContents);

            // Create the stored object which we will return.
            var storedMediaItem = new MediaItemModel();
            storedMediaItem.Data = blobContents;
            storedMediaItem.Data.Seek(0, SeekOrigin.Begin);
            PopulateStoredMediaItemFromCloudBlockBlob(storedMediaItem, blockBlob);

            return storedMediaItem;
        }

        #region Private methods

        private void PopulateStoredMediaItemFromCloudBlockBlob(MediaItemModel mediaItem, CloudBlockBlob blockBlob)
        {
            mediaItem.Id = blockBlob.Name;
            mediaItem.Name = blockBlob.Metadata["name"];
            mediaItem.OrderItemNodeId = Convert.ToInt32(blockBlob.Metadata["orderItemNodeId"]);
            mediaItem.Url = _configuration.BaseUrl + "umbraco/surface/MediaItemSurface/GetMediaItem/" + mediaItem.Id;
            mediaItem.CreateDate = Convert.ToDateTime(blockBlob.Metadata["createDate"]);
            mediaItem.ContentType = blockBlob.Properties.ContentType;
        }

        #endregion
    }
}
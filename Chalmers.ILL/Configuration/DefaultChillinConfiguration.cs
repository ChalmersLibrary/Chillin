using System;
using System.Configuration;

namespace Chalmers.ILL.Configuration
{
    public class DefaultChillinConfiguration : IConfiguration
    {
        public string BaseUrl
        {
            get
            {
                return ConfigurationManager.AppSettings["BaseUrl"];
            }
        }

        public string ElasticSearchUrl
        {
            get
            {
                return ConfigurationManager.AppSettings["ElasticSearchUrl"];
            }
        }

        public string ElasticSearchIndex
        {
            get
            {
                return ConfigurationManager.AppSettings["ElasticSearchIndex"];
            }
        }

        public string ElasticSearchTemplatesIndex
        {
            get
            {
                return ConfigurationManager.AppSettings["ElasticSearchTemplatesIndex"];
            }
        }

        public string StorageConnectionString
        {
            get
            {
                return ConfigurationManager.AppSettings["BlobStorageConnectionString"];
            }
        }

        public string LibPSearchUrl
        {
            get
            {
                return ConfigurationManager.AppSettings["LibPSearchUrl"];
            }
        }

        public string LibPSearchApiKey
        {
            get
            {
                return ConfigurationManager.AppSettings["LibPSearchApiKey"];
            }
        }
    }
}
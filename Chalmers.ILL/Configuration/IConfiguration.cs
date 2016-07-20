using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chalmers.ILL.Configuration
{
    public interface IConfiguration
    {
        string StorageConnectionString { get; }
        string ElasticSearchUrl { get; }
        string ElasticSearchIndex { get; }
        string BaseUrl { get; }
    }
}

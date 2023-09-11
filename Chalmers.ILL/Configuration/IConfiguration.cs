﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chalmers.ILL.Configuration
{
    public interface IConfiguration
    {
        bool UseMicrosoftGraphMailService { get; }
        string MicrosoftGraphApiUserId { get; }
        string MicrosoftGraphApiEndpoint { get; }
        string MicrosoftGraphAuthority { get; }
        string MicrosoftGraphClientId { get; }
        string MicrosoftGraphClientSecret { get; }
        string StorageConnectionString { get; }
        string ElasticSearchUrl { get; }
        string ElasticSearchIndex { get; }
        string ElasticSearchTemplatesIndex { get; }
        string BaseUrl { get; }
        string LibPSearchUrl { get; }
        string LibPSearchApiKey { get; }
    }
}

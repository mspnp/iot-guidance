// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.​
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.​
// ------------------------------------------------------------

using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.Documents.Spatial;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WarmPathUI.Config;
using WarmPathUI.Models;

namespace WarmPathUI.Services
{
    public class CosmosDBRepository : IDeviceEventsRepository
    {
        private string DatabaseId;
        private string CollectionId;
        private static DocumentClient client;
        private readonly ILogger logger;

        public CosmosDBRepository(IOptions<CosmosDBOptions> optionsAccessor, ILoggerFactory loggerFactory)
        {
            var options = optionsAccessor.Value;
            client = new DocumentClient(new Uri(options.EndpointUri), options.PrimaryKey);
            DatabaseId = options.DatabaseId;
            CollectionId = options.CollectionId;
            logger = loggerFactory.CreateLogger<CosmosDBRepository>();
        }

        public async Task<IEnumerable<DeviceEvent>> GetDeviceEventsAsync(Geometry geometry)
        {
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

            logger.LogInformation($"In GetDeviceEventsAsync. Geometry = {geometry}");

            IDocumentQuery<DeviceEvent> query = client.CreateDocumentQuery<DeviceEvent>(
                    UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), queryOptions)
                    .Where(a => a.Location.Within(geometry))
                    .AsDocumentQuery();

            var results = new List<DeviceEvent>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<DeviceEvent>());
            }
            return results;
        }
    }
}

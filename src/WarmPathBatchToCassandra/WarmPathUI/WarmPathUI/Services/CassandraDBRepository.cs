// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.​
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.​
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Spatial;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Dse;
using Dse.Auth;
using WarmPathUI.Config;
using WarmPathUI.Models;

namespace WarmPathUI.Services
{
    public class CassandraDBRepository : IDeviceEventsRepository
    {
        const int MaxDeviceEvents = 10000;

        private IDseSession session;
        private readonly ILogger logger;
        private readonly string tableName;

        public CassandraDBRepository(IOptions<CassandraDBOptions> optionsAccessor, ILoggerFactory loggerFactory)
        {
            var options = optionsAccessor.Value;
            IDseCluster cluster = DseCluster.Builder()
                .AddContactPoints(options.ContactPoints.Split(','))
                .WithAuthProvider(new DsePlainTextAuthProvider(options.Username, options.Password))
                .Build();
            session = cluster.Connect();

            tableName = options.Tablename;

            logger = loggerFactory.CreateLogger<CassandraDBRepository>();
        }

        public async Task<IEnumerable<DeviceEvent>> GetDeviceEventsAsync(double swLon, double swLat, double neLon, double neLat)
        {
            logger.LogInformation($"In GetDeviceEventsAsync. params = ({swLon},{swLat},{neLon},{neLat})");

            var results = new List<DeviceEvent>();

            var statementQuery = new SimpleStatement("select * from " + tableName + " where solr_query='{\"q\": \"*:*\", \"fq\":\"location:[\\\"" + swLon + " " + swLat + "\\\" TO \\\"" + neLon + " " + neLat + "\\\"]\"}' ORDER BY event_time DESC LIMIT " + MaxDeviceEvents);
            var rowSet = await session.ExecuteAsync(statementQuery);
            foreach (Row row in rowSet)
            {
                var location = (Dse.Geometry.Point)row["location"];
                var timestamp = (DateTimeOffset)row["event_time"];
                results.Add(new DeviceEvent
                                {
                                    id = row["device_id"].ToString(),
                                    Location = GeometryPoint.Create(location.X, location.Y),
                                    Timestamp = timestamp.DateTime
                                });
            }

            return results;
        }
    }
}

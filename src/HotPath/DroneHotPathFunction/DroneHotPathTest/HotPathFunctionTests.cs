// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Fabrikam.DroneManagement.DroneHotPathFunction;

namespace DroneHotPathTest
{
    [TestClass]
    public class HotPathFunctionTests
    {
        private HotTelemetry[] hotTelemetries;

        [TestMethod]
        public void CanDeserializeTelemetries()
        {
            

            hotTelemetries = JsonConvert.DeserializeObject<HotTelemetry[]>
                ("[{\"message\":\"hotTelemetry\",\"PartitionId\":1,\"deliveryId\":\"81046ea1-0329-4ba2-97cd-d5584e823b06\",\"deviceid\":\"Simulated.drone-01.3\",\"avgtemperature\":74.78,\"occurenceUtcTime\":\"2018-03-23T23:21:00.0000000Z\",\"EventEnqueuedUtcTime\":\"2018-03-23T23:21:00.0000000Z\"}," +
                  "{\"message\":\"hotTelemetry\",\"PartitionId\":1,\"deliveryId\":\"81046ea1-0329-4ba2-97cd-d5584e823b06\",\"deviceid\":\"Simulated.drone-01.3\",\"avgtemperature\":74.78,\"occurenceUtcTime\":\"2018-03-23T23:21:00.0000000Z\",\"EventEnqueuedUtcTime\":\"2018-03-23T23:21:00.0000000Z\"}]");
            Assert.AreEqual(2, hotTelemetries.Length);
            Assert.AreEqual("hotTelemetry", hotTelemetries[0].message);
            Assert.AreEqual(1, hotTelemetries[0].partition);
            Assert.AreEqual(74.78,hotTelemetries[0].AvgTemperature);
            Assert.AreEqual("81046ea1-0329-4ba2-97cd-d5584e823b06",hotTelemetries[0].Id);
            Assert.AreEqual("Simulated.drone-01.3",hotTelemetries[0].deviceid);
        }

        [TestMethod]
        public void CanDeserializeTelemetry()
        {

            hotTelemetries = JsonConvert.DeserializeObject<HotTelemetry[]>
                ("[{\"message\":\"hotTelemetry\",\"partitionid\":13,\"deliveryid\":\"59b8865f-32a5-4048-9a73-94f999ba4a8c\"," +
                "\"deviceid\":\"Simulated.drone-01.3\"," +
                "\"avgtemperature\":76.175," +
                "\"eventenqueuedutctime\":\"2018-04-04T23:33:00.0000000Z\"," +
                "\"occurrenceutctime\":\"2018-04-04T23:33:00.0000000Z\"}]");
            Assert.AreEqual(1, hotTelemetries.Length);
            Assert.AreEqual("hotTelemetry", hotTelemetries[0].message);
            Assert.AreEqual(13, hotTelemetries[0].partition);
            Assert.AreEqual(76.175, hotTelemetries[0].AvgTemperature);
            Assert.AreEqual("59b8865f-32a5-4048-9a73-94f999ba4a8c", hotTelemetries[0].Id);
            Assert.AreEqual("Simulated.drone-01.3", hotTelemetries[0].deviceid);

        }

        [TestMethod]
        public async Task CanReadStreamFromPayloadAsync()
        {

            string telemetryPayload = "[{\"partitionid\":1,\"device_id\":\"Simulated.drone-01.3\",\"avgtemperature\":74.78,\"time_sent\":\"2018-03-23T23:21:00.0000000Z\",\"eventprocessedutctime\":\"2018-03-23T23:21:00.0000000Z\"}," +
                  "{\"partitionid\":2,\"device_id\":\"Simulated.drone-01.9\",\"avgtemperature\":74.309999999999988,\"time_sent\":\"2018-03-23T23:21:00.0000000Z\",\"eventprocessedutctime\":\"2018-03-23T23:21:00.0000000Z\"}]";

            byte[] byteArray = Encoding.UTF8.GetBytes(telemetryPayload);       
            MemoryStream stream = new MemoryStream(byteArray);
            StreamReader streamReader = new StreamReader(stream);
            string telemetryString = await streamReader.ReadToEndAsync();

            Assert.AreEqual(telemetryPayload, telemetryString);

            stream.Dispose();
            streamReader.Dispose();
       
        }





    }
}

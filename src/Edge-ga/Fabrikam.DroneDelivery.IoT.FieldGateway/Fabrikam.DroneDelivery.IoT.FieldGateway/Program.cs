using System;
using System.Threading.Tasks;
using Fabrikam.DroneDelivery.IoT.DeviceSimulator;
using Microsoft.Azure.Devices;
using Fabrikam.DroneDelivery.IoT.ConsoleHost;
using System.Threading;

namespace Fabrikam.DroneDelivery.IoT.FieldGateway
{
    class Program
    {
        private const string IoTHubConnectionString = "<your-IoTHub-connection-string>";

        private const string EdgeDeviceConnectionString = "<your-edge-gateway-device-connection-string-with-gateway-host-appended>";

        private const int NumberOfLeafDevices = 5;

        private const int NumberOfEdgeDevices = 1;

        private const int DeviceQueryPageSize = 100;

        private const int GatewayMessagesCount = 60;

        private const string SensorPrefix = "drone-sensor-";

        private const string EdgePrefix = "edge-device-";

        static async Task Main(string[] args)
        {

            await ConsoleHost.ConsoleHost.WithOptions(new System.Collections.Generic.Dictionary<string, Func<System.Threading.CancellationToken, Task>>
            {
                {
                    "Add leaf devices to IoT Hub", CreateLeafDevicesAsync
                },
                {
                    "Add Edge devices to IoT Hub", CreateEdgeDevicesAsync
                },
                {
                    "Delete devices from IoT Hub", DeleteDevicesAsync
                },
                {
                    "Send telemetry to transparent edge gateway", SendTelemetryAsync
                }
            });
        }

        private static async Task SendTelemetryAsync(CancellationToken arg)
        {
            var deviceSimulator = new Simulation(EdgeDeviceConnectionString);
            await deviceSimulator.SendTelemetryAsync(GatewayMessagesCount);
        }

        private static async Task CreateLeafDevicesAsync(CancellationToken token)
        {
            var deviceGenerator = new DeviceGenerator(IoTHubConnectionString)
            {
                DeviceIdPrefix = SensorPrefix
            };

            var devices = await deviceGenerator.CreateDevicesAsync<Device>(NumberOfLeafDevices, edge: false);
        }

        private static async Task CreateEdgeDevicesAsync(CancellationToken token)
        {
            var deviceGenerator = new DeviceGenerator(IoTHubConnectionString)
            {
                DeviceIdPrefix = EdgePrefix
            };

            var devices = await deviceGenerator.CreateDevicesAsync<Device>(NumberOfEdgeDevices, edge: true);
        }

        private static async Task DeleteDevicesAsync(CancellationToken token)
        {
            await DeviceDestroyer.DeviceDestroyer.DeleteAllDevicesAsync(IoTHubConnectionString, DeviceQueryPageSize);
        }
    }
}

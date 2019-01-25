using Fabrikam.DroneDelivery.IoT.DeviceSimulator.data.model;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

namespace Fabrikam.DroneDelivery.IoT.DeviceSimulator
{
    public class Simulation
    {
        private readonly DeviceClient _deviceClient;

        private static readonly Random randomizer = new Random();

        private const double MinimumTemperature = 60;

        private const double AverageTemperature = 75;

        private const double TemperatureVariation = 15;

        private const double MaximumTemperature = 120;

        private const double MinimumBatteryLevel = 0.1;

        private const double MaximumBatteryLevel = 1.0;

        private const double BatteryVariation = 2;

        private const string DeviceId = "drone-sensor-0";

        private const string CertificatePath = "<path-to-root-ca-certificate-file>";

        // By setting below to 'true', we're assuming that certificate is installed in the local machine using a tool
        // such as 'certlm'. Otherwise you may install the certificate at application level by setting below to 'false'
        private static bool certificateInstalled = true;

        public Simulation(string connectionString)
        {
            this._deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt_Tcp_Only);
            if (!certificateInstalled)
            {
                X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                store.Add(new X509Certificate2(X509Certificate2.CreateFromCertFile(CertificatePath)));
                store.Close();
                certificateInstalled = true;
            }
        }
        public async Task SendTelemetryAsync(int numberOfMessages=1, int interval=1000)
        {
            string guid = Guid.NewGuid().ToString();
            double batteryLevel = MaximumBatteryLevel;
            for (int i = 0; i < numberOfMessages; i++)
            {
                double currentTemperature = VaryCondition(AverageTemperature, TemperatureVariation, MinimumTemperature, MaximumTemperature);
                batteryLevel = VaryCondition(batteryLevel, BatteryVariation, MinimumBatteryLevel, batteryLevel);

                var telemetry = new StateSensor()
                {
                    DeliveryId = guid,
                    DeviceId = DeviceId,
                    Temperature = Math.Round(currentTemperature,2),
                    BatteryLevel = Math.Round(batteryLevel,2)
                };

                var jsonMsg = JsonConvert.SerializeObject(telemetry);
                var msg = new Message(Encoding.ASCII.GetBytes(jsonMsg));

                msg.Properties.Add("temperatureAlert", currentTemperature > 75 ? "true" : "false");

                Console.WriteLine($"Sending message {i + 1}: {telemetry.ToString()}");

                await this._deviceClient.SendEventAsync(msg);
                await Task.Delay(interval);
            }
        }

        private static double VaryCondition(double avg, double percentage, double min, double max)
        {
            var someValue = avg * (1 + ((percentage / 100) * (2 * randomizer.NextDouble() - 1)));
            someValue = Math.Max(someValue, min);
            someValue = Math.Min(someValue, max);
            return someValue;
        }
    }
}

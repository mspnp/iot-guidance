using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.IoT.DeviceSimulator
{
    public class DeviceGenerator : IDeviceGenerator
    {
        public string DeviceIdPrefix { get; set; }
        public string DeviceIdSuffix { get; set; }

        private RegistryManager _registryManager;

        public DeviceGenerator(string connectionString)
        {
            if (this._registryManager == null)
            {
                this._registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            }
        }

        public async Task<ICollection<T>> CreateDevicesAsync<T>(int numberOfDevices = 10, bool edge = false) 
        {
            var totalRange = Enumerable.Range(0, numberOfDevices).ToArray();
            var devices = new List<Device>();

            Parallel.ForEach<int>(totalRange, (currentNum) =>
            {
                var device = new Device($"{DeviceIdPrefix}{currentNum}")
                {
                    Capabilities = new Microsoft.Azure.Devices.Shared.DeviceCapabilities()
                    {
                        IotEdge = edge
                    },

                    Authentication = new AuthenticationMechanism()
                    {
                        Type = AuthenticationType.CertificateAuthority
                    }
                };

                devices.Add(device);
            });

            var result = await this._registryManager.AddDevices2Async(devices);


            return result.IsSuccessful ? (ICollection<T>)devices : (ICollection<T>)result.Errors.ToList();
        }
    }
}

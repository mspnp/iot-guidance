using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.IoT.DeviceSimulator
{
    interface IDeviceGenerator
    {
        string DeviceIdPrefix { get; set; }
        string DeviceIdSuffix { get; set; }

        Task<ICollection<T>> CreateDevicesAsync<T>(int numberOfDevices, bool edge = false);
    }
}

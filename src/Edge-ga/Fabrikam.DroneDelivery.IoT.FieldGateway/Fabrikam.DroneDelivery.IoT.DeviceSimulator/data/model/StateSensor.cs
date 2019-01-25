using System;
using System.Collections.Generic;
using System.Text;

namespace Fabrikam.DroneDelivery.IoT.DeviceSimulator.data.model
{
    class StateSensor : ISensor
    {
        public string DeviceId { get; set; }

        public string DeliveryId { get; set; }

        public double Temperature { get; set; }

        public double BatteryLevel { get; set; }

        public override string ToString()
        {
            return $"DeviceId: {DeviceId}, DeliveryId: {DeliveryId}, Temperature: {Temperature}, BatteryLevel: {BatteryLevel}";
        }
    }
}

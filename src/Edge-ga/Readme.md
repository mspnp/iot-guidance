## Azure IoT Edge - Transparent Gateway
Azure IoT Edge can be used to satisfy all needs for an IoT gateway such as connectivity, identity, edge analytics et al. Here we're setting up an IoT Edge device as a transparent gateway. Leaf devices are unaware that they are communicating with the cloud via a gateway, thus it's called transparent gateway. Refer to [How an IoT Edge device can be used as a gateway](https://docs.microsoft.com/en-us/azure/iot-edge/iot-edge-as-gateway) to learn more about various types of gateways.

Our scenario involves running Edge gateway on a Linux box. Leaf device is running on a Windows 10 box. Here are the steps:

1. Install [Azure IoT Edge runtime](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-install-iot-edge-linux) on a Linux x64 box.
2. Generate non-production certificates that enable communication between modules/Edge runtime and leaf device/Edge runtime as described in [this article](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-create-transparent-gateway-linux).
3. Clone the repository <insert_link_to_repository_here>

    a. Open, build and deploy the Edge module (_BatchingModule_) under _Fabrikam.DroneDelivery.IoT.EdgeModule_ folder using VS Code.
    
    b. Open the solution under _Fabrikam.DroneDelivery.IoT.FieldGateway_ in VS 2017 and replace the following variables in ```Program.Main``` with appropriate values from your Azure IoT Hub susbcription:

```java
        private const string IoTHubConnectionString = "<your-IoTHub-connection-string>";

        private const string EdgeDeviceConnectionString = "<your-edge-gateway-device-connection-string-with-gateway-host-appended>";
```

4. Run the solution in Visual Studio or command line and choose option 4 that simulates sending messages to gateway Edge device.



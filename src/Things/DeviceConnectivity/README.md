# Introduction

this Quick Start provides with guidance for Device Connectivity Security as part of a more comprehensive IoT Solution. 

This solution is using X.509 Certificates to create cryptographic chain of trust(s) for attestation when provisioning to Azure IoT Hub Device Provisioning Service and authentication to Azure IoT Hub.

It also goes over the supply chain when the contract manufacturing Fabrikam IoT Drone Delivery company with a hierarchy of factories commissions to Fabrikam Drone Factory to do the manufacturing. The latter also delegates the IoT Device installation process to technicians.

Most of the roles covered in this Quick Start could be independent systems that lives in separated processes but for the sake of simplicity it is shipped as just one simple Console app.

# Prerequisites

- Azure suscription
- [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Setup Azure IoT Hub Device Provisioning resource](https://docs.microsoft.com/en-us/azure/iot-dps/quick-setup-auto-provision-cli)
- [Link an the Azure IoT Hub instance to Azure IoT Hub DPS](https://docs.microsoft.com/en-us/azure/iot-dps/quick-setup-auto-provision-cli#link-the-iot-hub-and-the-provisioning-service)

# Installation

1. git clone <github_repo_here>

2. Navigate to the project folder 
    
```bash
    cd <github_repo_here>/src/Things/DeviceConnectivity/Fabrikam.IoT.Devices.SupplyChain
```

3. Set the following environment variables or just jump to step 4, you could provide them later on

```bash
export DPS_PROVISIONING_CONNECTION_STRING=<YOUR_AZURE_DPS_PRIMARY_CONNECTION_STRING>
export DPS_GLOBALENDPOINT=<YOUR_AZURE_DPS_GLOBALENDPOINT>
export DPS_ID_SCOPE=<YOUR_AZURE_DPS_ID_SCOPE>
export DPS_ENROLLMENTGROUP_ID=<YOUR_AZURE_DPS_ENROLLMENTGROUP_ID>
```

4. Run the Console app 

```bash
dotnet restore && \
dotnet run
``` 

# Cross-Platform cryptography

> Important
>   By the time writting this, X509 Self Signed CA certs cannot be stored in macOS the X509 Store CurrentUser\Root. 
>   It throws a CrytographicException Access Denied. For more information please take at [Cryptography](##Cryptography)             

# Resources
the following resources have been taken into account in different areas of the Quick Start:

## Device Connectivity Store and IoT Roles: 
   - [Conceptual understanding of X.509 CA certificates in the IoT industry](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-x509ca-concept) 
   - [Internet of Things security best practices](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-security-best-practices#secure-an-iot-infrastructure)

## Azure IoT Hub Device Provisioning Service 
   - [Provisioning devices with Azure IoT Hub Device Provisioning Service](https://docs.microsoft.com/en-us/azure/iot-dps/about-iot-dps)
   - [Individual and Enrollment groups with X509 Certificates attestation](https://github.com/MicrosoftDocs/azure-docs/blob/master/articles/iot-dps/concepts-security.md#controlling-device-access-to-the-provisioning-service-with-x509-certificates)
   - [How to do proof-of-possession for X.509 CA certificates with your Device Provisioning Service](https://github.com/MicrosoftDocs/azure-docs/blob/master/articles/iot-dps/how-to-verify-certificates.md)

    [TBD Individual vs Enrollment Group table]

## Cryptography
   - [Cross-Platoform Cryptoghaphy](https://github.com/dotnet/corefx/blob/master/Documentation/architecture/cross-platform-cryptography.md)

## Code samples used as a reference in this Quickstart
   - [Azure IoT SDK C#: Provisioning Service Enrollment Grouop Sample](https://github.com/Azure/azure-iot-sdk-csharp/tree/master/provisioning/service/samples/ProvisioningServiceEnrollmentGroup)
   - [Azure IoT SDK C#: Provisioning Device Client Sample - X.509 Attestation](https://github.com/Azure/azure-iot-sdk-csharp/tree/master/provisioning/device/samples/ProvisioningDeviceClientX509)
   - [Azure IoT SDK C: CA Certificate Overview](https://github.com/Azure/azure-iot-sdk-c/blob/master/tools/CACertificates/CACertificateOverview.md)
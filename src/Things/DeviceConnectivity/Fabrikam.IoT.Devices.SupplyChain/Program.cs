// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fabrikam.IoT.Devices.SupplyChain.Actors;
using Fabrikam.IoT.Devices.SupplyChain.Runtime;
using Fabrikam.IoT.Devices.SupplyChain.Security.X509Certificates;
using Fabrikam.IoT.Devices.SupplyChain.Services;

namespace Fabrikam.IoT.Devices.SupplyChain
{
    /// <summary>
    /// Fabrikam IoT Drone Device Connectivity security and supply chain Quick Start.
    /// </summary>
    /// <remarks>
    /// This Quickstart provides with guidance for Device Connectivity Security as part of a more comprehensive IoT Solution. 
    /// This solution is using X.509 Certificates to create cryptographic chain of trust(s) for attestation when provisioning to 
    /// Azure IoT Hub Device Provisioning Service and authentication to Azure IoT Hub.
    /// Additionally, it goes over the supply chain when the contract manufacturing Fabrikam IoT Drone Delivery company with a hierarchy 
    /// of factories commissions to Fabrikam Drone Factory to do the manufacturing. The latter also delegates the IoT Device installation process 
    /// to technicians.
    ///
    /// Please find below some IoT Industry roles that might help to go over this Quickstart:
    /// - RootCertificateAuthority (not an IoT role per se): expose the Root CA API responsible for issuing its self signed crt(s) for itself 
    /// - CertificateAuthority (not an IoT role per se): expose the CA API responsible for issuing certificates for an intermediate entity
    /// - IoT Company: act as a Root CA in the chain of trust and is reponsible for issuing intermediate certificates (e.g. Fabrikam Drone IoT Company).
    /// - IoT Hardware Integrator: act as Certificate Authority in the chain of trust and is responsible for issuing intermediate certificates 
    ///                            for another intermediate entity or end entities if required (e.g. Fabrikam Drone Factory). 
    /// - IoT Deployer:  act as Certificate Authority in the chain of trust and is responsible for issuing end entity leaf certificates (e.g. Fabrikam's IoT Device Technician).
    ///
    /// This Quickstart can be represented in the follow diagram
    ///
    /// <code>
    /// +----------------------------+
    /// | Fabrikam Drone IoT Company |
    /// |                            |
    /// |             {}{}{}{}{}{}{} |
    /// |             {} X509 CA  {} |
    /// |             {} Root Cert{} |
    /// |             {} with Key {} |
    /// |             {}{}{}{}{}{}{} |
    /// +-------------+--------------+
    ///               |        +-----------------------------+
    ///               +------> +   Fabrikam Drone Factory    |
    ///                        |                             |
    ///                        |              {}{}{}{}{}{}{} |
    ///                        |              {} X509 Int {} |
    ///                        |              {} CA Cert 1{} |
    ///                        |              {} with Key {} |
    ///                        |              {}{}{}{}{}{}{} |
    ///                        +--------------+--------------+
    ///                                       |          +-----------------------------+
    ///                                       |          |    Fabrikam's IoT Device    |
    ///                                       +--------->+    Technician               |
    ///                                                  |                             |
    ///                                                  |              {}{}{}{}{}{}{} |
    ///                                                  |              {} X509 Int {} |
    ///                                                  |              {} CA Cert 2{} |
    ///                                                  |              {} with Key {} |
    ///                                                  |              {}{}{}{}{}{}{} |
    ///                                                  +--------------+--------------+
    ///                                                                /
    ///                                                              /
    ///                                                             v
    ///                    +----------------------------------------+------------------+
    ///                    |                 Fabrikam's IoT Drone Device               |
    ///                    |                                                           |
    ///                    |{}{}{}{}{}{}{}                                             |
    ///                    |{} X509 CA  {} {}{}{}{}{}{}{}                              |
    ///                    |{} Root Cert{} {} X509 Int {} {}{}{}{}{}{}{}               |
    ///                    |{}          {} {} CA Cert 1{} {} X509 Int {} {}{}{}{}{}{}{}|
    ///                    |{}{}{}{}{}{}{} {}          {} {} CA Cert 2{} {} X509 Leaf{}|
    ///                    |               {}{}{}{}{}{}{} {}          {} {} Cert     {}|
    ///                    |                              {}{}{}{}{}{}{} {} with Key {}|
    ///                    |                                             {}{}{}{}{}{}{}|
    ///                    +-----------------------------------------------------------+
    /// </code>
    /// </remarks>
    /// <see cref="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-x509ca-concept">Conceptual understanding of X.509 CA certificates in the IoT industry</see>
    /// <see cref="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-security-best-practices#secure-an-iot-infrastructure">Internet of Things security best practices</see>
    class Program
    {
        #region consts
        private const string endVarProvisioningConnectionString = "DPS_PROVISIONING_CONNECTION_STRING";
        private const string envVarGlobalEndpoint = "DPS_GLOBALENDPOINT";
        private const string envVarIdScope = "DPS_ID_SCOPE";
        private const string envVarEnrollmentGroupId = "DPS_ENROLLMENTGROUP_ID";
        private const string enterCompany = "Enter an IoT company name [Fabrikam Drone IoT Company if empty]";
        private const string enterHardwareIntegrator = "Enter a Manufacturing factory name [Fabrikam Drone Factory if empty]";
        private const string enterDeployer = "Enter a Factory's technician name [Fabrikam IoT Device Technician if empty]";
        private const string enterDPSConnStr = "Enter an Azure IoT DPS Primary Connection String";
        private const string enterDPSGlobalEndpoint = "Enter an Azure IoT DPS Global Endpoint";
        private const string enterDPSScopeId = "Enter an Azure IoT DPS Scope Id";
        private const string enterDPSEnrollmentGroup = "Enter an Azure IoT DPS EnrollmentGroup";
        private const string X509FriendlyName = "Fabrikam Drone IoT Quickstart Test Only";
        #endregion

        private static Config RuntimeConfig;
        private static IProvisioningService ProvisioningService;

        public static void Main(string[] args)
        {
            Console.WriteLine("Fabrikam IoT Supply Chain Quickstart");

            Bootstrap();

            try
            {
                string opt = String.Empty;
                while (true)
                {

                    // Ensure cleanup X509 Cert Store from prev run
                    CleanupStore();

                    Console.WriteLine("\nSelect one of the following options:");
                    Console.WriteLine("   1. Run Device Connectivity Supply Chain sample");
                    Console.WriteLine("   0. Exit");

                    opt = "Enter a valid option to continue"
                            .ReadTextFromTerminal(acceptEmptyValue: false);

                    if (opt == "0")
                    {
                        break;
                    }

                    switch (opt)
                    {
                        case "1":
                            {
                                RunDeviceConnectivitySupplyChain().GetAwaiter().GetResult();
                                break;
                            }
                        default:
                            {
                                Console.WriteLine("\nInvalid Option");
                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                var color = Console.BackgroundColor;
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nError:{ex.Message}\n");
                Console.BackgroundColor = color;
            }
            finally
            {
                // X509 Certs clean up before closing the app
                CleanupStore();
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static async Task RunDeviceConnectivitySupplyChain()
        {
            using (var iotCompany = new IoTCompany(
                                            RuntimeConfig, 
                                            ProvisioningService))
            {
                // ******** Azure IoT Hub DPS Service ******** 
                // 1. Create a Enrollment Group if not exists
                Console.Write($"\nCreating a new Enrollment Group: {RuntimeConfig.AzureDPSEnrollmentGroup}...");

                await iotCompany.CleanUpAndCreateEnrollmentGroupAsync().ConfigureAwait(false);

                Console.Write($"OK!\n");

                // 2. Go over the PoP
                await iotCompany.KickoffPoPAsync().ConfigureAwait(false);

                // ******** Azure IoT Hub DPS Service ******** 
                // 3. Supply Chain with Device Provisioning
                Console.Write("\nMaking and provisioning a new IoT Device to IoT Hub...");

                var brandNewIoTDevice = await iotCompany.MakeDeviceAsync().ConfigureAwait(false);

                Console.Write($"OK!\n");

                // 4. Start sending telemetry
                Console.Write($"\nAuthenticating {brandNewIoTDevice.DeviceID} and send telemetry sample message...");

                await brandNewIoTDevice.AuthNAndSendTelemetryAsync().ConfigureAwait(false);

                Console.Write($"OK!\n");

                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }

        private static void Bootstrap()
        {
            RuntimeConfig = new Config
            {
                IoTCompanyName =
                    enterCompany.ReadTextFromTerminal(),

                IoTHardwareIntegratorName =
                    enterHardwareIntegrator.ReadTextFromTerminal(),

                IoTDeployerName =
                    enterDeployer.ReadTextFromTerminal(),

                AzureDPSConnectionString =
                    Environment.GetEnvironmentVariable(
                                        endVarProvisioningConnectionString) ??
                    enterDPSConnStr.ReadTextFromTerminal(
                                                    acceptEmptyValue: false),

                AzureDPSGlobalEndpoint =
                    Environment.GetEnvironmentVariable(envVarGlobalEndpoint) ??
                    enterDPSGlobalEndpoint.ReadTextFromTerminal(
                                                     acceptEmptyValue: false),

                AzureDPSScopeId =
                    Environment.GetEnvironmentVariable(envVarIdScope) ??
                    enterDPSScopeId.ReadTextFromTerminal(
                                                    acceptEmptyValue: false),

                AzureDPSEnrollmentGroup =
                    Environment.GetEnvironmentVariable(envVarEnrollmentGroupId) ??
                    enterDPSEnrollmentGroup.ReadTextFromTerminal(
                                                    acceptEmptyValue: false)
            };

            if (string.IsNullOrWhiteSpace(RuntimeConfig.IoTCompanyName))
            {
                RuntimeConfig.IoTCompanyName = "Fabrikam Drone IoT Company";
            }

            if (string.IsNullOrWhiteSpace(RuntimeConfig.IoTHardwareIntegratorName))
            {
                RuntimeConfig.IoTHardwareIntegratorName = "Fabrikam Drone Factory";
            }

            if (string.IsNullOrWhiteSpace(RuntimeConfig.IoTDeployerName))
            {
                RuntimeConfig.IoTDeployerName = "Fabrikam IoT Device Technician";
            }

            ProvisioningService = new ProvisioningService(RuntimeConfig);
        }

        private static void CleanupStore()
        {
            Console.Write("Clean up X509 Cert Personal Store...");
            X509CertificateOperations.RemoveCertsByOrganizationName(
                StoreName.My,
                StoreLocation.CurrentUser,
                "Fabrikam Drone Delivery");
            Console.Write("OK!\n");

            Console.Write("Clean up X509 Cert CA Store...");
            X509CertificateOperations.RemoveCertsByOrganizationName(
                StoreName.CertificateAuthority,
                StoreLocation.CurrentUser,
                "Fabrikam Drone Delivery");
            Console.Write("OK!\n");

            Console.Write("Clean up X509 Cert Root Store...");
            X509CertificateOperations.RemoveCertsByOrganizationName(
                                        StoreName.Root,
                                        StoreLocation.CurrentUser,
                                        "Fabrikam Drone Delivery");
            Console.Write("OK!\n");
        }
    }
}

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Text;
using System.Threading.Tasks;
using Fabrikam.IoT.Devices.SupplyChain.Actors;
using Fabrikam.IoT.Devices.SupplyChain.Actors.Security;
using Fabrikam.IoT.Devices.SupplyChain.Security.X509Certificates;

namespace Fabrikam.IoT.Devices.SupplyChain
{

    public static class ProgramExtensions
    {
        public async static Task KickoffPoPAsync(this IIoTCompany iotCompany)
        {
            Console.WriteLine("\nStarting new Proof of Possession...");
            var caCerFileName = "FabrikamDDCAPoP.cer";
            await ((iotCompany as IRootCertificateAuthority)?
                    .personalSignedX509Certificate
                    .ExportToCerAsync(caCerFileName))
                   .ConfigureAwait(false);

            Console.WriteLine($"\nNavigate to the Azure portal, Add a new certicate to your Azure DPS Certificates using {caCerFileName} file and press any key to continue...");
            Console.ReadKey();
            Console.WriteLine($"\nOnce the certificate is uploaded, click on the new \"Unverified\" entry to open the Proof of Procession flyout, click Generate Verification Code and press any key to continue...");
            Console.ReadKey();

            string vericationCode = "Enter Verification Code for PoP"
                                        .ReadTextFromTerminal(
                                                    acceptEmptyValue: false);

            var dnVerificationCode = $"CN={vericationCode}";
            var verificationCertFileName = $"{vericationCode}.cer";

            await iotCompany
                    .GenerateProofOfVerficationAsync(
                        dnVerificationCode,
                        verificationCertFileName)
                    .ConfigureAwait(false);
            Console.WriteLine($"\nGo back to the Azure Portal PoP flyout and upload {verificationCertFileName} file, once your certificate is verified press key to continue...");
            Console.ReadKey();
        }

        public static string ReadTextFromTerminal(this string what,
                                                  bool acceptEmptyValue = true)
        {
            Console.WriteLine($"\n{what}: ");

            var verifCode = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (verifCode.Length > 0)
                    {
                        verifCode.Remove(verifCode.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    if (!acceptEmptyValue &&
                            string.IsNullOrWhiteSpace(verifCode.ToString()))
                    {
                        Console.WriteLine($"\n{what} is required please enter a valid value");
                        Console.WriteLine($"\nEnter the {what}: ");
                    }
                    else
                    {
                        Console.WriteLine();
                        break;
                    }
                }
                else
                {
                    Console.Write(key.KeyChar);
                    verifCode.Append(key.KeyChar);
                }
            }

            return verifCode.ToString();
        }
    }
}

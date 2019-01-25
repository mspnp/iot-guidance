// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Threading.Tasks;

namespace Fabrikam.DroneManagement.DroneHotPathFunction
{
    public interface INotificationService
    {
        Task SendNotificationAsync(HotTelemetry hotTelemetry);
    }
}

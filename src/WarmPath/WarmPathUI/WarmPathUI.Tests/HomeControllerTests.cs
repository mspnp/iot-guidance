// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.​
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.​
// ------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WarmPathUI.Controllers;
using WarmPathUI.Models;
using WarmPathUI.Services;
using Xunit;

namespace WarmPathUI.Test
{
    public class HomeControllerTests
    {
        [Fact]
        public async Task GetDevices_ReturnsDevices()
        {
            var devices = new List<DeviceEvent>()
            {
                new DeviceEvent { id = "device1" },
                new DeviceEvent { id = "device2" }
            };

            var mockRepo = new Mock<IDeviceEventsRepository>();
            mockRepo.Setup(repo => repo.GetDeviceEventsAsync(null))
                .ReturnsAsync(devices);

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(factory => factory.CreateLogger(It.IsAny<string>()))
                .Returns(NullLogger.Instance);

            var controller = new HomeController(mockRepo.Object, mockLoggerFactory.Object);

            var result = await controller.GetDevices(0, 0, 10, 10);

            Assert.IsType<OkObjectResult>(result);
        }
    }
}

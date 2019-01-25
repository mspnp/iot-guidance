// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.​
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.​
// ------------------------------------------------------------

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WarmPathUI.Models;
using WarmPathUI.Services;

namespace WarmPathUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDeviceEventsRepository repository;
        private readonly ILogger logger;

        public HomeController(IDeviceEventsRepository repository, ILoggerFactory loggerFactory)
        {
            this.repository = repository;
            this.logger = loggerFactory.CreateLogger<HomeController>();
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("api/devices")]
        public async Task<IActionResult> GetDevices(double swLon, double swLat, double neLon, double neLat)
        {
            logger.LogInformation($"In GetDevices action. params = ({swLon},{swLat},{neLon},{neLat})");

            var results = await repository.GetDeviceEventsAsync(swLon, swLat, neLon, neLat);
            return Ok(results);
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

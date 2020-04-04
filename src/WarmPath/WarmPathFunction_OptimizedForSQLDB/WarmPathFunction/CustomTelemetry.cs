// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;

namespace WarmPathFunction
{
    public static class CustomTelemetry
    {
        private static string key = TelemetryConfiguration.Active.InstrumentationKey =
        Environment.GetEnvironmentVariable(
                            "APPINSIGHTS_INSTRUMENTATIONKEY",
                            EnvironmentVariableTarget.Process);

        private static TelemetryClient telemetryClient =
            new TelemetryClient() { InstrumentationKey = key };

        // This correllates all telemetry with the current Function invocation
        private static void UpdateTelemetryContext(TelemetryContext context, ExecutionContext functionContext)
        {
            context.Operation.Id = functionContext.InvocationId.ToString();
            context.Operation.ParentId = functionContext.InvocationId.ToString();
            context.Operation.Name = functionContext.FunctionName;
        }

        public static void TrackMetric(ExecutionContext context, string metricName, double metricValue)
        {
            var metric2 = new MetricTelemetry(metricName, metricValue);
            UpdateTelemetryContext(metric2.Context, context);
            telemetryClient.TrackMetric(metric2);
        }
    }
}

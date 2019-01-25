// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace WarmPathFunction
{
    public static class DroneTelemetryConverter
    {
        public static (double Latitude, double Longitude) ConvertPosition(string positionField)
        {
            var positionRegEx = new Regex("([-+]?\\d*\\.?\\d+)\\|([-+]?\\d*\\.?\\d+)\\|([-+]?\\d*\\.?\\d+)");
            var match = positionRegEx.Match(positionField);

            var style = NumberStyles.Number;
            var culture = CultureInfo.CreateSpecificCulture("en-US");
            double latitude = 0, longitude = 0;
            if (match.Success && match.Groups.Count == 4)
            {
                Double.TryParse(match.Groups[1].Value, style, culture.NumberFormat, out latitude);
                Double.TryParse(match.Groups[2].Value, style, culture.NumberFormat, out longitude);
            }
            else
            {
                throw new ArgumentException("Invalid position field", "positionField");
            }

            if (latitude > 90 || latitude < -90 || longitude > 180 || longitude < -180)
            {
                throw new ArgumentException("Invalid lat/long value", "positionField");
            }

            return (latitude, longitude);
        }
    }
}

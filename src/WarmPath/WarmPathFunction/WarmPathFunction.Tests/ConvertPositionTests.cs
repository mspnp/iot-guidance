// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WarmPathFunction.Tests
{
    [TestClass]
    public class ConvertPositionTests
    {
        [TestMethod]
        public void ConvertPosition_Converts()
        {
            var inputString = "+47.757084|-121.989734|93.98";
            var result = DroneTelemetryConverter.ConvertPosition(inputString);

            Assert.AreEqual(47.757084, result.Latitude);
            Assert.AreEqual(-121.989734, result.Longitude);
        }

        [TestMethod]
        public void ConvertPosition_Throws_WithInvalidLongitude()
        {
            var inputString = "+95|-121.989734|93.98";

            Assert.ThrowsException<ArgumentException>(() => DroneTelemetryConverter.ConvertPosition(inputString));
        }

        [TestMethod]
        public void ConvertPosition_Throws_WithInvalidLatitude()
        {
            var inputString = "+47.757084|-190|93.98";

            Assert.ThrowsException<ArgumentException>(() => DroneTelemetryConverter.ConvertPosition(inputString));
        }


        [TestMethod]
        public void ConvertPosition_Throws_WithMissingValue()
        {
            var inputString = "47.757084||-121.989734";

            Assert.ThrowsException<ArgumentException>(() => DroneTelemetryConverter.ConvertPosition(inputString));
        }


        [TestMethod]
        public void ConvertPosition_Throws_WithNonNumericValues()
        {
            var inputString = "a|b|c";

            Assert.ThrowsException<ArgumentException>(() => DroneTelemetryConverter.ConvertPosition(inputString));
        }
    }
}

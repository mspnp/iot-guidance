// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.​
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.​
// ------------------------------------------------------------

namespace WarmPathUI.Config
{
    public class CassandraDBOptions
    {
        public string ContactPoints { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Tablename { get; set; }
    }
}

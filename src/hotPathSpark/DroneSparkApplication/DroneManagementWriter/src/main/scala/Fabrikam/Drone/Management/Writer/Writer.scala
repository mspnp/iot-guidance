// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


package Fabrikam.Drone.Management.Writer

import java.util.concurrent._
import org.apache.http.HttpResponse

trait Writer {
  def WriteTelemetry(url: String, data: String, funckey: String)

}

trait WriterTelemetry {
  def WriteTelemetry(url: String,customerId:String, logName:String,
                     xdate: Option[String], wKey:String,
                     data: String): String

}



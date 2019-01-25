
// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

package Fabrikam.Drone.Management.Writer
import scala.concurrent.Future
import scala.concurrent.duration._
import play.api.libs.ws.ahc._
import play.api.libs.ws.DefaultBodyReadables._
import play.api.libs.ws.DefaultBodyWritables._
import scala.util.{Try,Success,Failure}

import akka.actor.ActorSystem
import akka.stream.ActorMaterializer
import scala.concurrent.ExecutionContext.Implicits._

class FunctionWriter extends Writer {

  implicit val system = ActorSystem()
  implicit val materializer = ActorMaterializer()
  val ws = StandaloneAhcWSClient()

  def WriteTelemetry(url: String, data: String,
                     funckey: String) {

    var statusText: String = "noOk"
    val timeOut = 1 seconds

    ws.url(url)
      .addHttpHeaders(("x-functions-key",
        funckey))
      .withRequestTimeout(timeOut)
      .post(data)
      .map { response ⇒
        statusText = response.statusText
      }
      .andThen {
        case Failure(e) => {
          ws.close()
          throw new Exception("failed with messsage ".concat(statusText))
        }
        case _ => {
          ws.close()
        }
      }
  }




}

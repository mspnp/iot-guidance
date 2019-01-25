// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


package Fabrikam.Drone.Management.Writer
import java.nio.charset.StandardCharsets
import java.util._

import javax.crypto._
import javax.crypto.spec._
import org.slf4j.Logger
import org.slf4j.LoggerFactory
import org.apache.commons.codec.binary.Base64
import org.apache.http.HttpHeaders
import org.apache.http.client.methods.CloseableHttpResponse
import org.apache.http.client.methods.HttpPost
import org.apache.http.entity.StringEntity
import org.apache.http.impl.client.CloseableHttpClient
import org.apache.http.impl.client.HttpClients
import java.util.Locale
import java.time.{OffsetDateTime, ZoneOffset}

import javax.crypto.Mac
import javax.crypto.spec.SecretKeySpec
import java.io.UnsupportedEncodingException
import java.security.GeneralSecurityException

import org.apache.http.impl.client.CloseableHttpClient
import org.apache.http.impl.client.HttpClients
import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Date
import java.util.Locale
import java.util.TimeZone
import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Date
import java.util.Locale
import java.util.TimeZone

import org.apache.http.client.methods.HttpPost
import org.apache.http.entity.StringEntity
import java.security.{InvalidKeyException, NoSuchAlgorithmException}
import java.io.IOException

import org.apache.http._
import org.apache.http.impl.nio.client.{CloseableHttpAsyncClient, HttpAsyncClients}
import org.apache.http.HttpHeaders

class LogAnalyticsWriter extends WriterTelemetry {


  val xmsDate: Date = Calendar.getInstance.getTime

  val dateFormat = new SimpleDateFormat("EEE, dd MMM yyyy HH:mm:ss z", Locale.US)
  dateFormat.setTimeZone(TimeZone.getTimeZone("GMT"))
  val xmsDateString: String = dateFormat.format(xmsDate)




  private val ContentType = "application/json"

  private object LogAnalyticsHttpHeaders {
    val LOG_TYPE = "Log-Type"
    val X_MS_DATE = "x-ms-date"
    val TIME_GENERATED_FIELD = "time-generated-field"
  }

  @throws(classOf[HttpException])
  @throws(classOf[IOException])
  def WriteTelemetry(url: String,workspaceId:String, logType:String,
                     daterfc1123:Option[String], primaryKey:String,
                     data: String): String = {
  //  val timeOut = 1 seconds



   val xmsDate = Calendar.getInstance.getTime

   val dateFormat = new SimpleDateFormat("EEE, dd MMM yyyy HH:mm:ss z", Locale.US)
   dateFormat.setTimeZone(TimeZone.getTimeZone("GMT"))
   val xmsDateString = dateFormat.format(xmsDate)


    var statusText: String = "NoResponse"

    val contentType = "application/json"
    val resource = "/api/logs"

    var signature = BuildSignature(
      primaryKey,
      xmsDateString,
      data.length(),
      "POST",
      contentType,
      resource
    )

    signature = String.format("SharedKey %s:%s", workspaceId, signature)

    val httpClient:CloseableHttpAsyncClient = HttpAsyncClients.createDefault()
    httpClient.start()

     val httpPost = new HttpPost(url + workspaceId + ".ods.opinsights.azure.com/api/logs?api-version=2016-04-01")

    httpPost.setEntity(new StringEntity(data))


    httpPost.setHeader(HttpHeaders.CONTENT_TYPE, ContentType)
    httpPost.setHeader(LogAnalyticsHttpHeaders.LOG_TYPE, logType)
    httpPost.setHeader(LogAnalyticsHttpHeaders.X_MS_DATE, xmsDateString)
    httpPost.setHeader(HttpHeaders.AUTHORIZATION, signature)
    httpPost.setHeader(LogAnalyticsHttpHeaders.TIME_GENERATED_FIELD, xmsDateString)

    statusText = httpClient.execute(httpPost,null).get()
      .getStatusLine().toString

    httpClient.close()

    statusText
  }


  
  @throws(classOf[NoSuchAlgorithmException])
  @throws(classOf[InvalidKeyException])
  private def BuildSignature(primaryKey: String, xmsDate: String, contentLength: Int, method: String,
                              contentType: String, resource: String): String = {
    var result:String = ""
    val xHeaders = String.format("%s:%s", LogAnalyticsHttpHeaders.X_MS_DATE, xmsDate)
    //xHeaders = "Fri, 20 Jul 2018 16:28:59 GMT";


    val stringToHash = String.format("%s\n%s\n%s\n%s\n%s",
      method, contentLength.toString(), contentType, xHeaders, resource)




    val decodedBytes = Base64.decodeBase64(primaryKey)
    try {
      val hasher = Mac.getInstance("HmacSHA256")
      hasher.init(new SecretKeySpec(decodedBytes, "HmacSHA256"))
      var hash = new Array[Byte](0)
      try
        hash = hasher.doFinal(stringToHash.getBytes("UTF-8"))
      catch {
        case uee: UnsupportedEncodingException =>
        //   LOGGER.error("Unsupported character encoding for stringToHash", uee)
      }
      result = Base64.encodeBase64String(hash)
    } catch {
      case gse: GeneralSecurityException =>
      // LOGGER.error("Error initializing Mac algorithm", gse)
    }
    result
  }






}

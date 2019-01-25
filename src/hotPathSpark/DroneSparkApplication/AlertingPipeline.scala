
// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

// Databricks notebook source
package Fabrikam.DroneDelivery.Management.Monitoring
import org.apache.log4j._
import org.apache.spark.sql.streaming.StreamingQueryListener
import org.apache.spark.sql.streaming.StreamingQueryListener._
import org.apache.spark.sql.functions.get_json_object
import org.json4s._
import org.json4s.native._
import org.json4s.native.JsonMethods._
import Fabrikam.Drone.Management.Writer._
import com.google.gson.Gson

case class Telemetry(triggerExecution: Double,
                       getBatch:Double,
                       inputRowsPerSecond: Double, 
                        procRowsPerSecond: Double)

class TelemetryCollector(logType:String,workspaceId:String,primaryKey:String) extends StreamingQueryListener{
  
  val logger = Logger.getLogger(getClass.getName)
  implicit val formats = DefaultFormats
  
   
  override def onQueryStarted(event: QueryStartedEvent): Unit = {
  }

  override def onQueryProgress(event: QueryProgressEvent): Unit = {
    
    try{
      //parsing the telemetry Payload      
      var dataJson = parse(event.progress.json).asInstanceOf[JObject]
      var dataValue = dataJson \ "durationMs" \ "triggerExecution"
      val triggerExecution: Double = dataValue.extract[Double]
    
      dataValue = dataJson \ "durationMs" \ "getBatch"
      val getBatch: Double = dataValue.extract[Double]
    
      dataValue = dataJson \ "inputRowsPerSecond"
      val inRowsPerSecond: Double = dataValue.extract[Double]
    
      dataValue = dataJson \ "processedRowsPerSecond"
      val procRowsPerSecond: Double = dataValue.extract[Double]
    
      val telemetry:Telemetry = new Telemetry(triggerExecution,getBatch,inRowsPerSecond,procRowsPerSecond)
      val gson = new Gson()
     
      val telemetryWriter = new LogAnalyticsWriter()
      
      val result=telemetryWriter.WriteTelemetry("https://",workspaceId, logType,
                     None, primaryKey,gson.toJson(telemetry)) 
      
      logger.info(gson.toJson(telemetry))
      logger.info(result)
           
    }
  
    catch {         
        case e: Exception => logger.error(e.printStackTrace)
    }
                 
  }

  override def onQueryTerminated(event: QueryTerminatedEvent): Unit = {
  }
  
}


// COMMAND ----------

package Fabrikam.DroneDelivery.Management.Alerting
import org.apache.spark._

import org.apache.spark.streaming._
import org.apache.spark.eventhubs.ConnectionStringBuilder
import org.apache.spark.eventhubs.{ EventHubsConf, EventPosition, EventHubsUtils }
import org.apache.spark.sql._
import org.apache.spark.sql.functions.get_json_object

import Fabrikam.Drone.Management.Writer.FunctionWriter
import org.apache.spark.sql.types._
import org.apache.spark.sql.streaming._
import com.google.gson.Gson;
import org.apache.spark.sql._

case class HotTelemetryPOJO(message: String,
                            partitionid: Int,
                            deviceid: String, 
                            deliveryid: String,
                            avgtemperature: Double, 
                            lastenqueuedutctime: String,
                            lastoccurrenceutctime: String,
                            lastprocessedutctime: String)


class AlertTelemetrySink(functionEndpoint: String,functionKey:String) extends ForeachWriter[Row] {
  
  var gson:Gson = _
  var functionWriter:FunctionWriter = _
  
  val FUNC_END_POINT = functionEndpoint
  val FUNC_KEY = functionKey
           
      def open(partitionId: Long,version: Long): Boolean = {
        
        functionWriter = new FunctionWriter()
        gson = new Gson()
        true
      }
      def process(value: Row): Unit = {
        var hotTelemetries = new Array[HotTelemetryPOJO](1)
        val hotTelemetry = new HotTelemetryPOJO(value(0).asInstanceOf[String],
                                              value(1).asInstanceOf[Int],
                                              value(2).asInstanceOf[String],
                                              value(3).asInstanceOf[String],
                                              value(4).asInstanceOf[Double],
                                              value(5).asInstanceOf[String],
                                              value(6).asInstanceOf[String],
                                              value(7).asInstanceOf[String])
        hotTelemetries(0) = hotTelemetry
        val hotTelemetryJson = gson.toJson(hotTelemetries)
        
        functionWriter.WriteTelemetry(FUNC_END_POINT,hotTelemetryJson,FUNC_KEY)
       
      }
      def close(errorOrNull: Throwable): Unit = {        
      }
   }

// COMMAND ----------

import org.apache.spark.eventhubs._
import org.apache.spark.sql.functions._
import org.apache.spark.sql.streaming.Trigger.ProcessingTime
import org.apache.spark.sql.types._


import com.microsoft.azure.cosmosdb.spark.schema._
import com.microsoft.azure.cosmosdb.spark._
import com.microsoft.azure.cosmosdb.spark.config.Config
import org.codehaus.jackson.map.ObjectMapper
import com.microsoft.azure.cosmosdb.spark.streaming._

import Fabrikam.DroneDelivery.Management.Alerting._
import Fabrikam.DroneDelivery.Management.Monitoring._
//subscribe to the LogAnalytics StreamingQueryListener implementation
// it gets triggered at the end of the trigger execution
spark.streams.addListener(new TelemetryCollector("alertpipeline",
                                               dbutils.secrets.get("hotpathscope", "workspaceId"),
                                               dbutils.secrets.get("hotpathscope", "omskey")))

val alertConString = ConnectionStringBuilder(dbutils.secrets.get("hotpathscope", "alertingconnstring"))
  .setEventHubName("alertbus")
  .build

val eventHubsConfAlert =
  EventHubsConf(alertConString)
  .setConsumerGroup("alerting")
  .setMaxEventsPerTrigger(30000)
  .setStartingPosition(EventPosition.fromEndOfStream)

val functionEndpoint = dbutils.secrets.get("hotpathscope", "functionendpoint")
val functionKey = dbutils.secrets.get("hotpathscope", "functionkey")

val incomingStream = spark.readStream.format("eventhubs")
                          .options(eventHubsConfAlert.toMap).load()

val bodyStream = 
  incomingStream
 .select( 
          get_json_object(($"body").cast("string"), "$.lastoccurrenceutctime").cast(TimestampType)
          .as("lastoccurrenceutctime"),
          get_json_object(($"body").cast("string"), "$.lastenqueuedutctime").cast(TimestampType)
          .as("lastenqueuedutctime"),
	      get_json_object(($"body").cast("string"), "$.lastprocessedutctime").cast(TimestampType)
          .as("lastprocessedutctime"), 
          $"enqueuedTime".as("enqueuedUtcTime"),
         to_utc_timestamp(current_timestamp(),"MM/dd/yyyy HH:mm:ss").alias("processedutctime"),
         get_json_object(($"body").cast("string"), "$.message").alias("message"),
         get_json_object(($"body").cast("string"), "$.partitionid").cast(IntegerType).alias("partitionid"),
         get_json_object(($"body").cast("string"), "$.deviceid").alias("deviceid"),
         get_json_object(($"body").cast("string"), "$.deliveryid").alias("deliveryid"),
         get_json_object(($"body").cast("string"), "$.avgtemperature").cast(DoubleType).alias("avgtemperature")        
        )


val alertTempStream = bodyStream.withWatermark("lastoccurrenceutctime", "10 seconds")
                              .groupBy(window(col("lastoccurrenceutctime"), "150 seconds", "150 seconds"),
                                              col("deviceid"),col("deliveryid"),col("message"))
                              .agg(first("partitionid").as("partitionid"),
                                   first("avgtemperature").as("avgtemperature"),
                                   first("lastenqueuedutctime").as("lastenqueuedutctime"),
                                   first("lastoccurrenceutctime").as("lastoccurrenceutctime"),
                                   first("processedutctime").as("lastprocessedutctime"))


val alertTelemetrySink = new AlertTelemetrySink(functionEndpoint,functionKey)
val alertwrite =
  alertTempStream
    .select($"message",
            $"partitionid",
            $"deviceid",
            $"deliveryid",
            $"avgtemperature",
            $"lastenqueuedutctime".cast("string"),
            $"lastoccurrenceutctime".cast("string"),
            $"lastprocessedutctime".cast("string"))
    .writeStream
    .foreach(alertTelemetrySink)
    .outputMode("append")
    .option("checkpointLocation", "/alert")
    .start()

// COMMAND ----------

dbutils.fs.mkdirs("/alert/")

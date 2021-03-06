
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

package Fabrikam.DroneDelivery.Management.CosmosWriter

case class HotTelemetryPOJO(message: String,
                            partitionid: Int,
                            deviceid: String, 
                            deliveryid: String,
                            avgtemperature: Double, 
                            lastenqueuedutctime: String,
                            lastoccurrenceutctime: String,
                            lastprocessedutctime: String)

import org.apache.spark.sql.types._
import org.apache.spark.sql.streaming._
import com.google.gson.Gson;
import com.microsoft.azure.documentdb.ConnectionPolicy;
import com.microsoft.azure.documentdb.ConsistencyLevel;
import com.microsoft.azure.documentdb.Database;
import com.microsoft.azure.documentdb.Document;
import com.microsoft.azure.documentdb.DocumentClient;
import com.microsoft.azure.documentdb.DocumentClientException;
import com.microsoft.azure.documentdb.DocumentCollection;
import com.microsoft.azure.documentdb.RequestOptions;

import org.apache.spark.sql._
                      

class HotTelemetrySink(endPoint: String,masterKey: String,databaseId: String, collectionId:String) extends ForeachWriter[Row] {
     var documentClient:DocumentClient = _
     var gson:Gson = _
    
     val END_POINT = endPoint
     val MASTER_KEY = masterKey
     var DATABASE_ID = databaseId
     var COLLECTION_ID = collectionId
         
      def open(partitionId: Long,version: Long): Boolean = {
        documentClient = new DocumentClient(END_POINT,
                MASTER_KEY, ConnectionPolicy.GetDefault(),
                ConsistencyLevel.Session);
        gson = new Gson()
        true
      }
      def process(value: Row): Unit = {
      val hotTelemetry = new HotTelemetryPOJO(value(0).asInstanceOf[String], 
                                    value(1).asInstanceOf[Int],          
                                    value(2).asInstanceOf[String],
                                    value(3).asInstanceOf[String],
                                    value(4).asInstanceOf[Double],
                                    value(5).asInstanceOf[String],
                                    value(6).asInstanceOf[String],
                                    value(7).asInstanceOf[String])
                                          
        val hotTelemetryJson = gson.toJson(hotTelemetry)
        val hotTelemetryDocument = new Document(hotTelemetryJson);
               
        documentClient.createDocument("dbs/" + DATABASE_ID + "/colls/" + COLLECTION_ID, hotTelemetryDocument, null, false)
      }
      def close(errorOrNull: Throwable): Unit = {        
      }
   }

// COMMAND ----------

//absence telemetry to cosmos db

import org.apache.spark.eventhubs._
import org.apache.spark.sql.functions._
import org.apache.spark.sql.streaming.Trigger.ProcessingTime
import org.apache.spark.sql.types._

import com.microsoft.azure.cosmosdb.spark.schema._
import com.microsoft.azure.cosmosdb.spark._
import com.microsoft.azure.cosmosdb.spark.config.Config
import org.codehaus.jackson.map.ObjectMapper
import com.microsoft.azure.cosmosdb.spark.streaming._

import Fabrikam.DroneDelivery.Management.CosmosWriter._
import Fabrikam.DroneDelivery.Management.Monitoring._

//subscribe to the LogAnalytics StreamingQueryListener implementation
// it gets triggered at the end of the trigger execution
spark.streams.addListener(new TelemetryCollector("notelpipeline",
                                               dbutils.secrets.get("hotpathscope", "workspaceId"),
                                               dbutils.secrets.get("hotpathscope", "omskey")))

    // Build connection string with the above information
val EhConnString = ConnectionStringBuilder(dbutils.secrets.get("hotpathscope", "iothubconnstring"))
.setEventHubName("replacewitheventhubcompatiblename")
      .build


val LEventthubParameters =
      EventHubsConf(EhConnString)
      .setConsumerGroup("hotpath4")
      .setMaxEventsPerTrigger(50000)
      .setStartingPosition(EventPosition.fromEndOfStream)

val REventhubParameters =
      EventHubsConf(EhConnString)
      .setConsumerGroup("hotpath5")
      .setMaxEventsPerTrigger(50000)
      .setStartingPosition(EventPosition.fromEndOfStream)

val endPoint = dbutils.secrets.get("hotpathscope", "cosmosdbendpoint");
val masterKey = dbutils.secrets.get("hotpathscope", "cosmosdbmasterkey");
val databaseId = "hottelemetrydb";
val collectionId = "absencetelemetrycol"


val lDeviceStream = spark.readStream.format("eventhubs")
                          .options(LEventthubParameters.toMap).load()

val rDeviceStream = spark.readStream.format("eventhubs")
                          .options(REventhubParameters.toMap).load()

val lDeviceData = 
  lDeviceStream
 .select(get_json_object(($"body").cast("string"), "$.occurrenceUtcTime")
         .cast(TimestampType).as("loccurrenceUtcTime"),
         $"enqueuedTime".as("lenqueuedUtcTime"),
         to_utc_timestamp(current_timestamp(),"MM/dd/yyyy HH:mm:ss").alias("lprocesstime"),
         get_json_object(($"body").cast("string"), "$.deviceId").alias("ldeviceid"),
         get_json_object(($"body").cast("string"), "$.sensorType").alias("sensorType"),
         get_json_object(($"body").cast("string"), "$.deliveryId").alias("ldeliveryid"),
         get_json_object(($"body").cast("string"), "$.temperature").cast(DoubleType).alias("ltemperature")
        )
        .where("sensorType = 'drone-state-sensor;v1'").select("*")

val rDeviceData = 
  rDeviceStream
 .select(get_json_object(($"body").cast("string"), "$.occurrenceUtcTime")
         .cast(TimestampType).as("roccurrenceUtcTime"),
         $"enqueuedTime".as("renqueuedUtcTime"),
         to_utc_timestamp(current_timestamp(),"MM/dd/yyyy HH:mm:ss").alias("rprocesstime"),
         get_json_object(($"body").cast("string"), "$.deviceId").alias("rdeviceid"),
         get_json_object(($"body").cast("string"), "$.sensorType").alias("sensorType"),
         get_json_object(($"body").cast("string"), "$.deliveryId").alias("rdeliveryid"),
         get_json_object(($"body").cast("string"), "$.temperature").cast(DoubleType).alias("rtemperature")
        )
        .where("sensorType = 'drone-state-sensor;v1'").select("*")


val lDevice = lDeviceData 
                 .withWatermark("loccurrenceUtcTime", "10 seconds")

val rDevice = rDeviceData
                 .withWatermark("roccurrenceUtcTime", "20 seconds")


val noTelemetry = lDevice.join(
  rDevice,
  expr("""
    rdeviceid = ldeviceid AND
    roccurrenceUtcTime >= loccurrenceUtcTime AND
    roccurrenceUtcTime <= loccurrenceUtcTime + interval 5 minutes
    """),
  joinType = "leftOuter"      // can be "inner", "leftOuter", "rightOuter"
 )
         
   



val hotTelemetrySink = new HotTelemetrySink(endPoint,masterKey,databaseId,collectionId)

val cosmoswrite =
  noTelemetry
    .withColumn("message",lit("noTelemetry"))
    .withColumn("partitionid",lit(0))
    .where($"rdeviceid".isNull)
    .select($"message",
            $"partitionid",
            $"ldeviceid".as("deviceid"),
            $"ldeliveryid".as("deliveryid"),
            $"ltemperature".as("avgtemperature"),
            $"lenqueuedUtcTime".cast("string").as("lastenqueuedutctime"),
            $"loccurrenceUtcTime".cast("string").as("lastoccurrenceutctime"),
            $"lprocesstime".cast("string").as("lastprocessedutctime"))
    .writeStream
    .foreach(hotTelemetrySink)
    .outputMode("append")
    .option("checkpointLocation", "/noteltocosmos")
    .start()




// COMMAND ----------

//absence telemetry to alerting

import org.apache.spark.eventhubs._
import org.apache.spark.sql.functions._
import org.apache.spark.sql.streaming.Trigger.ProcessingTime
import org.apache.spark.sql.types._

import com.microsoft.azure.cosmosdb.spark.schema._
import com.microsoft.azure.cosmosdb.spark._
import com.microsoft.azure.cosmosdb.spark.config.Config
import org.codehaus.jackson.map.ObjectMapper
import com.microsoft.azure.cosmosdb.spark.streaming._
import Fabrikam.DroneDelivery.Management.Monitoring._

//subscribe to the LogAnalytics StreamingQueryListener implementation
// it gets triggered at the end of the trigger execution
spark.streams.addListener(new TelemetryCollector("notelalerting",
                                               dbutils.secrets.get("hotpathscope", "workspaceId"),
                                               dbutils.secrets.get("hotpathscope", "omskey")))

    // Build connection string with the above information
val EhConnString = ConnectionStringBuilder(dbutils.secrets.get("hotpathscope", "iothubconnstring"))
.setEventHubName("replacewitheventhubcompatiblename")
      .build

val alertConString = ConnectionStringBuilder(dbutils.secrets.get("hotpathscope", "alertingconnstring"))
  .setEventHubName("alertbus")
  .build

val eventHubsConfAlert =
  EventHubsConf(alertConString)
  .setConsumerGroup("alerting3")
  .setStartingPosition(EventPosition.fromEndOfStream)

val LEventthubParameters =
      EventHubsConf(EhConnString)
      .setConsumerGroup("hotpath6")
      .setMaxEventsPerTrigger(50000)
      .setStartingPosition(EventPosition.fromEndOfStream)

val REventhubParameters =
      EventHubsConf(EhConnString)
      .setConsumerGroup("hotpath7")
      .setMaxEventsPerTrigger(50000)
      .setStartingPosition(EventPosition.fromEndOfStream)


val lDeviceStream = spark.readStream.format("eventhubs")
                          .options(LEventthubParameters.toMap).load()

val rDeviceStream = spark.readStream.format("eventhubs")
                          .options(REventhubParameters.toMap).load()


   

val lDeviceData = 
  lDeviceStream
 .select(get_json_object(($"body").cast("string"), "$.occurrenceUtcTime")
         .cast(TimestampType).as("loccurrenceUtcTime"),
         $"enqueuedTime".as("lenqueuedUtcTime"),
         to_utc_timestamp(current_timestamp(),"MM/dd/yyyy HH:mm:ss").alias("lprocesstime"),
         get_json_object(($"body").cast("string"), "$.deviceId").alias("ldeviceid"),
         get_json_object(($"body").cast("string"), "$.sensorType").alias("sensorType"),
         get_json_object(($"body").cast("string"), "$.deliveryId").alias("ldeliveryid"),
         get_json_object(($"body").cast("string"), "$.temperature").cast(DoubleType).alias("ltemperature")
        )
        .where("sensorType = 'drone-state-sensor;v1'").select("*")

val rDeviceData = 
  rDeviceStream
 .select(get_json_object(($"body").cast("string"), "$.occurrenceUtcTime")
         .cast(TimestampType).as("roccurrenceUtcTime"),
         $"enqueuedTime".as("renqueuedUtcTime"),
         to_utc_timestamp(current_timestamp(),"MM/dd/yyyy HH:mm:ss").alias("rprocesstime"),
         get_json_object(($"body").cast("string"), "$.deviceId").alias("rdeviceid"),
         get_json_object(($"body").cast("string"), "$.sensorType").alias("sensorType"),
         get_json_object(($"body").cast("string"), "$.deliveryId").alias("rdeliveryid"),
         get_json_object(($"body").cast("string"), "$.temperature").cast(DoubleType).alias("rtemperature")
        )
        .where("sensorType = 'drone-state-sensor;v1'").select("*")


val lDevice = lDeviceData 
                 .withWatermark("loccurrenceUtcTime", "10 seconds")

val rDevice = rDeviceData
                 .withWatermark("roccurrenceUtcTime", "20 seconds")


val noTelemetry = lDevice.join(
  rDevice,
  expr("""
    rdeviceid = ldeviceid AND
    roccurrenceUtcTime >= loccurrenceUtcTime AND
    roccurrenceUtcTime <= loccurrenceUtcTime + interval 5 minutes
    """),
  joinType = "leftOuter"      // can be "inner", "leftOuter", "rightOuter"
 )
         
   
//display(noTelemetry.where($"rdeviceid".isNull))

val eventsDf =  noTelemetry
                .withColumn("message",lit("noTelemetry"))
                .withColumn("partitionid",lit(0))
                .where($"rdeviceid".isNull)
                .select($"message",
                        $"partitionid",
                        $"ldeviceid".as("deviceid"),
                        $"ldeliveryid".as("deliveryid"),
                        $"ltemperature".as("avgtemperature"),
                        $"lenqueuedUtcTime".cast("string")
                        .as("lastenqueuedutctime"),
                        $"loccurrenceUtcTime".cast("string")
                        .as("lastoccurrenceutctime"),
                        $"lprocesstime".cast("string")
                        .as("lastprocessedutctime"))

val ehwrite =
  eventsDf
  .select(to_json(struct($"*"))).toDF("body")
  .writeStream
  .format("eventhubs")
  .options(eventHubsConfAlert.toMap)
  .outputMode("append")
  .option("checkpointLocation", "/noteltoalert")
  .start()



// COMMAND ----------

dbutils.fs.mkdirs("/noteltocosmos/")
dbutils.fs.mkdirs("/noteltoalert/")


// Databricks notebook source
// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


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
                    

case class HotTelemetryPOJO(message: String,
                            partitionid: Int,
                            deviceid: String, 
                            deliveryid: String,
                            avgtemperature: Double, 
                            lastenqueuedutctime: String,
                            lastoccurrenceutctime: String,
                            lastprocessedutctime: String)


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

//hot Telemetry to Cosmos DB
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
spark.streams.addListener(new TelemetryCollector("hottemppipeline",
                                              dbutils.secrets.get("hotpathscope", "workspaceId"),
                                              dbutils.secrets.get("hotpathscope", "omskey")))

    // Build connection string with the above information
val connectionString = ConnectionStringBuilder(dbutils.secrets.get("hotpathscope", "iothubconnstring"))
.setEventHubName("replacewitheventhubcompatiblename")
      .build

 val endPoint = dbutils.secrets.get("hotpathscope", "cosmosdbendpoint");
 val masterKey = dbutils.secrets.get("hotpathscope", "cosmosdbmasterkey");
 val databaseId = "hottelemetrydb";
 val collectionId = "hottelemetrycol"


val customEventhubParameters =
      EventHubsConf(connectionString)
      .setMaxEventsPerTrigger(120000) 
      .setConsumerGroup("hotpath2")
      .setStartingPosition(EventPosition.fromEndOfStream)


val incomingStream = spark.readStream.format("eventhubs")
                          .options(customEventhubParameters.toMap).load()

val bodyStream = 
  incomingStream
 .select(unix_timestamp(get_json_object(($"body").cast("string"), "$.occurrenceUtcTime"), "MM/dd/yyyy HH:mm:ss")
         .cast(TimestampType).as("occurrenceUtcTime"),
         $"enqueuedTime".as("enqueuedUtcTime"),
         to_utc_timestamp(current_timestamp(),"MM/dd/yyyy HH:mm:ss").alias("processedUtcTime"),
         get_json_object(($"body").cast("string"), "$.deviceId").alias("deviceId"),
         get_json_object(($"body").cast("string"), "$.sensorType").alias("sensorType"),
         get_json_object(($"body").cast("string"), "$.deliveryId").alias("deliveryId"),
         get_json_object(($"body").cast("string"), "$.temperature").cast(DoubleType).alias("temperature")
        )
        .where("sensorType = 'drone-state-sensor;v1'").select("*")

val hotTempStream = bodyStream.withWatermark("occurrenceUtcTime", "5 seconds")
                              .groupBy(window(col("occurrenceUtcTime"), "90 seconds", "90 seconds"),
                                              col("deviceId"),col("deliveryId"))
                              .agg(avg("temperature").as("avgtemperature"),
                                   last("enqueuedUtcTime").as("lastenqueuedutctime"),
                                   last("occurrenceUtcTime").as("lastoccurrenceutctime"),
                                   last("processedUtcTime").as("lastprocessedutctime"))


val eventsDf = hotTempStream.withColumn("message",lit("hotTemperature"))
                       .withColumn("partitionid",lit(0))
                       .where("avgtemperature >=75")
                       .select($"message",
                                $"partitionid",
                                $"deviceId".as("deviceid"),
                                $"deliveryId".as("deliveryid"),
                                $"avgtemperature",
                                $"lastenqueuedutctime",
                                $"lastoccurrenceutctime",
                                $"lastprocessedutctime")



val hotTelemetrySink = new HotTelemetrySink(endPoint,masterKey,databaseId,collectionId)
val cosmoswrite =
  eventsDf
    .select($"message",
            $"partitionid",
            $"deviceId".as("deviceid"),
            $"deliveryId".as("deliveryid"),
            $"avgtemperature",
            $"lastenqueuedutctime".cast("string"),
            $"lastoccurrenceutctime".cast("string"),
            $"lastprocessedutctime".cast("string"))
    .writeStream
    .foreach(hotTelemetrySink)
    .outputMode("append")
    .option("checkpointLocation", "/hottocosmos1")
    .start()




// COMMAND ----------

// hot telemetry to alerting
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
spark.streams.addListener(new TelemetryCollector("hotalertpipeline",
                                              dbutils.secrets.get("hotpathscope", "workspaceId"),
                                              dbutils.secrets.get("hotpathscope", "omskey")))


    // Build connection string with the above information
val connectionString = ConnectionStringBuilder(dbutils.secrets.get("hotpathscope", "iothubconnstring"))
.setEventHubName("replacewitheventhubcompatiblename")
      .build

val alertConString = ConnectionStringBuilder(dbutils.secrets.get("hotpathscope", "alertingconnstring"))
  .setEventHubName("alertbus")
  .build

val customEventhubParameters =
      EventHubsConf(connectionString)
      .setMaxEventsPerTrigger(120000) 
      .setConsumerGroup("hotpath3")
      .setStartingPosition(EventPosition.fromEndOfStream)

val eventHubsConfAlert =
  EventHubsConf(alertConString)
  .setConsumerGroup("alerting2")
  .setStartingPosition(EventPosition.fromEndOfStream)


val incomingStream = spark.readStream.format("eventhubs")
                          .options(customEventhubParameters.toMap).load()

val bodyStream = 
  incomingStream
 .select(unix_timestamp(get_json_object(($"body").cast("string"), "$.occurrenceUtcTime"), "MM/dd/yyyy HH:mm:ss")
         .cast(TimestampType).as("occurrenceUtcTime"),
         $"enqueuedTime".as("enqueuedUtcTime"),
         to_utc_timestamp(current_timestamp(),"MM/dd/yyyy HH:mm:ss").alias("processedUtcTime"),
         get_json_object(($"body").cast("string"), "$.deviceId").alias("deviceId"),
         get_json_object(($"body").cast("string"), "$.sensorType").alias("sensorType"),
         get_json_object(($"body").cast("string"), "$.deliveryId").alias("deliveryId"),
         get_json_object(($"body").cast("string"), "$.temperature").cast(DoubleType).alias("temperature")
        )
        .where("sensorType = 'drone-state-sensor;v1'").select("*")

val hotTempStream = bodyStream.withWatermark("occurrenceUtcTime", "5 seconds")
                              .groupBy(window(col("occurrenceUtcTime"), "90 seconds", "90 seconds"),
                                              col("deviceId"),col("deliveryId"))
                              .agg(avg("temperature").as("avgtemperature"),
                                   last("enqueuedUtcTime").as("lastenqueuedutctime"),
                                   last("occurrenceUtcTime").as("lastoccurrenceutctime"),
                                   last("processedUtcTime").as("lastprocessedutctime"))


val eventsDf = hotTempStream.withColumn("message",lit("hotTemperature"))
                       .withColumn("partitionid",lit(0))
                       .where("avgtemperature >=75")
                       .select($"message",
                                $"partitionid",
                                $"deviceId".as("deviceid"),
                                $"deliveryId".as("deliveryid"),
                                $"avgtemperature",
                                $"lastenqueuedutctime",
                                $"lastoccurrenceutctime",
                                $"lastprocessedutctime")



val ehwrite =
  eventsDf
  .select(to_json(struct($"*"))).toDF("body")
  .writeStream
  .format("eventhubs")
  .options(eventHubsConfAlert.toMap)
  .outputMode("append")
  .option("checkpointLocation", "/hottoalert1")
  .start()


// COMMAND ----------

dbutils.fs.mkdirs("/hottocosmos1/")
dbutils.fs.mkdirs("/hottoalert1/")

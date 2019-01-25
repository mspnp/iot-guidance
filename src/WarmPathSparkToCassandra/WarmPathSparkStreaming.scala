// Databricks notebook source
// This notebook reads a stream of drone telemetry events from Azure IoT Hub/Event Hub, transforming each event message, and writing the resulting message stream to Cassandra using the proprietary BYOS DSE Spark Cassandra Connector: https://docs.datastax.com/en/dse/6.0/dse-dev/datastax_enterprise/spark/byosIntro.html. The BYOS version of the connector is used over the OSS version because the BYOS connector supports DSE Geospatial data types such as Point.

// You may need to add spark.cassandra.auth.username and spark.cassandra.auth.password configuration values if you are using username/password authentication in Cassandra.

// This notebook saves deviceId, enqueuedTime, and location to Cassandra. The location data is sent in a Well-known text form of a geometric point: https://en.wikipedia.org/wiki/Well-known_text 

// This notebook uses the Spark Streaming API. At the time this notebook was developed the Spark Structured Streaming API are not fully supported by the Spark Cassandra Connector.

// This notebook depends on the following libraries:
// azure-eventhubs-spark_2.11-2.3.2
// DSE BYOS 6.0.1 jar copied from Cassandra node

// Please note that secrets management is only supported in the Premium sku of Azure Databricks

// COMMAND ----------

import org.apache.spark._
import org.apache.spark.streaming._
import org.apache.spark.eventhubs.ConnectionStringBuilder
import org.apache.spark.eventhubs.{ EventHubsConf, EventPosition, EventHubsUtils }
import org.apache.spark.sql._
import org.apache.spark.sql.functions.get_json_object
import org.json._
import com.datastax.spark.connector._
import com.datastax.spark.connector.streaming._
                                                
val connectionString = ConnectionStringBuilder(dbutils.secrets.get("mySecretScope", "myEventHubConnectionString"))
      .setEventHubName("myEventHubName")
      .build

val ehConf =
      EventHubsConf(connectionString)
      .setMaxEventsPerTrigger(64000)
      .setConsumerGroup("myConsumerGroup")
      .setStartingPosition(EventPosition.fromEndOfStream)
      .setMaxRatePerPartition(3000)

val ssc = new StreamingContext(sc, Seconds(1))

val incomingStream = EventHubsUtils.createDirectStream(ssc, ehConf)


val locationStream = incomingStream.map{record => {
  val eventData = record.asInstanceOf[com.microsoft.azure.eventhubs.EventData]
  val jsonBody = new JSONObject(new String(eventData.getBytes()))

  if (jsonBody.getString("sensorType")=="drone-event-sensor;v1" ){
    val position = jsonBody.getString("position")
    val latitude = position.split("\\|")(0)
    val longitude = position.split("\\|")(1)
    
    (
      jsonBody.getString("deviceId"), 
      eventData.getSystemProperties().getEnqueuedTime().toString(),
      "POINT (" + longitude + " " + latitude + ")"
    )    
  }
  else null
}}.filter(record => record != null)

locationStream.saveToCassandra("device_telemetry", "positions2", SomeColumns("device_id", "event_time", "location"))

ssc.start()
ssc.awaitTermination()

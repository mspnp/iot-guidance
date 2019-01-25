-- Databricks notebook source
-- MAGIC %scala
-- MAGIC 
-- MAGIC dbutils.fs.mount(
-- MAGIC   source = "wasbs://telemetry@<your storage account name>.blob.core.windows.net/<your iot hub name>/",
-- MAGIC   mountPoint = "/mnt/rawtelemetry",
-- MAGIC   extraConfigs = Map("fs.azure.account.key.<your storage account name>.blob.core.windows.net" -> "<your access key>"))
-- MAGIC 
-- MAGIC spark.conf.set("avro.mapred.ignore.inputs.without.extension", "false")

-- COMMAND ----------

DROP TABLE IF EXISTS rawtelemetry;

CREATE TEMPORARY TABLE rawtelemetry
  USING com.databricks.spark.avro OPTIONS (path="/mnt/rawtelemetry/*/*/*/*/*/*")

-- COMMAND ----------

------------------------------------------------------------
-- Copyright (c) Microsoft Corporation.  All rights reserved.
-- Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
------------------------------------------------------------

DROP TABLE IF EXISTS enrichedtelemetrytable;

CREATE TABLE `enrichedtelemetrytable`(
  `occurrenceUtcTime` string, 
  `deviceId` string, 
  `sensorType` string, 
  `deliveryId` string, 
  `velocity` double, 
  `acceleration` double, 
  `latitude` double, 
  `longitude` double, 
  `altitude` double, 
  `flightStatus` string);

INSERT INTO TABLE enrichedtelemetrytable
SELECT * FROM (
    SELECT
        get_json_object(telemetryjson,'$.occurrenceUtcTime') AS occurrenceUtcTime,
        get_json_object(telemetryjson,'$.deviceId') AS deviceId,	  
        get_json_object(telemetryjson,'$.sensorType') AS sensorType,
        get_json_object(telemetryjson,'$.deliveryId') AS deliveryId,
        get_json_object(telemetryjson,'$.velocity') AS velocity,
        get_json_object(telemetryjson,'$.acceleration') AS acceleration,
        split(get_json_object(telemetryjson,'$.position'),'\\|')[0] AS latitude,
        split(get_json_object(telemetryjson,'$.position'),'\\|')[1] AS longitude,
        split(get_json_object(telemetryjson,'$.position'),'\\|')[2] AS altitude,
        array('DPS', 'INF', 'PUP', 'DOP', 'ARS')[cast(get_json_object(telemetryjson,'$.flightStatus') AS int)] AS flightStatus
	    --Flight Status Codes: 1) Depart station, 2) In flight, 3) Picked up package, 4) Dropped off package, 5) Arrived at station
    FROM (
	    SELECT cast(body as string) AS telemetryjson FROM rawtelemetry
    ) sub1
) sub2
WHERE sensorType LIKE 'drone-event-sensor%'

-- COMMAND ----------

------------------------------------------------------------
-- Copyright (c) Microsoft Corporation.  All rights reserved.
-- Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
------------------------------------------------------------

DROP TABLE IF EXISTS deliverysummarytable;

CREATE TABLE `deliverysummarytable`(
  `deliveryid` string, 
  `startdate` string, 
  `completedate` string);
 
INSERT INTO TABLE deliverysummarytable
SELECT deliveryId, max(startDateTime) as startDate, max(completeDateTime) as completeDate
FROM (
    SELECT deliveryId,
        CASE
            WHEN flightStatus == 'DPS' --Departed Station
                THEN occurrenceUtcTime
                ELSE null
            END AS startDateTime,
        CASE
            WHEN flightStatus == 'DOP' --Dropped off Package
                THEN occurrenceUtcTime
                ELSE null
            END AS completeDateTime
    FROM enrichedtelemetrytable
    WHERE flightStatus == 'DPS' OR flightStatus == 'DOP'
) sub
GROUP BY deliveryId

-- COMMAND ----------

select * from deliverysummarytable

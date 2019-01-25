------------------------------------------------------------
-- Copyright (c) Microsoft Corporation.  All rights reserved.
-- Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
------------------------------------------------------------

-- Please provide the names of your storage account, blob container, and IoT Hub in the script below.
DROP TABLE IF EXISTS rawtelemetry;

CREATE EXTERNAL TABLE rawtelemetry
  ROW FORMAT SERDE
  'org.apache.hadoop.hive.serde2.avro.AvroSerDe'
  STORED AS INPUTFORMAT
  'org.apache.hadoop.hive.ql.io.avro.AvroContainerInputFormat'
  OUTPUTFORMAT
  'org.apache.hadoop.hive.ql.io.avro.AvroContainerOutputFormat'
  LOCATION
  'wasbs://telemetry@<your storage account>.blob.core.windows.net/<your IoT Hub name>/'
  TBLPROPERTIES (
    'avro.schema.url'='wasbs://telemetry@<your storage account>.blob.core.windows.net/telemetry.avsc');
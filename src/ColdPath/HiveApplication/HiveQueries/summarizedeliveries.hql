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
CREATE TABLE events (deviceid varchar(50),timestamp datetime, geo varchar(255), json varchar(max))
GO
CREATE CLUSTERED COLUMNSTORE INDEX cci ON events
GO
CREATE NONCLUSTERED INDEX nclIdxDashboard
ON [dbo].[events] ([deviceid],[timestamp]) INCLUDE ([geo])
GO
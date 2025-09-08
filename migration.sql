-- This script needs to be run manually to update the database schema.
-- It adds the PingHistory column to the Rede table.

ALTER TABLE Rede
ADD PingHistory NVARCHAR(MAX) NULL;

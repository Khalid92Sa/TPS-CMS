-- Lets a delegation duration be unlimited (no expiration date).
-- Existing durations stay limited. Delegations created from an unlimited duration
-- store ExpirationDate = 9999-12-31 and DurationMinutes = NULL.
-- Safe to re-run.

IF COL_LENGTH('dbo.DelegationDurations', 'IsUnlimited') IS NULL
    ALTER TABLE [dbo].[DelegationDurations] ADD [IsUnlimited] BIT NOT NULL CONSTRAINT [DF_DelegationDurations_IsUnlimited] DEFAULT 0;
GO

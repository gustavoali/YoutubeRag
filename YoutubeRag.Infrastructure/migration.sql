CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    ALTER DATABASE CHARACTER SET utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE TABLE `ProcessingConfigurations` (
        `Id` varchar(36) CHARACTER SET utf8mb4 NOT NULL,
        `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `UseLocalWhisper` tinyint(1) NOT NULL DEFAULT TRUE,
        `UseLocalEmbeddings` tinyint(1) NOT NULL DEFAULT TRUE,
        `MaxConcurrentJobs` int NOT NULL DEFAULT 3,
        `RetryAttempts` int NOT NULL DEFAULT 3,
        `TimeoutMinutes` int NOT NULL DEFAULT 30,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `WhisperModel` varchar(50) CHARACTER SET utf8mb4 NULL,
        `WhisperLanguage` varchar(10) CHARACTER SET utf8mb4 NULL,
        `EmbeddingModel` varchar(100) CHARACTER SET utf8mb4 NULL,
        `ChunkSize` int NOT NULL DEFAULT 500,
        `ChunkOverlap` int NOT NULL DEFAULT 50,
        `DefaultQueue` varchar(50) CHARACTER SET utf8mb4 NULL,
        `Priority` int NOT NULL DEFAULT 0,
        `AdditionalSettings` json NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_ProcessingConfigurations` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE TABLE `Users` (
        `Id` varchar(36) CHARACTER SET utf8mb4 NOT NULL,
        `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Email` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `PasswordHash` longtext CHARACTER SET utf8mb4 NOT NULL,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `IsEmailVerified` tinyint(1) NOT NULL DEFAULT FALSE,
        `EmailVerificationToken` varchar(255) CHARACTER SET utf8mb4 NULL,
        `EmailVerifiedAt` datetime(6) NULL,
        `GoogleId` varchar(255) CHARACTER SET utf8mb4 NULL,
        `GoogleRefreshToken` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `Avatar` varchar(500) CHARACTER SET utf8mb4 NULL,
        `Bio` varchar(500) CHARACTER SET utf8mb4 NULL,
        `LastLoginAt` datetime(6) NULL,
        `FailedLoginAttempts` int NOT NULL DEFAULT 0,
        `LockoutEndDate` datetime(6) NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_Users` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE TABLE `RefreshTokens` (
        `Id` varchar(36) CHARACTER SET utf8mb4 NOT NULL,
        `Token` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `ExpiresAt` datetime(6) NOT NULL,
        `IsRevoked` tinyint(1) NOT NULL DEFAULT FALSE,
        `RevokedAt` datetime(6) NULL,
        `RevokedReason` varchar(500) CHARACTER SET utf8mb4 NULL,
        `DeviceInfo` varchar(255) CHARACTER SET utf8mb4 NULL,
        `IpAddress` varchar(45) CHARACTER SET utf8mb4 NULL,
        `UserId` varchar(36) CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_RefreshTokens` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_RefreshTokens_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE TABLE `Videos` (
        `Id` varchar(36) CHARACTER SET utf8mb4 NOT NULL,
        `Title` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `Description` TEXT CHARACTER SET utf8mb4 NULL,
        `YouTubeId` varchar(50) CHARACTER SET utf8mb4 NULL,
        `Url` varchar(500) CHARACTER SET utf8mb4 NULL,
        `OriginalUrl` varchar(500) CHARACTER SET utf8mb4 NULL,
        `ThumbnailUrl` varchar(500) CHARACTER SET utf8mb4 NULL,
        `Duration` double NULL,
        `ViewCount` int NULL,
        `LikeCount` int NULL,
        `PublishedAt` datetime(6) NULL,
        `ChannelId` varchar(100) CHARACTER SET utf8mb4 NULL,
        `ChannelTitle` varchar(255) CHARACTER SET utf8mb4 NULL,
        `CategoryId` varchar(50) CHARACTER SET utf8mb4 NULL,
        `Tags` json NOT NULL,
        `Status` varchar(255) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Pending',
        `ProcessingStatus` int NOT NULL,
        `TranscriptionStatus` varchar(255) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'NotStarted',
        `FilePath` varchar(500) CHARACTER SET utf8mb4 NULL,
        `AudioPath` varchar(500) CHARACTER SET utf8mb4 NULL,
        `ProcessingLog` TEXT CHARACTER SET utf8mb4 NULL,
        `ErrorMessage` TEXT CHARACTER SET utf8mb4 NULL,
        `ProcessingProgress` int NOT NULL DEFAULT 0,
        `Metadata` JSON NULL,
        `Language` varchar(10) CHARACTER SET utf8mb4 NULL,
        `TranscribedAt` datetime(6) NULL,
        `EmbeddingStatus` varchar(255) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'None',
        `EmbeddedAt` datetime(6) NULL,
        `EmbeddingProgress` int NOT NULL DEFAULT 0,
        `UserId` varchar(36) CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_Videos` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Videos_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE TABLE `Jobs` (
        `Id` varchar(36) CHARACTER SET utf8mb4 NOT NULL,
        `Type` longtext CHARACTER SET utf8mb4 NULL,
        `Status` varchar(255) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Pending',
        `StatusMessage` varchar(500) CHARACTER SET utf8mb4 NULL,
        `Progress` int NOT NULL DEFAULT 0,
        `Result` TEXT CHARACTER SET utf8mb4 NULL,
        `ErrorMessage` TEXT CHARACTER SET utf8mb4 NULL,
        `Parameters` JSON NULL,
        `Metadata` JSON NULL,
        `StartedAt` datetime(6) NULL,
        `CompletedAt` datetime(6) NULL,
        `FailedAt` datetime(6) NULL,
        `RetryCount` int NOT NULL DEFAULT 0,
        `MaxRetries` int NOT NULL DEFAULT 3,
        `Priority` int NOT NULL,
        `WorkerId` varchar(255) CHARACTER SET utf8mb4 NULL,
        `HangfireJobId` varchar(100) CHARACTER SET utf8mb4 NULL,
        `UserId` varchar(36) CHARACTER SET utf8mb4 NOT NULL,
        `VideoId` varchar(36) CHARACTER SET utf8mb4 NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_Jobs` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Jobs_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_Jobs_Videos_VideoId` FOREIGN KEY (`VideoId`) REFERENCES `Videos` (`Id`) ON DELETE SET NULL
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE TABLE `TranscriptSegments` (
        `Id` varchar(36) CHARACTER SET utf8mb4 NOT NULL,
        `VideoId` varchar(36) CHARACTER SET utf8mb4 NOT NULL,
        `Text` TEXT CHARACTER SET utf8mb4 NOT NULL,
        `StartTime` double NOT NULL,
        `EndTime` double NOT NULL,
        `SegmentIndex` int NOT NULL,
        `EmbeddingVector` TEXT CHARACTER SET utf8mb4 NULL,
        `Confidence` double NULL,
        `Language` varchar(10) CHARACTER SET utf8mb4 NULL,
        `Speaker` varchar(100) CHARACTER SET utf8mb4 NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_TranscriptSegments` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_TranscriptSegments_Videos_VideoId` FOREIGN KEY (`VideoId`) REFERENCES `Videos` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE TABLE `JobStages` (
        `Id` varchar(36) CHARACTER SET utf8mb4 NOT NULL,
        `JobId` varchar(36) CHARACTER SET utf8mb4 NOT NULL,
        `StageName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Status` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `Progress` int NOT NULL DEFAULT 0,
        `Order` int NOT NULL,
        `Weight` int NOT NULL DEFAULT 1,
        `StartedAt` datetime(6) NULL,
        `CompletedAt` datetime(6) NULL,
        `ErrorMessage` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `ErrorDetails` json NULL,
        `InputData` json NULL,
        `OutputData` json NULL,
        `RetryCount` int NOT NULL DEFAULT 0,
        `MaxRetries` int NOT NULL DEFAULT 3,
        `NextRetryAt` datetime(6) NULL,
        `EstimatedDuration` time(6) NULL,
        `Metadata` json NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_JobStages` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_JobStages_Jobs_JobId` FOREIGN KEY (`JobId`) REFERENCES `Jobs` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE UNIQUE INDEX `IX_Jobs_HangfireJobId` ON `Jobs` (`HangfireJobId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_Jobs_Status` ON `Jobs` (`Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_Jobs_Status_WorkerId` ON `Jobs` (`Status`, `WorkerId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_Jobs_UserId` ON `Jobs` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_Jobs_UserId_Status` ON `Jobs` (`UserId`, `Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_Jobs_VideoId` ON `Jobs` (`VideoId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_JobStages_CreatedAt` ON `JobStages` (`CreatedAt`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_JobStages_JobId` ON `JobStages` (`JobId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE UNIQUE INDEX `IX_JobStages_JobId_Order` ON `JobStages` (`JobId`, `Order`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_JobStages_Status` ON `JobStages` (`Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_ProcessingConfigurations_CreatedAt` ON `ProcessingConfigurations` (`CreatedAt`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_ProcessingConfigurations_IsActive` ON `ProcessingConfigurations` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE UNIQUE INDEX `IX_ProcessingConfigurations_Name` ON `ProcessingConfigurations` (`Name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_RefreshTokens_ExpiresAt` ON `RefreshTokens` (`ExpiresAt`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE UNIQUE INDEX `IX_RefreshTokens_Token` ON `RefreshTokens` (`Token`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_RefreshTokens_UserId` ON `RefreshTokens` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_RefreshTokens_UserId_IsRevoked` ON `RefreshTokens` (`UserId`, `IsRevoked`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_TranscriptSegments_VideoId` ON `TranscriptSegments` (`VideoId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE UNIQUE INDEX `IX_TranscriptSegments_VideoId_SegmentIndex` ON `TranscriptSegments` (`VideoId`, `SegmentIndex`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_TranscriptSegments_VideoId_StartTime` ON `TranscriptSegments` (`VideoId`, `StartTime`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE UNIQUE INDEX `IX_Users_Email` ON `Users` (`Email`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_Users_GoogleId` ON `Users` (`GoogleId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_Videos_EmbeddingStatus` ON `Videos` (`EmbeddingStatus`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_Videos_Status` ON `Videos` (`Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_Videos_TranscriptionStatus` ON `Videos` (`TranscriptionStatus`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_Videos_UserId` ON `Videos` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_Videos_UserId_Status` ON `Videos` (`UserId`, `Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    CREATE INDEX `IX_Videos_YouTubeId` ON `Videos` (`YouTubeId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251003214418_InitialMigrationWithDefaults') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20251003214418_InitialMigrationWithDefaults', '8.0.11');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;


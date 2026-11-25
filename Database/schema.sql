-- Lodgo A9 member database
CREATE DATABASE IF NOT EXISTS lodgo_a9 CHARACTER SET utf8mb4;
USE lodgo_a9;

ALTER DATABASE CHARACTER SET utf8mb4;


CREATE TABLE `users` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Email` varchar(254) CHARACTER SET utf8mb4 NOT NULL,
    `NormalizedEmail` varchar(254) CHARACTER SET utf8mb4 NOT NULL,
    `FirstName` varchar(80) CHARACTER SET utf8mb4 NOT NULL,
    `LastName` varchar(80) CHARACTER SET utf8mb4 NOT NULL,
    `PasswordHash` varchar(512) CHARACTER SET utf8mb4 NOT NULL,
    `Role` varchar(16) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAtUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_users` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;


CREATE UNIQUE INDEX `IX_users_NormalizedEmail` ON `users` (`NormalizedEmail`);



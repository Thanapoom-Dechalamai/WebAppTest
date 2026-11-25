-- Run after hotel-owner-schema.sql; preserves existing accounts and assignments.
SET NAMES utf8mb4;
USE lodgo_a9;
START TRANSACTION;
INSERT INTO users (FirstName,LastName,Email,NormalizedEmail,PasswordHash,Role,CreatedAtUtc) VALUES ('พนักงานโรงแรม','ตัวอย่าง','hotelowner@lodgo.test','HOTELOWNER@LODGO.TEST','AQAAAAIAAYagAAAAEFAUmeIYEYV7aJjGEHGJ6DJsKneOUXgEUc2d1jOJ75OQPCDnkdnRCkWGtuIjRDvI/Q==','HotelOwner',UTC_TIMESTAMP(6)) ON DUPLICATE KEY UPDATE Id=Id;
INSERT INTO hotel_assignments (HotelId,OwnerUserId,UpdatedAtUtc) SELECT 'th-stay-001',Id,UTC_TIMESTAMP(6) FROM users WHERE NormalizedEmail='HOTELOWNER@LODGO.TEST' AND Role='HotelOwner' ON DUPLICATE KEY UPDATE UpdatedAtUtc=UpdatedAtUtc;
COMMIT;

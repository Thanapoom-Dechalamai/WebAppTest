-- Lodgo: six demo application accounts + realistic activity logs (wallets, orders, wallet_entries, promotions).
-- Requires Database/schema.sql or application startup first.
SET NAMES utf8mb4;
USE lodgo_a9;
START TRANSACTION;

INSERT INTO users (FirstName, LastName, Email, NormalizedEmail, PasswordHash, Role, CreatedAtUtc) VALUES
('ลูกค้า', 'ตัวอย่าง', 'customer@lodgo.test', 'CUSTOMER@LODGO.TEST', 'AQAAAAIAAYagAAAAELZyfuU4lJGmJT2VZucCBvav8slqkDO7JCfJFPFmAYRoDd62aBuxxCEH3d3tiqTB5g==', 'User', '2026-11-01 02:00:00.000000'),
('พนักงาน', 'จองห้องพัก', 'staff@lodgo.test', 'STAFF@LODGO.TEST', 'AQAAAAIAAYagAAAAEOH0nV7bq4mgLkYUpVNVQxAbR2JWKg2gpzdSClgIkR9JvM23n4//47eMVWmJraLLBA==', 'Staff', '2026-11-01 02:05:00.000000'),
('เจ้าของ', 'กิจการ', 'owner@lodgo.test', 'OWNER@LODGO.TEST', 'AQAAAAIAAYagAAAAEDY+G6X76c8OZEM9lYldHIRTEXvyo7u8K5brFtoBvqSruGm2YClxVejlNzrQ/FN+oQ==', 'Owner', '2026-11-01 02:10:00.000000'),
('ผู้จัดการ', 'ทีมงาน', 'manager@lodgo.test', 'MANAGER@LODGO.TEST', 'AQAAAAIAAYagAAAAEESrLVyb+7VEHQ826yRPNLAM48HYJg5NO35syYwnRzsqzy7EycvsQaO/VoCXe4CxkA==', 'Manager', '2026-11-01 02:15:00.000000'),
('ผู้ดูแล', 'ระบบ', 'admin@lodgo.test', 'ADMIN@LODGO.TEST', 'AQAAAAIAAYagAAAAEDDNB0sxSnW7v0zBOImjWP7Yn1V37qBG9a7UgJ/NB2tWnRSXhCdig+Qv7kvIzX7m9A==', 'Admin', '2026-11-01 02:20:00.000000'),
('เจ้าหน้าที่', 'บัญชี', 'ac@lodgo.test', 'AC@LODGO.TEST', 'AQAAAAIAAYagAAAAEGtQmCMm/+r0+sZabj6vivAFBuSC43Hk3NGZ7OhUNg/iJyxvARXDBQ05I3pjKmJXgw==', 'AC', '2026-11-01 02:25:00.000000')
ON DUPLICATE KEY UPDATE PasswordHash = VALUES(PasswordHash);

SET @u_customer = (SELECT Id FROM users WHERE NormalizedEmail = 'CUSTOMER@LODGO.TEST' LIMIT 1);
SET @u_staff    = (SELECT Id FROM users WHERE NormalizedEmail = 'STAFF@LODGO.TEST' LIMIT 1);
SET @u_owner    = (SELECT Id FROM users WHERE NormalizedEmail = 'OWNER@LODGO.TEST' LIMIT 1);
SET @u_manager  = (SELECT Id FROM users WHERE NormalizedEmail = 'MANAGER@LODGO.TEST' LIMIT 1);
SET @u_admin    = (SELECT Id FROM users WHERE NormalizedEmail = 'ADMIN@LODGO.TEST' LIMIT 1);
SET @u_ac       = (SELECT Id FROM users WHERE NormalizedEmail = 'AC@LODGO.TEST' LIMIT 1);

-- 1. Member Wallets
INSERT INTO member_wallets (UserId, Balance) VALUES
(@u_customer, 6100.00),
(@u_staff,    3800.00),
(@u_owner,   14400.00),
(@u_manager,  5050.00),
(@u_admin,    6600.00),
(@u_ac,       3550.00)
ON DUPLICATE KEY UPDATE Balance = VALUES(Balance);

-- 2. Stay Orders (Bookings)
INSERT INTO stay_orders (UserId, RequestId, HotelId, HotelName, City, RoomName, CheckIn, CheckOut, Rooms, Guests, NightlyRate, Total, Status, CreatedAtUtc) VALUES
(@u_customer, '11111111-1111-4111-8111-111111111101', 'th-stay-010', 'U Nimman Chiang Mai', 'เชียงใหม่', 'พรีเมียร์ · เตียงแฝด', '2026-11-15 00:00:00.000000', '2026-11-17 00:00:00.000000', 1, 2, 1900.00, 3800.00, 'Confirmed', '2026-11-05 03:22:00.000000'),
(@u_customer, '11111111-1111-4111-8111-111111111102', 'th-stay-012', 'Grande Centre Point Pattaya', 'ชลบุรี', 'ดีลักซ์ · เตียงใหญ่', '2026-11-18 00:00:00.000000', '2026-11-20 00:00:00.000000', 1, 2, 1950.00, 3900.00, 'Cancelled', '2026-11-06 04:10:00.000000'),
(@u_customer, '11111111-1111-4111-8111-111111111103', 'th-stay-018', 'Panan Krabi Resort', 'กระบี่', 'สวีท · พื้นที่พักผ่อน', '2026-11-22 00:00:00.000000', '2026-11-24 00:00:00.000000', 1, 2, 2550.00, 5100.00, 'Confirmed', '2026-11-08 08:30:00.000000'),
(@u_staff,    '11111111-1111-4111-8111-111111111104', 'th-stay-001', 'Amari Don Muang Airport Bangkok Hotel', 'กรุงเทพมหานคร', 'ดีลักซ์ · เตียงใหญ่', '2026-11-16 00:00:00.000000', '2026-11-17 00:00:00.000000', 1, 1, 1200.00, 1200.00, 'Confirmed', '2026-11-07 02:15:00.000000'),
(@u_owner,    '11111111-1111-4111-8111-111111111105', 'th-stay-043', 'SO/ Sofitel Hua Hin', 'เพชรบุรี', 'สวีท · พื้นที่พักผ่อน', '2026-11-25 00:00:00.000000', '2026-11-27 00:00:00.000000', 1, 2, 2800.00, 5600.00, 'Confirmed', '2026-11-08 04:20:00.000000'),
(@u_manager,  '11111111-1111-4111-8111-111111111106', 'th-stay-016', 'U Inchantree Kanchanaburi Hotel', 'กาญจนบุรี', 'ดีลักซ์ · เตียงใหญ่', '2026-11-21 00:00:00.000000', '2026-11-22 00:00:00.000000', 1, 2, 2950.00, 2950.00, 'Confirmed', '2026-11-09 03:18:00.000000'),
(@u_admin,    '11111111-1111-4111-8111-111111111107', 'th-stay-032', 'Nan Seasons Boutique Resort', 'น่าน', 'พรีเมียร์ · เตียงแฝด', '2026-11-19 00:00:00.000000', '2026-11-20 00:00:00.000000', 1, 2, 3400.00, 3400.00, 'Cancelled', '2026-11-09 07:10:00.000000'),
(@u_admin,    '11111111-1111-4111-8111-111111111108', 'th-stay-011', 'Le Méridien Chiang Rai Resort, Thailand', 'เชียงราย', 'ดีลักซ์ · เตียงใหญ่', '2026-11-28 00:00:00.000000', '2026-11-30 00:00:00.000000', 1, 2, 1700.00, 3400.00, 'Confirmed', '2026-11-09 07:40:00.000000'),
(@u_ac,       '11111111-1111-4111-8111-111111111109', 'th-stay-046', 'Krungsri River Hotel', 'พระนครศรีอยุธยา', 'ดีลักซ์ · เตียงใหญ่', '2026-11-20 00:00:00.000000', '2026-11-21 00:00:00.000000', 1, 2, 2450.00, 2450.00, 'Confirmed', '2026-11-10 02:15:00.000000')
ON DUPLICATE KEY UPDATE Status = VALUES(Status);

SET @ord_cust_cancel  = (SELECT Id FROM stay_orders WHERE UserId = @u_customer AND RequestId = '11111111-1111-4111-8111-111111111102' LIMIT 1);
SET @ord_admin_cancel = (SELECT Id FROM stay_orders WHERE UserId = @u_admin    AND RequestId = '11111111-1111-4111-8111-111111111107' LIMIT 1);

-- 3. Wallet Entries (TopUp, Payment, Refund logs)
INSERT INTO wallet_entries (UserId, RequestId, Kind, Amount, BalanceAfter, Description, CreatedAtUtc) VALUES
-- Customer logs
(@u_customer, 'topup:22222222-2222-4222-8222-222222222201', 'TopUp',   15000.00, 15000.00, 'เติมเครดิตด้วยการโอนจำลอง', '2026-11-05 03:15:00.000000'),
(@u_customer, 'order:11111111-1111-4111-8111-111111111101', 'Payment', -3800.00, 11200.00, 'จอง U Nimman Chiang Mai', '2026-11-05 03:22:00.000000'),
(@u_customer, 'order:11111111-1111-4111-8111-111111111102', 'Payment', -3900.00,  7300.00, 'จอง Grande Centre Point Pattaya', '2026-11-06 04:10:00.000000'),
(@u_customer, CONCAT('refund:', @ord_cust_cancel),          'Refund',   3900.00, 11200.00, CONCAT('คืนเครดิตรายการ LG', LPAD(@ord_cust_cancel, 6, '0'), ' · เจ้าหน้าที่ #', @u_staff, ': ลูกค้าแจ้งเลื่อนวันเดินทางเนื่องจากติดภารกิจ'), '2026-11-06 06:45:00.000000'),
(@u_customer, 'order:11111111-1111-4111-8111-111111111103', 'Payment', -5100.00,  6100.00, 'จอง Panan Krabi Resort', '2026-11-08 08:30:00.000000'),
-- Staff logs
(@u_staff,    'topup:22222222-2222-4222-8222-222222222202', 'TopUp',    5000.00,  5000.00, 'เติมเครดิตด้วยการโอนจำลอง', '2026-11-07 02:00:00.000000'),
(@u_staff,    'order:11111111-1111-4111-8111-111111111104', 'Payment', -1200.00,  3800.00, 'จอง Amari Don Muang Airport Bangkok Hotel', '2026-11-07 02:15:00.000000'),
-- Owner logs
(@u_owner,    'topup:22222222-2222-4222-8222-222222222203', 'TopUp',   20000.00, 20000.00, 'เติมเครดิตด้วยการโอนจำลอง', '2026-11-08 04:00:00.000000'),
(@u_owner,    'order:11111111-1111-4111-8111-111111111105', 'Payment', -5600.00, 14400.00, 'จอง SO/ Sofitel Hua Hin', '2026-11-08 04:20:00.000000'),
-- Manager logs
(@u_manager,  'topup:22222222-2222-4222-8222-222222222204', 'TopUp',    8000.00,  8000.00, 'เติมเครดิตด้วยการโอนจำลอง', '2026-11-09 03:00:00.000000'),
(@u_manager,  'order:11111111-1111-4111-8111-111111111106', 'Payment', -2950.00,  5050.00, 'จอง U Inchantree Kanchanaburi Hotel', '2026-11-09 03:18:00.000000'),
-- Admin logs
(@u_admin,    'topup:22222222-2222-4222-8222-222222222205', 'TopUp',   10000.00, 10000.00, 'เติมเครดิตด้วยการโอนจำลอง', '2026-11-09 07:00:00.000000'),
(@u_admin,    'order:11111111-1111-4111-8111-111111111107', 'Payment', -3400.00,  6600.00, 'จอง Nan Seasons Boutique Resort', '2026-11-09 07:10:00.000000'),
(@u_admin,    CONCAT('refund:', @ord_admin_cancel),         'Refund',   3400.00, 10000.00, CONCAT('คืนเครดิตรายการ LG', LPAD(@ord_admin_cancel, 6, '0')), '2026-11-09 07:25:00.000000'),
(@u_admin,    'order:11111111-1111-4111-8111-111111111108', 'Payment', -3400.00,  6600.00, 'จอง Le Méridien Chiang Rai Resort, Thailand', '2026-11-09 07:40:00.000000'),
-- AC logs
(@u_ac,       'topup:22222222-2222-4222-8222-222222222206', 'TopUp',    6000.00,  6000.00, 'เติมเครดิตด้วยการโอนจำลอง', '2026-11-10 02:00:00.000000'),
(@u_ac,       'order:11111111-1111-4111-8111-111111111109', 'Payment', -2450.00,  3550.00, 'จอง Krungsri River Hotel', '2026-11-10 02:15:00.000000')
ON DUPLICATE KEY UPDATE BalanceAfter = VALUES(BalanceAfter), Description = VALUES(Description);

-- 4. Promotions (created/updated by Manager and Admin)
DELETE FROM promotions WHERE Title IN ('test', 'ดีลเที่ยวหน้าหนาว เหนือ-อีสาน รับเครดิตคืนพิเศษ', 'พักผ่อนวันธรรมดา ห้องสวีทและพรีเมียร์ราคาพิเศษ', 'แคมเปญเปิดตัวระบบจองที่พัก Lodgo ครบ 77 จังหวัด');
INSERT INTO promotions (Title, Details, StartsOn, EndsOn, IsActive, UpdatedByUserId, UpdatedAtUtc) VALUES
('ดีลเที่ยวหน้าหนาว เหนือ-อีสาน รับเครดิตคืนพิเศษ', 'จองที่พักโซนภาคเหนือและภาคตะวันออกเฉียงเหนือขั้นต่ำ 2 คืน ยกเลิกฟรีก่อนวันเช็กอินพร้อมคืนเครดิตเต็มจำนวนทันที', '2026-11-01 00:00:00.000000', '2026-12-31 00:00:00.000000', 1, @u_manager, '2026-11-05 04:00:00.000000'),
('พักผ่อนวันธรรมดา ห้องสวีทและพรีเมียร์ราคาพิเศษ', 'อัปเกรดห้องพักเป็นห้องพรีเมียร์เตียงแฝดหรือห้องสวีทพร้อมอาหารเช้าสำหรับสมาชิก Lodgo ทุกท่าน', '2026-11-05 00:00:00.000000', '2026-11-30 00:00:00.000000', 1, @u_admin, '2026-11-07 05:30:00.000000'),
('แคมเปญเปิดตัวระบบจองที่พัก Lodgo ครบ 77 จังหวัด', 'โปรโมชั่นช่วงเปิดตัวระบบทดลองจองที่พักออนไลน์ด้วยเครดิตจำลอง (สิ้นสุดแคมเปญแล้ว)', '2026-10-01 00:00:00.000000', '2026-10-31 00:00:00.000000', 0, @u_manager, '2026-11-01 03:00:00.000000');

COMMIT;

SELECT Id, FirstName, LastName, Email, Role FROM users WHERE NormalizedEmail IN ('CUSTOMER@LODGO.TEST', 'STAFF@LODGO.TEST', 'OWNER@LODGO.TEST', 'MANAGER@LODGO.TEST', 'ADMIN@LODGO.TEST', 'AC@LODGO.TEST') ORDER BY FIELD(Role, 'User', 'Staff', 'Owner', 'Manager', 'Admin', 'AC');

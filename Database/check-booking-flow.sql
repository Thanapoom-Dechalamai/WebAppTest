-- Read-only queries. Select database lodgo_a9 first.
USE lodgo_a9;

-- Catalog: website reads these tables on each request.
SELECT Id,Name,City,Price,IsActive FROM hotels ORDER BY Id;
SELECT * FROM room_types ORDER BY Extra;

-- Orders with the customer and the assigned hotel owner.
SELECT o.Id,o.HotelName,o.RoomName,o.Status,o.Total,o.CheckIn,o.CheckOut,
 c.Email AS CustomerEmail,u.Email AS AssignedOwner
FROM stay_orders o JOIN users c ON c.Id=o.UserId
LEFT JOIN hotel_assignments a ON a.HotelId=o.HotelId
LEFT JOIN users u ON u.Id=a.OwnerUserId ORDER BY o.Id DESC;

-- Credit balance must equal the total ledger; no rows = no mismatch.
SELECT w.UserId,w.Balance,COALESCE(SUM(e.Amount),0) AS LedgerBalance
FROM member_wallets w LEFT JOIN wallet_entries e ON e.UserId=w.UserId
GROUP BY w.UserId,w.Balance HAVING w.Balance<>COALESCE(SUM(e.Amount),0);

-- Log history (UTC stored; Thai time shown). No web log viewer is exposed.
SELECT l.Id,DATE_ADD(l.CreatedAtUtc,INTERVAL 7 HOUR) AS ThaiTime,
 l.ActorUserId,u.Email AS Actor,l.TableName,l.Action,l.RecordId,l.OldData,l.NewData
FROM audit_logs l LEFT JOIN users u ON u.Id=l.ActorUserId
ORDER BY l.Id DESC LIMIT 200;

-- Owner receipts and reversals.
SELECT OrderId,OwnerUserId,Kind,Amount,CreatedAtUtc FROM hotel_owner_entries ORDER BY Id DESC;

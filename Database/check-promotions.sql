-- Read-only. Select the existing database first.
USE lodgo_a9;
SELECT Category, COUNT(*) AS Hotels FROM stay_environments WHERE Category<>'' GROUP BY Category;
SELECT h.Id,h.Name,e.Category,e.ViewType,e.HasPool,e.SeaDistanceKm,e.ForestDistanceKm,e.SourceUrl,h.PhotoUrl,h.PhotoSourceUrl FROM hotels h LEFT JOIN stay_environments e ON e.HotelId=h.Id ORDER BY h.Id;
SELECT Id,Code,DiscountKind,DiscountValue,MinNights,MinSpend,MaxDiscount,IsActive,StartsOn,EndsOn FROM promotions ORDER BY Id;
SELECT r.UserId,r.Code,r.OrderId,r.Subtotal,r.DiscountAmount,o.Total,o.Status,r.CreatedAtUtc FROM promotion_redemptions r JOIN stay_orders o ON o.Id=r.OrderId ORDER BY r.Id DESC;
SELECT PromotionId,UserId,COUNT(*) AS Uses FROM promotion_redemptions GROUP BY PromotionId,UserId HAVING COUNT(*)>1;
SELECT o.Id FROM stay_orders o WHERE o.Subtotal IS NOT NULL AND o.Subtotal-o.DiscountAmount<>o.Total;
SELECT w.UserId,w.Balance,COALESCE(SUM(e.Amount),0) AS LedgerBalance FROM member_wallets w LEFT JOIN wallet_entries e ON e.UserId=w.UserId GROUP BY w.UserId,w.Balance HAVING w.Balance<>COALESCE(SUM(e.Amount),0);
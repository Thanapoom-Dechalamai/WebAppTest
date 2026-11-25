SET NAMES utf8mb4;
USE lodgo_a9;
CREATE TABLE IF NOT EXISTS hotel_assignments (
    HotelId varchar(32) NOT NULL, OwnerUserId int NOT NULL,
    UpdatedAtUtc datetime(6) NOT NULL,
    PRIMARY KEY (HotelId, OwnerUserId),
    KEY IX_hotel_assignments_owner (OwnerUserId),
    FOREIGN KEY (OwnerUserId) REFERENCES users(Id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS hotel_owner_entries (
    Id int NOT NULL AUTO_INCREMENT PRIMARY KEY, OwnerUserId int NOT NULL, OrderId int NOT NULL,
    HotelId varchar(32) NOT NULL, HotelName varchar(120) NOT NULL, Kind varchar(20) NOT NULL,
    Amount decimal(12,2) NOT NULL, CreatedAtUtc datetime(6) NOT NULL,
    UNIQUE KEY UX_hotel_owner_order_kind (OrderId, Kind),
    FOREIGN KEY (OwnerUserId) REFERENCES users(Id) ON DELETE RESTRICT,
    FOREIGN KEY (OrderId) REFERENCES stay_orders(Id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS hotel_decisions (
    OrderId int NOT NULL PRIMARY KEY, ActorUserId int NOT NULL,
    Action varchar(20) NOT NULL, Note varchar(255) NOT NULL, UpdatedAtUtc datetime(6) NOT NULL,
    FOREIGN KEY (OrderId) REFERENCES stay_orders(Id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

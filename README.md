# Simple ASP.NET Core MVC Todo List

โปรเจกต์ตัวอย่าง Todo List แบบ CRUD ครบ (Create, Read, Update, Delete)
เขียนด้วย ASP.NET Core MVC (.NET 8), เก็บข้อมูลแบบ in-memory (ไม่ต้องใช้ฐานข้อมูล)
เหมาะสำหรับใช้เป็นจุดเริ่มต้นศึกษาโครงสร้าง MVC

## โครงสร้างโปรเจกต์
```
TodoListApp/
├── Controllers/
│   └── TodoController.cs      # CRUD actions
├── Models/
│   └── TodoItem.cs            # โมเดลข้อมูล + validation
├── Data/
│   └── TodoStore.cs           # เก็บข้อมูลใน memory (singleton)
├── Views/
│   ├── Todo/                  # Index, Create, Edit, Delete
│   └── Shared/_Layout.cshtml  # เลย์เอาต์หลัก (Bootstrap 5)
├── wwwroot/css/site.css
├── Program.cs
└── TodoListApp.csproj
```

## วิธีรัน

ต้องติดตั้ง [.NET 8 SDK](https://dotnet.microsoft.com/download) ก่อน

```bash
cd TodoListApp
dotnet restore
dotnet run
```

จากนั้นเปิดเบราว์เซอร์ไปที่ URL ที่ terminal แสดง (เช่น `https://localhost:5001` หรือ `http://localhost:5000`)

## ฟีเจอร์
- แสดงรายการงานทั้งหมด พร้อมสถานะเสร็จ/ไม่เสร็จ
- เพิ่มงานใหม่ (ชื่อ, รายละเอียด, กำหนดส่ง)
- แก้ไขงาน
- ลบงาน (มีหน้ายืนยัน)
- ติ๊กเครื่องหมายเสร็จ/ยังไม่เสร็จได้ทันทีจากหน้ารายการ
- Validation ฝั่ง client และ server (Data Annotations)

## ต่อยอด
ตอนนี้ข้อมูลเก็บใน memory เท่านั้น (หายเมื่อรีสตาร์ทแอป) ถ้าต้องการเก็บถาวร
ให้เปลี่ยน `TodoStore` เป็น Entity Framework Core + SQLite/SQL Server แทนได้

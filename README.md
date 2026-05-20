# Quanlybanvexekhach

# Các api của 3 ngôn ngữ
API Đã Thêm
Go service, port 7001:

POST /api/go/stops/generate: sinh điểm đón/trả cho chuyến.
POST /api/go/routes/estimate: ước tính tuyến đường.
Rust service, port 7002:

POST /api/rust/seats/generate: sinh danh sách ghế theo capacity.
POST /api/rust/seats/validate: kiểm tra ghế có hợp lệ không.
Java service, port 7003:

POST /api/java/tickets/qr: sinh QR text.
POST /api/java/tickets/invoice-code: sinh mã hóa đơn.
C# đã gọi các service này ở luồng thật:

TripsController: gọi Go khi tạo/sửa chuyến hoặc tự sinh StopPoints.
SeatsController: gọi Rust khi lấy sơ đồ ghế và validate ghế khi hold.
BookingsController: gọi Rust validate ghế, gọi Java sinh QR/mã hóa đơn khi tạo booking.

# Cách chạy server
Cách Chạy
Mở 4 terminal.

Terminal 1, Go service:

cd "C:\Users\Dat\Documents\Cuối\FW (2)\FW (3)\FW\BaseCore\Integrations\GoService"
go run main.go
Máy hiện tại của bạn chưa nhận lệnh go, nên cần cài Go SDK trước hoặc thêm Go vào PATH.

Terminal 2, Rust service:

cd "C:\Users\Dat\Documents\Cuối\FW (2)\FW (3)\FW\BaseCore\Integrations\RustService"
cargo run
Terminal 3, Java service:

cd "C:\Users\Dat\Documents\Cuối\FW (2)\FW (3)\FW\BaseCore\Integrations\JavaService"
javac TicketIntegrationService.java
java TicketIntegrationService
Terminal 4, C# backend:

cd "C:\Users\Dat\Documents\Cuối\FW (2)\FW (3)\FW\BaseCore"
dotnet run --project BaseCore.APIService
Frontend nếu cần:

cd "C:\Users\Dat\Documents\Cuối\FW (2)\FW (3)\FW\BaseCore\BaseCore.WebClient"
npm.cmd run dev

# Cách test và chứng minh api
Hãy nhập các dòng này và test bằng Postman sau đó xem phần terminal của từng server để thấy api hoạt động.

Test trực tiếp từng ngôn ngữ bằng Postman
Go API 1:

POST http://localhost:7001/api/go/stops/generate
Content-Type: application/json
Body:

{
  "departureLocation": "Đà Nẵng",
  "arrivalLocation": "Huế",
  "departureTime": "2026-05-20T07:30:00",
  "arrivalTime": "2026-05-20T10:30:00"
}
Go API 2:

POST http://localhost:7001/api/go/routes/estimate
Body:

{
  "from": "Đà Nẵng",
  "to": "Huế"
}




Rust API 1:

POST http://localhost:7002/api/rust/seats/generate
Body:

{
  "capacity": 45
}
Rust API 2:

POST http://localhost:7002/api/rust/seats/validate
Body:

{
  "capacity": 45,
  "seatLabels": ["A1", "B2", "Z99"]
}








Java API 1:

POST http://localhost:7003/api/java/tickets/qr
Body:

{
  "bookingId": 22,
  "tripId": 10,
  "seatLabels": ["D3"],
  "customerPhone": "0825352966"
}
Java API 2:

POST http://localhost:7003/api/java/tickets/invoice-code
Body:

{
  "bookingId": 22,
  "bookingDate": "2026-05-20T10:00:00"
}

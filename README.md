# PM Tạp Hóa Desktop (WinForms + SQLite)

Ứng dụng quản lý kho và bán hàng (POS) cho cửa hàng tạp hóa nhỏ, chạy cục bộ trên máy tính.

## Công nghệ

- .NET 8 Windows Forms
- SQLite (nhúng file, không cần cài server)
- Dapper
- ClosedXML (xuất Excel `.xlsx`)

## Chạy dự án

1. Cài .NET SDK 8 trên Windows.
2. Mở thư mục `PMTapHoa.Desktop`.
3. Chạy:

```bash
dotnet restore
dotnet run
```

## Tài khoản mặc định

- Manager: `admin` / `admin123`
- Staff: `staff` / `staff123`

## Tính năng chính

- POS quét mã vạch, tự tăng số lượng nếu quét trùng.
- Thanh toán: lưu hóa đơn, trừ kho, in hóa đơn nhiệt (58/80mm).
- Quản lý kho: CRUD sản phẩm, nhập hàng nhanh, cảnh báo thiếu hàng.
- Công nợ: ghi nợ, thu nợ từng phần, theo dõi lịch sử thanh toán.
- Báo cáo: doanh thu, xuất Excel.
- Lịch sử hóa đơn + in lại.
- Quản lý người dùng, đổi mật khẩu, phân quyền.
- Audit log thao tác.
- Cấu hình máy in mặc định.
- Sao lưu/khôi phục SQLite.
- Dashboard tổng quan.
- Nạp dữ liệu demo.
- Kiểm tra sức khỏe hệ thống.

## Dữ liệu

- Schema được tạo từ `PMTapHoa.Desktop/database.sql`.
- File database chạy thực tế nằm tại:
  `%LocalAppData%\PMTapHoa\taphoa.db`

## Lưu ý vận hành

- Khuyến nghị đổi mật khẩu mặc định ngay khi triển khai.
- Dùng chức năng sao lưu định kỳ hằng ngày.
- Chỉ dùng "Khôi phục" khi đã xác nhận ghi đè toàn bộ dữ liệu hiện tại.

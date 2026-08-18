# Tuần 6 - Entity Framework Core

Mục tiêu tuần: Kết nối API với SQL Server qua EF Core, migration, relationship, performance cơ bản.
Sản phẩm chính của tuần: Student API dùng EF Core + SQL Server.

---

## Ngày 1: Kết nối EF Core

**Deliverable:** GET đọc SQL Server

### Nội dung
- **DbContext/DbSet:** bổ sung đủ `DbSet` trong `AppDbContext` cho các bảng đã tạo ở Tuần 5 (`Students`, `Classes`, `Subjects`, `StudentGrades`).
- **Entity class:** tạo/sửa lại các entity `Student`, `Class`, `Subject`, `StudentGrade` khớp đúng tên cột với bảng SQL đã có (`StudentID`, `StudentCode`, `FullName`, `Gender`, `BirthDate`, `Email`, `ClassID`...), thêm navigation property (`Student.Class`, `Class.Students`).
- **Connection string:** xác nhận lại `appsettings.json` trỏ đúng connection string `defaultConnection` tới database `StudentManagement` (dùng `(localdb)\MSSQLLocalDB`).
- **API đọc DB thật:** chuyển `StudentController` từ dùng list tĩnh trong RAM (`_students`) sang `AppDbContext` (`_context.Students`) — áp dụng cho toàn bộ `GetAll`, `GetById`, `Create`, `Update`, `Delete`, `GetPage`, đều dùng `async/await` và `SaveChangesAsync()`.

### Thay đổi chính
- `Data/AppDbContext.cs` — thêm `DbSet<Class>`, `DbSet<Subject>`, `DbSet<StudentGrade>`; cấu hình composite key cho `StudentGrade` (`StudentID` + `SubjectID`); cấu hình quan hệ `Student` ↔ `Class`.
- `models/Student.cs`, `models/Class.cs`, `models/Subject.cs`, `models/StudentGrade.cs` — entity class khớp đúng bảng SQL.
- `Controllers/StudentController.cs` — bỏ hẳn list `_students` tĩnh, toàn bộ CRUD chuyển sang `_context`.
- `DTO/CreateStudentDto.cs`, `DTO/UpdateStudentDto.cs`, `DTO/StudentResponseDto.cs` — sửa lại đầy đủ field (`StudentCode`, `Gender`, `BirthDate`, `Email`, `ClassID`) thay vì chỉ `Name`/`Age` cũ.
- `Validators/CreateStudentDtoValidator.cs`, `Validators/UpdateStudentDtoValidator.cs` — cập nhật rule khớp DTO mới.

---

## Ngày 2: Migration

**Deliverable:** Migration files

### Nội dung
- **Migration:** dùng `add-migration` để EF Core tự sinh script tạo bảng dựa trên Entity class, thay vì viết SQL thủ công như Tuần 5.
- **Update database:** dùng `update-database` để áp dụng migration vào database thật, tạo bảng `__EFMigrationsHistory` theo dõi migration đã chạy.
- **Seed data:** dùng `HasData()` trong `OnModelCreating` để nạp sẵn dữ liệu mẫu (Classes, Students, Subjects, StudentGrades) khớp với data đã tạo thủ công ở Tuần 5.
- **Rollback:** hiểu cách quay lại migration trước bằng `update-database <TenMigration>` hoặc `update-database 0`, và `migrations remove` để xóa migration chưa apply.

### Vấn đề gặp phải và cách xử lý
- Bảng `Students`, `Classes`, `Subjects`, `StudentGrades` đã được tạo thủ công bằng SQL script ở Tuần 5 → khi chạy `update-database` lần đầu bị lỗi `There is already an object named 'Classes' in the database` vì migration cố `CREATE TABLE` lại từ đầu.
- **Cách xử lý:** `DROP TABLE`/`DROP VIEW` toàn bộ (đúng thứ tự: bảng con trước, bảng cha sau) để database trống hoàn toàn, sau đó `update-database` chạy migration `initialCreate` tạo lại toàn bộ từ Entity class — đảm bảo schema đồng bộ 100% với code.
- Sửa warning `No store type was specified for the decimal property 'Mark'` bằng cách thêm `HasPrecision(4, 2)` cho property `Mark` trong `OnModelCreating`.

### Thay đổi chính
- `Migrations/` — thư mục migration mới sinh ra (`initialCreate` và các migration tiếp theo nếu có).
- `Data/AppDbContext.cs` — thêm seed data đầy đủ 4 bảng qua `HasData()`, thêm `HasPrecision(4, 2)` cho `Mark`.

---

## Ngày 3: EF Core relationships

**Deliverable:** API trả dữ liệu quan hệ

**Đánh giá:** Chấm endpoint

### Nội dung
- **Relationship 1-n:** bổ sung khai báo quan hệ cho `StudentGrade` (trước đó chỉ mới có `Student` ↔ `Class`):
  - `StudentGrade` → `Student` (1 sinh viên có nhiều điểm)
  - `StudentGrade` → `Subject` (1 môn có nhiều điểm)
- **Include/ThenInclude:** load dữ liệu quan hệ trực tiếp (`Include`) và lồng nhau 2 cấp (`ThenInclude`) — ví dụ `StudentGrade → Student → Class`.
- **DTO projection:** dùng `Select()` đặt trực tiếp trong query LINQ (trước `ToListAsync()`), để EF Core dịch thẳng thành 1 câu SQL chỉ lấy đúng cột cần dùng, không load toàn bộ entity vào RAM rồi mới mapping thủ công.
- **Endpoint dữ liệu quan hệ:** áp dụng cả 2 kỹ thuật trên vào endpoint thực tế.

### Thay đổi chính
- `Data/AppDbContext.cs` — thêm cấu hình quan hệ `StudentGrade` → `Student`, `StudentGrade` → `Subject`.
- `DTO/StudentWithClassDto.cs` (mới) — `StudentID`, `StudentCode`, `FullName`, `ClassName`.
- `DTO/GradeDetailDto.cs` (mới) — `StudentCode`, `StudentFullName`, `ClassName`, `SubjectName`, `Mark`.
- `Controllers/StudentController.cs` — thêm các endpoint:
  - `GET /api/students/with-class` — DTO projection, chỉ lấy đúng cột cần.
  - `GET /api/students/include-demo` — dùng `Include` để so sánh với cách projection.
  - `GET /api/students/grades-detail` — dùng `Include` + `ThenInclude` load quan hệ 2 cấp.
  - Đồng thời áp dụng lại projection cho `GetAll`, `GetById`, `GetPage` (tối ưu, tránh kéo toàn bộ entity về RAM rồi mới map).



---

## Cách test toàn bộ
1. Đảm bảo database `StudentManagement` đã migrate và seed đầy đủ (`update-database`).
2. Chạy project (`dotnet run` hoặc F5).
3. Test qua Swagger hoặc file `.http`:
   - `GET /api/students`
   - `GET /api/students/{id}`
   - `GET /api/students/Page`
   - `GET /api/students/with-class`
   - `GET /api/students/include-demo`
   - `GET /api/students/grades-detail`
   - `POST /api/students`, `PUT /api/students/{id}`, `DELETE /api/students/{id}`
 
   ###
   # Tuần 6 - Ngày 4 & Ngày 5

---

## Ngày 4: EF Core efficient querying

**Deliverable:** Tối ưu list endpoint
**Đánh giá:** Code review performance

### Nội dung
- **AsNoTracking:** tắt tracking cho query chỉ đọc, tránh EF Core tốn tài nguyên theo dõi thay đổi không cần thiết.
- **Projection DTO:** dùng `Select()` chiếu thẳng sang DTO trong query LINQ, tránh load thừa cột không dùng đến.
- **Paging performance:** đảm bảo thứ tự đúng trong query — `Where → OrderBy → Skip → Take → Select` — và bắt buộc có `OrderBy` để phân trang cho kết quả ổn định giữa các lần gọi.
- **Query tối ưu:** tổng hợp checklist áp dụng cho toàn bộ endpoint đọc danh sách.

### Vấn đề phát hiện khi review code và cách sửa
Rà lại `StudentController.cs`, phát hiện các endpoint đọc dữ liệu (`GetAll`, `GetById`) chưa áp dụng đúng chuẩn tối ưu, dù `GetPage` đã đúng từ trước:

| Endpoint | Trước | Sau |
|---|---|---|
| `GetAll` | `ToListAsync()` rồi mới `Select` map ở C#, không `AsNoTracking` | Thêm `AsNoTracking()`, chuyển `Select()` vào trong query trước `ToListAsync()`, thêm `OrderBy` |
| `GetById` | Load full entity rồi map, không `AsNoTracking` | Dùng `Select()` projection trực tiếp trong query, thêm `AsNoTracking()` |
| `GetPage` | Đã đúng chuẩn từ trước | Giữ nguyên |
| `GetGradesDetail` | Dùng `Include`/`ThenInclude`, thiếu `AsNoTracking` | Thêm `AsNoTracking()` (giữ nguyên `Include` vì đây là endpoint minh họa cách load quan hệ, không phải projection) |

### Checklist tối ưu áp dụng
1. `AsNoTracking()` cho mọi query chỉ đọc (`GET`).
2. `Select()` projection thay vì load full entity khi chỉ cần đọc dữ liệu.
3. `Where` lọc trước, `OrderBy` bắt buộc khi có phân trang, `Skip/Take` sau `OrderBy`.
4. Không gọi `ToListAsync()` giữa chừng rồi xử lý tiếp bằng LINQ to Objects — giữ toàn bộ logic trong `IQueryable` để EF Core dịch hết thành 1 câu SQL.
5. Các endpoint `PUT`/`DELETE` (cần sửa entity) **không** dùng `AsNoTracking()`, vì cần EF Core theo dõi thay đổi để `SaveChangesAsync()` hoạt động đúng.

---

## Ngày 5: Checkpoint tuần 6

**Deliverable:** Source + DB script
**Đánh giá:** Checkpoint tuần 6

### Nội dung
- **Tổng hợp EF Core:** rà soát lại toàn bộ kiến thức đã học trong tuần — DbContext/DbSet, Entity class, Migration, Relationship, Include/ThenInclude, Projection, AsNoTracking.
- **Hoàn thiện CRUD DB:** đảm bảo `Create`, `Update`, `Delete` hoạt động đúng với database thật qua EF Core, không còn sót thao tác nào dùng dữ liệu giả.
- **Viết hướng dẫn migration:** tài liệu ngắn cho người khác (hoặc chính mình sau này) biết cách setup lại database từ đầu bằng migration.
- **Checkpoint tuần 6:** tổng kết, chuẩn bị source code + script database để mentor review.

### Hướng dẫn migration (setup database từ đầu)

Dành cho người mới clone project, chưa có database `StudentManagement`:

1. Cài SQL Server / LocalDB (`(localdb)\MSSQLLocalDB`) nếu chưa có.
2. Cấu hình connection string trong `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "defaultConnection": "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=StudentManagement;Integrated Security=True;TrustServerCertificate=True;"
   }
   ```
3. Mở **Package Manager Console** trong Visual Studio, chạy:
   ```
   update-database
   ```
   Lệnh này tự động tạo database `StudentManagement`, áp dụng toàn bộ migration trong `Migrations/` (tạo bảng `Classes`, `Students`, `Subjects`, `StudentGrades`), và nạp sẵn seed data mẫu.
4. Chạy project (`dotnet run` hoặc F5), test qua Swagger.

**Nếu cần tạo migration mới** (sau khi sửa Entity class):
```
add-migration <TenMigrationMoTaThayDoi>
update-database
```

**Nếu cần rollback:**
```
update-database <TenMigrationTruocDo>
```
hoặc về trạng thái ban đầu:
```
update-database 0
```

=======
  



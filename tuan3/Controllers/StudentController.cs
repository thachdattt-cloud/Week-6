using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tuan3.ApiResponse;
using tuan3.Data;
using tuan3.DTO;
using tuan3.Exceptions;
using tuan3.models;
using tuan3.Pagination;

namespace tuan3.Controllers
{
    [ApiController]
    [Route("api/students")]
    public class StudentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StudentController(AppDbContext context)
        {
            _context = context;
        }

        private StudentResponseDto MapToDto(Student student)
        {
            int age = 0;
            if (student.BirthDate.HasValue)
            {
                age = DateTime.Today.Year - student.BirthDate.Value.Year;
            }

            var dto = new StudentResponseDto();
            dto.Id = student.StudentID;
            dto.Name = student.FullName;
            dto.Age = age;
            dto.StudentCode = student.StudentCode;
            dto.Gender = student.Gender;
            dto.Email = student.Email;
            dto.ClassID = student.ClassID;

            return dto;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<StudentResponseDto>>>> GetAll([FromQuery] string? keyword)
        {
            var query = _context.Students.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(s => s.FullName.Contains(keyword));
            }

            var result = await query
                .OrderBy(s => s.StudentID)
                .Select(s => new StudentResponseDto
                {
                    Id = s.StudentID,
                    Name = s.FullName,
                    StudentCode = s.StudentCode,
                    Gender = s.Gender,
                    Email = s.Email,
                    ClassID = s.ClassID,
                    Age = s.BirthDate.HasValue ? DateTime.Today.Year - s.BirthDate.Value.Year : 0
                })
                .ToListAsync();

            return Ok(ApiResponse<List<StudentResponseDto>>.Ok(result, "Lay danh sach thanh cong"));
        }

        [HttpGet("with-class")]
        public async Task<ActionResult<ApiResponse<List<StudentWithClassDto>>>> GetAllWithClass()
        {
            var result = await _context.Students
                .Select(s => new StudentWithClassDto
                {
                    StudentID = s.StudentID,
                    StudentCode = s.StudentCode,
                    FullName = s.FullName,
                    ClassName = s.Class.ClassName
                })
                .ToListAsync();

            return Ok(ApiResponse<List<StudentWithClassDto>>.Ok(result, "Lay danh sach sinh vien kem lop thanh cong"));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<StudentResponseDto>>> GetById([FromRoute] int id)
        {
            var dto = await _context.Students
                .AsNoTracking()
                .Where(s => s.StudentID == id)
                .Select(s => new StudentResponseDto
                {
                    Id = s.StudentID,
                    Name = s.FullName,
                    StudentCode = s.StudentCode,
                    Gender = s.Gender,
                    Email = s.Email,
                    ClassID = s.ClassID,
                    Age = s.BirthDate.HasValue ? DateTime.Today.Year - s.BirthDate.Value.Year : 0
                })
                .FirstOrDefaultAsync();

            if (dto == null)
            {
                throw new NotFoundException("khong tim thay sinh vien");
            }

            return Ok(ApiResponse<StudentResponseDto>.Ok(dto, "Lay du lieu thanh cong"));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<StudentResponseDto>>> Create([FromBody] CreateStudentDto dto)
        {
            //if (string.IsNullOrWhiteSpace(dto.Name))
            //{
            //    throw new BadRequestException("ten khong duoc de trong kk");
            //}

            var newStudent = new Student();
            newStudent.FullName = dto.Name;
            newStudent.Gender = dto.Gender;
            newStudent.BirthDate = dto.BirthDate;
            newStudent.Email = dto.Email;
            newStudent.ClassID = dto.ClassID;

            if (!string.IsNullOrWhiteSpace(dto.StudentCode))
            {
                newStudent.StudentCode = dto.StudentCode;
            }

            _context.Students.Add(newStudent);
            await _context.SaveChangesAsync();

            var response = MapToDto(newStudent);

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.Id },
                ApiResponse<StudentResponseDto>.Ok(response, "Tao moi thanh cong"));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<StudentResponseDto>>> Update([FromRoute] int id, [FromBody] UpdateStudentDto dto)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentID == id);
            if (student == null)
            {
                throw new NotFoundException("khong tim thay sinh vien can sua");
            }

            student.FullName = dto.Name;
            student.Gender = dto.Gender;
            student.BirthDate = dto.BirthDate;
            student.Email = dto.Email;
            student.ClassID = dto.ClassID;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<StudentResponseDto>.Ok(MapToDto(student), "Cap nhat thanh cong"));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> Delete([FromRoute] int id)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentID == id);
            if (student == null)
            {
                throw new NotFoundException("khong tim thay sinh vien can xoa");
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<string>.Ok(null!, "Xoa thanh cong"));
        }

        [HttpGet("Page")]
        public async Task<ActionResult<ApiResponse<PagedResult<StudentResponseDto>>>> GetPage([FromQuery] PaginationQuery query)
        {
            var queryStudent = _context.Students.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                queryStudent = queryStudent.Where(s => s.FullName.Contains(query.Keyword));
            }

            var totalItems = await queryStudent.CountAsync();
            var skipCount = (query.PageNumber - 1) * query.PageSize;

            var items = await queryStudent
                .OrderBy(s => s.StudentID)
                .Skip(skipCount)
                .Take(query.PageSize)
                .Select(s => new StudentResponseDto
                {
                    Id = s.StudentID,
                    Name = s.FullName,
                    StudentCode = s.StudentCode,
                    Gender = s.Gender,
                    Email = s.Email,
                    ClassID = s.ClassID,
                    Age = s.BirthDate.HasValue ? DateTime.Today.Year - s.BirthDate.Value.Year : 0
                })
                .ToListAsync();

            var pageResult = new PagedResult<StudentResponseDto>();
            pageResult.Items = items;
            pageResult.PageNumber = query.PageNumber;
            pageResult.PageSize = query.PageSize;
            pageResult.TotalItems = totalItems;

            return Ok(ApiResponse<PagedResult<StudentResponseDto>>.Ok(pageResult, "danh sach thong tin"));
        }

        [HttpGet("grades-detail")]
        public async Task<ActionResult<ApiResponse<List<GradeDetailDto>>>> GetGradesDetail()
        {
            var grades = await _context.StudentGrades
                .AsNoTracking()
                .Include(g => g.Student)
                    .ThenInclude(s => s.Class)
                .Include(g => g.Subject)
                .ToListAsync();

            var result = new List<GradeDetailDto>();
            foreach (var g in grades)
            {
                var dto = new GradeDetailDto();
                dto.StudentCode = g.Student.StudentCode;
                dto.StudentFullName = g.Student.FullName;
                dto.ClassName = g.Student.Class.ClassName;
                dto.SubjectName = g.Subject.SubjectName;
                dto.Mark = g.Mark;
                result.Add(dto);
            }

            return Ok(ApiResponse<List<GradeDetailDto>>.Ok(result, "Lay chi tiet diem thanh cong"));
        }

        [HttpGet("test-error-500")]
        public IActionResult testError()
        {
            throw new Exception("loi 500");
        }
    }
}
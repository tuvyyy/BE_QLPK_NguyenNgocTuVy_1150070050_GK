using _1150070050_QLPK_GK_LTM.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _1150070050_QLPK_GK_LTM.Models.DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using QuestPDF.Drawing;


namespace _1150070050_QLPK_GK_LTM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestResultsController : ControllerBase
    {
        private readonly ClinicDbContext _context;

        public TestResultsController(ClinicDbContext context)
        {
            _context = context;
        }

        // ✅ Lấy tất cả kết quả xét nghiệm
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var results = await _context.TestResults
                .Include(r => r.Record)
                .Select(r => new
                {
                    r.ResultId,
                    r.RecordId,
                    RecordCode = r.Record.RecordCode,
                    r.TestType,
                    r.ResultSummary,
                    r.FileUrl,
                    r.IndicatorsJson,
                    r.IsSigned,
                    r.DoctorId,   // ✅ chỉ lấy DoctorId
                    r.CreatedAt,
                    r.SignedAt
                })
                .ToListAsync();

            return Ok(results);
        }
        
        
        
        // ✅ Lấy chi tiết theo ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _context.TestResults
                .Include(r => r.Record)
                .ThenInclude(rec => rec.Patient)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
                return NotFound(new { message = "❌ Không tìm thấy kết quả xét nghiệm!" });

            // 🩵 Nếu chưa có file URL thì tự sinh luôn
            if (string.IsNullOrEmpty(result.FileUrl))
            {
                var host = $"{Request.Scheme}://{Request.Host}";
                result.FileUrl = $"{host}/api/TestResults/export-pdf/{result.ResultId}";
            }

            // 🩷 Trả dữ liệu JSON gọn gàng (không bị vòng lặp)
            var response = new
            {
                result.ResultId,
                result.RecordId,
                RecordCode = result.Record?.RecordCode,
                result.TestType,
                result.ResultSummary,
                result.FileUrl,
                result.CreatedAt,
                result.IsSigned,
                result.SignedAt
            };

            return Ok(response);
        }




        // ✅ Lấy dữ liệu biểu đồ (dạng JSON indicators)
        [HttpGet("{id}/chart-data")]
        public async Task<IActionResult> GetChartData(int id)
        {
            var result = await _context.TestResults.FindAsync(id);

            if (result == null)
                return NotFound(new { message = "❌ Không tìm thấy kết quả xét nghiệm!" });

            if (string.IsNullOrEmpty(result.IndicatorsJson))
                return NotFound(new { message = "❌ Kết quả này chưa có dữ liệu biểu đồ!" });

            // ✅ Trả trực tiếp JSON string ra cho Android
            return Content(result.IndicatorsJson, "application/json");
        }


        // ✅ Lấy theo RecordId
        [HttpGet("by-record/{recordId}")]
        public async Task<IActionResult> GetByRecord(int recordId)
        {
            var results = await _context.TestResults
                .Include(r => r.Record)
                .Where(r => r.RecordId == recordId)
                .Select(r => new
                {
                    r.ResultId,
                    r.RecordId,
                    RecordCode = r.Record.RecordCode,
                    r.TestType,
                    r.ResultSummary,
                    r.FileUrl,
                    r.CreatedAt,
                    r.IsSigned,
                    r.SignedAt
                })
                .ToListAsync();

            if (!results.Any())
                return NotFound(new { message = "❌ Hồ sơ này chưa có kết quả xét nghiệm!" });

            return Ok(results);
        }



        // ✅ Lấy theo PatientId
        [HttpGet("by-patient/{patientId}")]
        public async Task<IActionResult> GetByPatient(int patientId)
        {
            var results = await _context.TestResults
                .Include(r => r.Record)
                .Where(r => r.Record.PatientId == patientId)
                .Select(r => new
                {
                    r.ResultId,
                    r.Record.RecordId,
                    r.Record.RecordCode,
                    r.TestType,
                    r.ResultSummary,
                    r.CreatedAt,
                    r.IsSigned,
                    r.SignedAt
                })
                .ToListAsync();

            if (!results.Any())
                return NotFound(new { message = "❌ Bệnh nhân chưa có kết quả xét nghiệm nào!" });

            return Ok(results);
        }



        // ✅ Thêm mới kết quả xét nghiệm
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TestResultCreateDto dto)
        {
            if (!_context.MedicalRecords.Any(r => r.RecordId == dto.RecordId))
                return BadRequest(new { message = "❌ Hồ sơ bệnh án không tồn tại!" });

            var result = new TestResult
            {
                RecordId = dto.RecordId,
                TestType = dto.TestType,
                ResultSummary = dto.ResultSummary,
                FileUrl = dto.FileUrl,
                IndicatorsJson = dto.IndicatorsJson,
                CreatedAt = DateTime.Now,
                IsSigned = false
            };

            _context.TestResults.Add(result);

            // 🔔 Gửi thông báo cho bệnh nhân
            var record = await _context.MedicalRecords
                .Include(r => r.Patient)
                .FirstOrDefaultAsync(r => r.RecordId == dto.RecordId);

            if (record?.Patient != null && record.Patient.UserId.HasValue)
            {
                var notification = new Notification
                {
                    ReceiverId = record.Patient.UserId.Value,
                    Title = "Kết quả xét nghiệm mới",
                    Message = $"Bạn vừa có kết quả xét nghiệm mới cho hồ sơ {record.RecordCode}.",
                    NotificationType = "TestResult",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "✅ Thêm kết quả xét nghiệm thành công!", result.ResultId });
        }



        // ✅ Cập nhật kết quả (chặn nếu đã ký)
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] TestResultUpdateDto dto)
        {
            var result = await _context.TestResults.FindAsync(id);
            if (result == null)
                return NotFound(new { message = "❌ Không tìm thấy kết quả xét nghiệm!" });

            if (result.IsSigned)
                return BadRequest(new { message = "⚠️ Kết quả đã được ký, không thể chỉnh sửa!" });

            result.TestType = dto.TestType ?? result.TestType;
            result.ResultSummary = dto.ResultSummary ?? result.ResultSummary;
            result.FileUrl = dto.FileUrl ?? result.FileUrl;
            result.IndicatorsJson = dto.IndicatorsJson ?? result.IndicatorsJson; // 🩵 BỔ SUNG DÒNG NÀY

            await _context.SaveChangesAsync();
            return Ok(new { message = "✅ Cập nhật thành công!" });
        }



        // ✅ Ký duyệt kết quả
        [HttpPut("{id}/sign")]
        public async Task<IActionResult> SignResult(int id, [FromBody] int doctorId)
        {
            var result = await _context.TestResults
                .Include(r => r.Record)
                .ThenInclude(rec => rec.Patient)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
                return NotFound(new { message = "❌ Không tìm thấy kết quả xét nghiệm!" });

            if (result.IsSigned)
                return BadRequest(new { message = "⚠️ Kết quả này đã được ký duyệt!" });

            result.IsSigned = true;
            result.DoctorId = doctorId;
            result.SignedAt = DateTime.Now;

            // 🔔 Thông báo cho bệnh nhân
            var patient = result.Record.Patient;
            if (patient != null && patient.UserId.HasValue)
            {
                _context.Notifications.Add(new Notification
                {
                    ReceiverId = patient.UserId.Value,
                    Title = "Kết quả xét nghiệm đã được ký duyệt",
                    Message = $"Bác sĩ đã ký duyệt kết quả cho hồ sơ {result.Record.RecordCode}.",
                    NotificationType = "TestResult",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "✅ Đã ký duyệt kết quả xét nghiệm!" });
        }



        // ✅ Hủy ký (cho phép sửa lại)
        [HttpPut("{id}/unsign")]
        public async Task<IActionResult> UnsignResult(int id)
        {
            var result = await _context.TestResults.FindAsync(id);
            if (result == null)
                return NotFound(new { message = "❌ Không tìm thấy kết quả xét nghiệm!" });

            if (!result.IsSigned)
                return BadRequest(new { message = "⚠️ Kết quả này chưa được ký duyệt!" });

            result.IsSigned = false;
            result.SignedAt = null;
            result.DoctorId = null;

            await _context.SaveChangesAsync();
            return Ok(new { message = "✅ Đã hủy ký duyệt, có thể chỉnh sửa lại nội dung!" });
        }



        // ✅ Xóa kết quả (chặn nếu đã ký)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _context.TestResults.FindAsync(id);
            if (result == null)
                return NotFound(new { message = "❌ Không tìm thấy kết quả xét nghiệm!" });

            if (result.IsSigned)
                return BadRequest(new { message = "⚠️ Không thể xóa kết quả đã được ký duyệt!" });

            _context.TestResults.Remove(result);
            await _context.SaveChangesAsync();
            return Ok(new { message = "🗑️ Đã xóa kết quả xét nghiệm!" });
        }



        // ✅ Xuất PDF kết quả xét nghiệm (có biểu đồ + chữ ký)
        [HttpGet("export-pdf/{id}")]
        public async Task<IActionResult> ExportPdf(int id)
        {
            var result = await _context.TestResults
                .Include(r => r.Record)
                .ThenInclude(rec => rec.Patient)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
                return NotFound(new { message = "❌ Không tìm thấy kết quả xét nghiệm!" });

            // 🔹 Parse JSON chỉ số xét nghiệm
            List<(string name, double value, string? unit, string? range)> indicators = new();
            try
            {
                if (!string.IsNullOrEmpty(result.IndicatorsJson))
                {
                    {
                        using var doc = JsonDocument.Parse(result.IndicatorsJson);
                        foreach (var item in doc.RootElement.EnumerateArray())
                        {
                            string name = item.GetProperty("name").GetString() ?? "";
                            double value = item.TryGetProperty("value", out var v) ? v.GetDouble() : 0;
                            string? unit = item.TryGetProperty("unit", out var u) ? u.GetString() : null;
                            string? range = item.TryGetProperty("range", out var r) ? r.GetString() : null;
                            indicators.Add((name, value, unit, range));
                        }
                    }
                }
            }
            catch { }

            byte[] pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);

                    // 🔹 Header
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("PHÒNG KHÁM XYZ")
                                .FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                            col.Item().Text("Địa chỉ: 123 Nguyễn Huệ, TP.HCM").FontSize(10);
                            col.Item().Text("Điện thoại: 0123 456 789").FontSize(10);
                        });

                        row.ConstantItem(80).Height(80)
                            .AlignCenter().AlignMiddle()
                            .Background(Colors.Grey.Lighten3)
                            .Text("LOGO").FontSize(12).Bold();
                    });

                    // 🔹 Nội dung chính
                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        // Tiêu đề
                        col.Item().AlignCenter().Text("KẾT QUẢ XÉT NGHIỆM")
                            .FontSize(20).Bold().FontColor(Colors.Red.Medium);
                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                        // Thông tin chung
                        col.Item().PaddingBottom(10).Grid(grid =>
                        {
                            grid.Columns(2);
                            grid.Item().Text($"Mã hồ sơ: {result.Record.RecordCode}");
                            grid.Item().Text($"Bệnh nhân: {result.Record.Patient.FullName}");
                            grid.Item().Text($"Ngày khám: {result.Record.VisitDate:dd/MM/yyyy}");
                            grid.Item().Text($"Loại xét nghiệm: {result.TestType}");
                        });

                        // Chi tiết
                        col.Item().PaddingBottom(10).Column(detail =>
                        {
                            detail.Item().Text($"Tóm tắt kết quả: {result.ResultSummary}").FontSize(11);
                            detail.Item().Text($"Ngày tạo: {result.CreatedAt:dd/MM/yyyy HH:mm}");
                        });

                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // 🔹 Nếu có dữ liệu chỉ số
                        if (indicators.Any())
                        {
                            col.Item().PaddingTop(15).Text("📊 Chỉ số xét nghiệm:")
                                .FontSize(14).Bold();

                            // Bảng chỉ số
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(3);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(1);
                                    cols.RelativeColumn(2);
                                });

                                // Header
                                table.Header(header =>
                                {
                                    header.Cell().Element(c =>
                                        c.Background(Colors.Grey.Lighten3).Padding(4).Text("Chỉ số").Bold()
                                    );
                                    header.Cell().Element(c =>
                                        c.Background(Colors.Grey.Lighten3).Padding(4).Text("Giá trị").Bold()
                                    );
                                    header.Cell().Element(c =>
                                        c.Background(Colors.Grey.Lighten3).Padding(4).Text("Đơn vị").Bold()
                                    );
                                    header.Cell().Element(c =>
                                        c.Background(Colors.Grey.Lighten3).Padding(4).Text("Mức chuẩn").Bold()
                                    );
                                });

                                // Rows với zebra style
                                for (int i = 0; i < indicators.Count; i++)
                                {
                                    var (name, value, unit, range) = indicators[i];
                                    var bgColor = (i % 2 == 0) ? Colors.White : Colors.Grey.Lighten4;

                                    table.Cell().Element(c =>
                                        c.Background(bgColor).Padding(3).Text(name)
                                    );
                                    table.Cell().Element(c =>
                                        c.Background(bgColor).Padding(3).Text(value.ToString("0.##"))
                                    );
                                    table.Cell().Element(c =>
                                        c.Background(bgColor).Padding(3).Text(unit ?? "-")
                                    );
                                    table.Cell().Element(c =>
                                        c.Background(bgColor).Padding(3).Text(range ?? "-")
                                    );
                                }
                            });

                            // 🔹 Biểu đồ thanh ngang
                            col.Item().PaddingTop(15).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(10);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Chỉ số").Bold();
                                    header.Cell().Text("Biểu đồ (tỉ lệ)").Bold();
                                });

                                var max = indicators.Max(i => i.value);
                                foreach (var ind in indicators)
                                {
                                    var ratio = (float)(ind.value / max * 100);
                                    table.Cell().Text(ind.name);
                                    table.Cell().Stack(stack =>
                                    {
                                        stack.Item().Border(1)
                                            .Width(ratio * 2.5f)
                                            .Height(10)
                                            .Background(Colors.Blue.Medium);
                                        stack.Item().Text($"{ind.value:0.##} {ind.unit}").FontSize(9);
                                    });
                                }
                            });
                        }

                        // 🔹 Chữ ký
                        col.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        if (result.IsSigned && result.DoctorId != null)
                        {
                            col.Item().AlignRight().Column(c =>
                            {
                                c.Item().Text($"Bác sĩ phụ trách: {result.DoctorId}").Bold();
                                c.Item().Text($"Ký ngày: {result.SignedAt:dd/MM/yyyy HH:mm}");
                                c.Item().Text("(Đã ký duyệt điện tử)").Italic().FontSize(10);
                            });
                        }
                        else
                        {
                            col.Item().AlignRight().Text("(Chưa ký duyệt)").Italic().FontSize(10);
                        }
                    });

                    // Footer
                    page.Footer().AlignCenter().Text("Kết quả được tạo bởi hệ thống quản lý phòng khám - © 2025")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            }).GeneratePdf();

            return File(pdf, "application/pdf", $"TestResult_{id}.pdf");
        }

        // 📨 Gửi kết quả xét nghiệm cho bệnh nhân
        [HttpPost("{recordId}/send-result")]
        public async Task<IActionResult> SendResultToPatient(int recordId)
        {
            var record = await _context.MedicalRecords.FindAsync(recordId);
            if (record == null)
                return NotFound(new { message = "❌ Không tìm thấy hồ sơ!" });

            var results = await _context.TestResults
                .Where(r => r.RecordId == recordId)
                .ToListAsync();

            if (results.Count == 0)
                return BadRequest(new { message = "⚠️ Hồ sơ này chưa có kết quả xét nghiệm nào!" });

            //bool allSigned = results.All(r => r.IsSigned);
            //if (!allSigned)
            //    return BadRequest(new { message = "❌ Vẫn còn kết quả chưa ký duyệt!" });

            // ✅ Cập nhật cờ IsResultSent
            record.IsResultSent = true;
            _context.Entry(record).State = EntityState.Modified; // ⚡ bắt EF cập nhật
            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ Đã gửi kết quả xét nghiệm cho bệnh nhân!" });
        }

    }

}


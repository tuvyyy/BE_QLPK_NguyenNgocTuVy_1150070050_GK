using _1150070050_QLPK_GK_LTM.Models.Entities;
using _1150070050_QLPK_GK_LTM.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Helpers;
using QuestPDF.Fluent;

namespace _1150070050_QLPK_GK_LTM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrescriptionsController : ControllerBase
    {
        private readonly ClinicDbContext _context;

        public PrescriptionsController(ClinicDbContext context)
        {
            _context = context;
        }

        // ✅ Lấy danh sách thuốc đang hoạt động (cho bác sĩ kê đơn)
        [HttpGet]
        public IActionResult GetAllActive()
        {
            var list = _context.Medicines
                .Where(m => m.IsActive == true)
                .OrderBy(m => m.MedicineName)
                .Select(m => new
                {
                    m.MedicineId,
                    m.MedicineName,
                    m.Unit,
                    m.IsActive
                })
                .ToList();

            return Ok(list);
        }

        // ✅ Lấy tất cả toa thuốc theo RecordId
        [HttpGet("by-record/{recordId}")]
        public async Task<IActionResult> GetByRecord(int recordId)
        {
            var data = await _context.Prescriptions
                .Include(p => p.Record)
                .Include(p => p.Medicine)
                .Where(p => p.RecordId == recordId)
                .Select(p => new PrescriptionResponseDto
                {
                    PrescriptionId = p.PrescriptionId,
                    MedicineId = p.MedicineId,
                    MedicineName = p.Medicine.MedicineName,
                    Dosage = p.Dosage,
                    Instructions = p.Instructions,
                    Duration = p.Duration,
                    RecordCode = p.Record.RecordCode
                })
                .ToListAsync();

            if (!data.Any())
                return NotFound(new { message = "❌ Không có toa thuốc nào cho hồ sơ này!" });

            return Ok(data);
        }

        // ✅ Lấy tất cả toa thuốc theo PatientId
        [HttpGet("by-patient/{patientId}")]
        public async Task<IActionResult> GetByPatient(int patientId)
        {
            var data = await _context.Prescriptions
                .Include(p => p.Record)
                .ThenInclude(r => r.Patient)
                .Include(p => p.Medicine)
                .Where(p => p.Record.PatientId == patientId)
                .Select(p => new
                {
                    p.PrescriptionId,
                    p.MedicineId,
                    MedicineName = p.Medicine.MedicineName,
                    p.Dosage,
                    p.Instructions,
                    p.Duration,
                    p.Record.RecordId,
                    p.Record.RecordCode,
                    p.Record.VisitDate,
                    p.Record.NextAppointment
                })
                .ToListAsync();

            if (!data.Any())
                return NotFound(new { message = "❌ Bệnh nhân này chưa có toa thuốc nào!" });

            return Ok(data);
        }

        // ✅ Thêm toa thuốc mới (kê đơn)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PrescriptionCreateDto dto)
        {
            // 0️⃣ Kiểm tra đầu vào cơ bản
            if (dto.Duration <= 0 || dto.Duration > 365)
                return BadRequest(new { message = "⚠️ Số ngày dùng thuốc phải từ 1 đến 365 ngày!" });

            if (string.IsNullOrWhiteSpace(dto.Dosage) || string.IsNullOrWhiteSpace(dto.Instructions))
                return BadRequest(new { message = "⚠️ Vui lòng nhập đầy đủ liều dùng và hướng dẫn!" });

            var record = await _context.MedicalRecords
                .Include(r => r.Patient)
                .FirstOrDefaultAsync(r => r.RecordId == dto.RecordId);

            if (record == null)
                return BadRequest(new { message = "❌ Hồ sơ bệnh án không tồn tại!" });

            // 1️⃣ Kiểm tra thuốc có tồn tại và còn hoạt động không
            var medicine = await _context.Medicines.FirstOrDefaultAsync(m => m.MedicineId == dto.MedicineId);
            if (medicine == null || medicine.IsActive == false)
                return BadRequest(new { message = "❌ Thuốc không tồn tại hoặc đã ngừng sử dụng!" });

            // 2️⃣ Check dị ứng thuốc
            var allergy = await _context.PatientAllergies
                .Include(a => a.Medicine)
                .FirstOrDefaultAsync(a => a.PatientId == record.PatientId && a.MedicineId == dto.MedicineId);

            if (allergy != null)
            {
                return BadRequest(new
                {
                    message = "🚨 Cảnh báo: Bệnh nhân dị ứng với thuốc này!",
                    allergy = new
                    {
                        allergy.MedicineId,
                        MedicineName = allergy.Medicine.MedicineName,
                        allergy.Notes
                    }
                });
            }

            // 3️⃣ Check tương tác thuốc với các thuốc đã kê
            var existingMedicines = await _context.Prescriptions
                .Where(p => p.RecordId == dto.RecordId)
                .Select(p => p.MedicineId)
                .ToListAsync();

            var interactions = await _context.DrugInteractions
                .Include(di => di.MedicineId1Navigation)
                .Include(di => di.MedicineId2Navigation)
                .Where(di =>
                    (di.MedicineId1 == dto.MedicineId && existingMedicines.Contains(di.MedicineId2)) ||
                    (di.MedicineId2 == dto.MedicineId && existingMedicines.Contains(di.MedicineId1))
                )
                .ToListAsync();

            if (interactions.Any())
            {
                return BadRequest(new
                {
                    message = "⚠️ Cảnh báo: Thuốc mới kê có tương tác với thuốc khác trong toa!",
                    interactions = interactions.Select(i => new
                    {
                        i.Severity,
                        i.Description,
                        Medicine1 = i.MedicineId1Navigation.MedicineName,
                        Medicine2 = i.MedicineId2Navigation.MedicineName
                    })
                });
            }

            // 4️⃣ Nếu hợp lệ → thêm đơn thuốc
            var prescription = new Prescription
            {
                RecordId = dto.RecordId,
                MedicineId = dto.MedicineId,
                Dosage = dto.Dosage,
                Instructions = dto.Instructions,
                Duration = dto.Duration
            };

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ Thêm toa thuốc thành công!", prescriptionId = prescription.PrescriptionId });
        }

        // ✅ Cập nhật toa thuốc
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PrescriptionCreateDto dto)
        {
            var pres = await _context.Prescriptions.FindAsync(id);
            if (pres == null)
                return NotFound(new { message = "❌ Không tìm thấy toa thuốc!" });

            if (!_context.Medicines.Any(m => m.MedicineId == dto.MedicineId))
                return BadRequest(new { message = "❌ Thuốc không tồn tại!" });

            if (dto.Duration <= 0 || dto.Duration > 365)
                return BadRequest(new { message = "⚠️ Số ngày dùng thuốc phải trong khoảng 1 - 365 ngày!" });

            pres.MedicineId = dto.MedicineId;
            pres.Dosage = dto.Dosage ?? pres.Dosage;
            pres.Instructions = dto.Instructions ?? pres.Instructions;
            pres.Duration = dto.Duration ?? pres.Duration;

            await _context.SaveChangesAsync();
            return Ok(new { message = "✅ Cập nhật toa thuốc thành công!", pres });
        }

        // ✅ Xóa toa thuốc
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var pres = await _context.Prescriptions.FindAsync(id);
            if (pres == null)
                return NotFound(new { message = "❌ Không tìm thấy toa thuốc!" });

            _context.Prescriptions.Remove(pres);
            await _context.SaveChangesAsync();

            return Ok(new { message = "🗑️ Đã xóa toa thuốc!" });
        }

        // ✅ Xuất PDF toa thuốc theo hồ sơ
        [HttpGet("export-pdf/by-record/{recordId}")]
        public async Task<IActionResult> ExportPdfByRecord(int recordId)
        {
            var record = await _context.MedicalRecords
                .Include(r => r.Patient)
                .FirstOrDefaultAsync(r => r.RecordId == recordId);

            if (record == null)
                return NotFound(new { message = "❌ Không tìm thấy hồ sơ bệnh án!" });

            var prescriptions = await _context.Prescriptions
                .Include(p => p.Medicine)
                .Where(p => p.RecordId == recordId)
                .ToListAsync();

            if (!prescriptions.Any())
                return NotFound(new { message = "❌ Hồ sơ này chưa có toa thuốc!" });

            byte[] pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("PHÒNG KHÁM TIUV").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                            col.Item().Text("Địa chỉ: 123 Nguyễn Huệ, TP.HCM");
                            col.Item().Text("Điện thoại: 0123 456 789");
                        });

                        row.ConstantItem(80).Height(80).Background(Colors.Grey.Lighten2)
                           .AlignCenter().AlignMiddle()
                           .Text("LOGO").FontSize(12).Bold();
                    });

                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Item().AlignCenter().Text("TOA THUỐC")
                            .FontSize(20).Bold().FontColor(Colors.Green.Medium);

                        col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                        col.Item().Column(info =>
                        {
                            info.Item().Text($"Mã hồ sơ: {record.RecordCode}");
                            info.Item().Text($"Bệnh nhân: {record.Patient.FullName}");
                            info.Item().Text($"Ngày khám: {record.VisitDate:dd/MM/yyyy}");
                        });

                        col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("STT").Bold();
                                header.Cell().Text("Tên thuốc").Bold();
                                header.Cell().Text("Liều dùng").Bold();
                                header.Cell().Text("Hướng dẫn").Bold();
                                header.Cell().Text("Số ngày").Bold();
                            });

                            int index = 1;
                            foreach (var p in prescriptions)
                            {
                                table.Cell().Text(index++.ToString());
                                table.Cell().Text(p.Medicine?.MedicineName ?? "-");
                                table.Cell().Text(p.Dosage ?? "-");
                                table.Cell().Text(p.Instructions ?? "-");
                                table.Cell().Text(p.Duration?.ToString() ?? "-");
                            }
                        });

                        col.Item().PaddingVertical(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text("");
                            r.ConstantItem(200).Column(c =>
                            {
                                c.Item().AlignCenter().Text("Bác sĩ kê đơn").Bold();
                                c.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(10);
                            });
                        });
                    });

                    page.Footer().AlignCenter()
                        .Text("Toa thuốc được in tự động từ hệ thống quản lý phòng khám - © 2025")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            }).GeneratePdf();

            return File(pdf, "application/pdf", $"Prescription_Record_{recordId}.pdf");
        }

        // ✅ Ký toa thuốc (chốt hồ sơ, không cho sửa)
        [HttpPost("sign/{recordId}")]
        public async Task<IActionResult> SignPrescription(int recordId)
        {
            var record = await _context.MedicalRecords.FindAsync(recordId);
            if (record == null)
                return NotFound(new { message = "❌ Không tìm thấy hồ sơ!" });

            if (record.IsSigned == true)
                return BadRequest(new { message = "⚠️ Hồ sơ đã được ký!" });

            record.IsSigned = true;
            record.SignedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ Hồ sơ đã được bác sĩ ký xác nhận!", recordId, record.SignedAt });
        }

        // ✅ Hủy ký (cho phép sửa/xóa lại)
        [HttpPost("unsign/{recordId}")]
        public async Task<IActionResult> UnsignPrescription(int recordId)
        {
            var record = await _context.MedicalRecords.FindAsync(recordId);
            if (record == null)
                return NotFound(new { message = "❌ Không tìm thấy hồ sơ!" });

            if (record.IsSigned == false)
                return BadRequest(new { message = "⚠️ Hồ sơ chưa được ký!" });

            record.IsSigned = false;
            record.SignedAt = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "🌀 Đã hủy ký, cho phép chỉnh sửa toa thuốc!", recordId });
        }

    }
}

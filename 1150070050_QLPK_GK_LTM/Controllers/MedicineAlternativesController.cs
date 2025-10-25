using _1150070050_QLPK_GK_LTM.Models.DTOs;
using _1150070050_QLPK_GK_LTM.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _1150070050_QLPK_GK_LTM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicineAlternativesController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        public MedicineAlternativesController(ClinicDbContext context) => _context = context;

        [HttpGet("by-medicine/{medicineId}")]
        public IActionResult GetByMedicine(int medicineId)
        {
            var list = _context.MedicineAlternatives
                .Include(a => a.AlternativeMedicine)
                .Where(a => a.MedicineId == medicineId)
                .Select(a => new {
                    a.Id,
                    OriginalMedicineId = a.MedicineId,
                    Alternative = a.AlternativeMedicine.MedicineName,
                    a.Notes
                })
                .ToList();

            return Ok(list);
        }

        [HttpPost]
        public IActionResult Create([FromBody] MedicineAlternativeCreateDto dto)
        {
            var alt = new MedicineAlternative
            {
                MedicineId = dto.MedicineId,
                AlternativeMedicineId = dto.AlternativeMedicineId,
                Notes = dto.Notes
            };

            _context.MedicineAlternatives.Add(alt);
            _context.SaveChanges();
            return Ok(new { message = "✅ Thêm thuốc thay thế thành công!", alt });
        }

    }
}

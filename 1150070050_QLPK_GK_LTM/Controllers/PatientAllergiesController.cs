using _1150070050_QLPK_GK_LTM.Models.DTOs;
using _1150070050_QLPK_GK_LTM.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _1150070050_QLPK_GK_LTM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientAllergiesController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        public PatientAllergiesController(ClinicDbContext context) => _context = context;

        [HttpGet("by-patient/{patientId}")]
        public IActionResult GetByPatient(int patientId)
        {
            var list = _context.PatientAllergies
                .Include(a => a.Medicine)
                .Where(a => a.PatientId == patientId)
                .Select(a => new {
                    a.Id,
                    a.PatientId,
                    Medicine = a.Medicine.MedicineName,
                    a.Notes
                })
                .ToList();

            return Ok(list);
        }

        [HttpPost]
        public IActionResult Create([FromBody] PatientAllergyCreateDto dto)
        {
            var allergy = new PatientAllergy
            {
                PatientId = dto.PatientId,
                MedicineId = dto.MedicineId,
                Notes = dto.Notes
            };

            _context.PatientAllergies.Add(allergy);
            _context.SaveChanges();
            return Ok(new { message = "✅ Thêm dị ứng thành công!", allergy });
        }

    }
}

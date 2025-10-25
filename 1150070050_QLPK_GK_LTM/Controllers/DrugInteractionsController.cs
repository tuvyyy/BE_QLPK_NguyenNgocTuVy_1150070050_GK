using _1150070050_QLPK_GK_LTM.Models.DTOs;
using _1150070050_QLPK_GK_LTM.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _1150070050_QLPK_GK_LTM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DrugInteractionsController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        public DrugInteractionsController(ClinicDbContext context) => _context = context;

        [HttpGet]
        public IActionResult GetAll()
        {
            var list = _context.DrugInteractions
                .Include(di => di.MedicineId1Navigation)
.Include(di => di.MedicineId2Navigation)
.Select(di => new
{
    di.Id,
    Medicine1 = di.MedicineId1Navigation.MedicineName,
    Medicine2 = di.MedicineId2Navigation.MedicineName,
    di.Severity,
    di.Description
})

                .ToList();

            return Ok(list);
        }

        [HttpPost]
        public IActionResult Create([FromBody] DrugInteractionCreateDto dto)
        {
            var di = new DrugInteraction
            {
                MedicineId1 = dto.MedicineId1,
                MedicineId2 = dto.MedicineId2,
                Severity = dto.Severity,
                Description = dto.Description
            };

            _context.DrugInteractions.Add(di);
            _context.SaveChanges();

            return Ok(new { message = "✅ Thêm tương tác thuốc thành công!", di });
        }
    }
}


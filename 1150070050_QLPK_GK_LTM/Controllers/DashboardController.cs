using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _1150070050_QLPK_GK_LTM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly tuvyContext _context;

        public DashboardController(tuvyContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetSummary()
        {
            var totalDoctors = await _context.Doctors.CountAsync();
            var totalPatients = await _context.Patients.CountAsync();
            var totalServices = await _context.Services.CountAsync();
            var totalAppointments = await _context.Appointments.CountAsync();

            return new
            {
                Doctors = totalDoctors,
                Patients = totalPatients,
                Services = totalServices,
                Appointments = totalAppointments
            };
        }

        [HttpGet("appointments-by-month")]
        public async Task<ActionResult<IEnumerable<object>>> GetAppointmentsByMonth()
        {
            var data = await _context.Appointments
                .GroupBy(a => new { a.AppointmentDate.Year, a.AppointmentDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            return data;
        }
    }

}

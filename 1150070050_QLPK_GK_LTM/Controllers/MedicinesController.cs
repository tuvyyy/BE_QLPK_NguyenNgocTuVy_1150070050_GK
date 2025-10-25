using _1150070050_QLPK_GK_LTM.Models.DTOs;
using _1150070050_QLPK_GK_LTM.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace _1150070050_QLPK_GK_LTM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicinesController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        public MedicinesController(ClinicDbContext context)
        {
            _context = context;
        }

        // ✅ Lấy danh sách tất cả thuốc (có lọc theo trạng thái)
        [HttpGet]
        public IActionResult GetAll([FromQuery] bool? active)
        {
            var query = _context.Medicines.AsQueryable();

            // 🔹 Nếu có truyền query param ?active=true/false
            if (active.HasValue)
                query = query.Where(m => m.IsActive == active.Value);

            var medicines = query
                .Select(m => new MedicineDto
                {
                    MedicineId = m.MedicineId,
                    MedicineName = m.MedicineName,
                    Description = m.Description,
                    Unit = m.Unit,
                    MaxDosagePerDay = m.MaxDosagePerDay,
                    IsActive = m.IsActive
                })
                .OrderByDescending(m => m.MedicineId)
                .ToList();

            return Ok(medicines);
        }


        [HttpGet("search")]
        public IActionResult Search([FromQuery] string? keyword = null)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                var all = _context.Medicines
                    .OrderBy(m => m.MedicineName)
                    .Select(m => new MedicineDto
                    {
                        MedicineId = m.MedicineId,
                        MedicineName = m.MedicineName,
                        Description = m.Description,
                        Unit = m.Unit,
                        MaxDosagePerDay = m.MaxDosagePerDay,
                        IsActive = m.IsActive
                    })
                    .ToList();
                return Ok(all);
            }

            keyword = NormalizeText(keyword);

            // 1️⃣ LIKE cơ bản
            var sqlResults = _context.Medicines
                .Where(m =>
                    EF.Functions.Like(m.MedicineName.ToLower(), $"%{keyword}%") ||
                    EF.Functions.Like(m.Description.ToLower(), $"%{keyword}%"))
                .ToList();

            // 2️⃣ Nếu LIKE không có kết quả, dùng fuzzy nâng cao
            if (!sqlResults.Any())
            {
                var allMeds = _context.Medicines.ToList();

                var fuzzyMatches = allMeds
                    .Select(m =>
                    {
                        string name = NormalizeText(m.MedicineName);
                        string desc = NormalizeText(m.Description);

                        int distName = LevenshteinDistance(name, keyword);
                        int distDesc = LevenshteinDistance(desc, keyword);

                        double score = Math.Min(distName, distDesc);

                        // ⚙️ Ưu tiên nếu trùng “root” thuốc
                        string[] roots = { "amoxi", "cillin", "para", "vitamin", "cef", "clar", "azith", "ibup", "ome", "amol" };
                        foreach (var r in roots)
                        {
                            if (keyword.Contains(r) || name.Contains(r))
                                score -= 3;
                        }

                        // ⚙️ Ưu tiên nếu tên chứa hoặc bắt đầu keyword
                        if (name.StartsWith(keyword)) score -= 3;
                        else if (name.Contains(keyword)) score -= 2;

                        // ⚙️ Phạt độ dài chênh lệch
                        score += Math.Abs(name.Length - keyword.Length) * 0.3;

                        // ⚙️ Phạt tên ngắn
                        if (name.Length < 5) score += 2;

                        return new { Medicine = m, Score = score };
                    })
                    .OrderBy(x => x.Score)
                    .Take(10)
                    .ToList();

                // ✅ Nếu có fuzzy match tốt → suggest
                if (fuzzyMatches.Any())
                {
                    var best = fuzzyMatches.First();

                    if (best.Score <= 8) // tăng ngưỡng nhận diện
                    {
                        // ✅ Trả gợi ý thông minh
                        var related = fuzzyMatches
                            .Skip(1)
                            .Take(3)
                            .Select(x => new
                            {
                                x.Medicine.MedicineId,
                                x.Medicine.MedicineName,
                                x.Medicine.Description
                            })
                            .ToList();

                        return Ok(new
                        {
                            message = $"🧩 Có phải bạn muốn tìm: **{best.Medicine.MedicineName}**?",
                            suggestion = new
                            {
                                best.Medicine.MedicineId,
                                best.Medicine.MedicineName,
                                best.Medicine.Description,
                                best.Medicine.Unit,
                                best.Medicine.MaxDosagePerDay,
                                best.Medicine.IsActive
                            },
                            related // Gợi ý thêm 3 thuốc gần đúng
                        });
                    }

                    // Nếu không có score thấp, trả danh sách gần đúng
                    sqlResults = fuzzyMatches
                        .Where(x => x.Score <= 9)
                        .Select(x => x.Medicine)
                        .ToList();
                }
            }

            // 3️⃣ Không có kết quả luôn → báo lỗi
            if (!sqlResults.Any())
                return NotFound(new { message = "❌ Không tìm thấy thuốc phù hợp!" });

            // 4️⃣ Map DTO
            var results = sqlResults
                .Select(m => new MedicineDto
                {
                    MedicineId = m.MedicineId,
                    MedicineName = m.MedicineName,
                    Description = m.Description,
                    Unit = m.Unit,
                    MaxDosagePerDay = m.MaxDosagePerDay,
                    IsActive = m.IsActive
                })
                .OrderBy(m => m.MedicineName)
                .ToList();

            return Ok(results);
        }


        // ==========================
        // 🧩 Helper Functions
        // ==========================
        #region Chuẩn hóa & bỏ dấu
        private static string NormalizeText(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            input = input.ToLower().Trim();
            input = RemoveDiacritics(input);
            input = System.Text.RegularExpressions.Regex.Replace(input, @"[^a-z\s]", "");
            input = System.Text.RegularExpressions.Regex.Replace(input, @"\s+", " ");
            return input;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
        #endregion


        #region Fuzzy Match
        private static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return t.Length;
            if (string.IsNullOrEmpty(t)) return s.Length;

            int[,] d = new int[s.Length + 1, t.Length + 1];
            for (int i = 0; i <= s.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= t.Length; j++) d[0, j] = j;

            for (int i = 1; i <= s.Length; i++)
            {
                for (int j = 1; j <= t.Length; j++)
                {
                    int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }
            return d[s.Length, t.Length];
        }
        #endregion





        // ✅ Lấy thuốc theo ID
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var medicine = _context.Medicines.Find(id);
            if (medicine == null)
                return NotFound(new { message = "❌ Không tìm thấy thuốc!" });

            return Ok(medicine);
        }



        // ✅ Thêm thuốc mới
        [HttpPost]
        public IActionResult Create([FromBody] Medicine medicine)
        {
            if (string.IsNullOrWhiteSpace(medicine.MedicineName))
                return BadRequest(new { message = "❌ Tên thuốc là bắt buộc!" });

            medicine.CreatedAt = DateTime.Now;
            if (medicine.IsActive == null) medicine.IsActive = true;

            _context.Medicines.Add(medicine);
            _context.SaveChanges();

            return Ok(new { message = "✅ Thêm thuốc thành công!", medicine });
        }

        // ✅ Cập nhật thuốc
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] Medicine update)
        {
            if (update == null)
                return BadRequest(new { message = "❌ Dữ liệu gửi lên trống!" });

            var med = _context.Medicines.Find(id);
            if (med == null)
                return NotFound(new { message = "❌ Không tìm thấy thuốc!" });

            med.MedicineName = update.MedicineName ?? med.MedicineName;
            med.Description = update.Description ?? med.Description;
            med.Unit = update.Unit ?? med.Unit;
            med.MaxDosagePerDay = update.MaxDosagePerDay ?? med.MaxDosagePerDay;
            med.IsActive = update.IsActive ?? med.IsActive;

            try
            {
                _context.SaveChanges();
                return Ok(new { message = "✅ Cập nhật thuốc thành công!", med });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "❌ Lỗi khi cập nhật!", error = ex.Message });
            }
        }

        // ✅ Ngừng sử dụng thuốc (soft delete)
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var med = _context.Medicines.Find(id);
            if (med == null)
                return NotFound(new { message = "❌ Không tìm thấy thuốc!" });

            med.IsActive = false;
            _context.SaveChanges();

            return Ok(new { message = "🗑️ Thuốc đã được ngừng sử dụng!", med });
        }

        // ✅ Khôi phục thuốc đã ngừng sử dụng
        [HttpPut("restore/{id}")]
        public IActionResult Restore(int id)
        {
            var med = _context.Medicines.Find(id);
            if (med == null)
                return NotFound(new { message = "❌ Không tìm thấy thuốc!" });

            if (med.IsActive == true)
                return BadRequest(new { message = "⚠️ Thuốc này đang được sử dụng rồi!" });

            med.IsActive = true;
            _context.SaveChanges();

            return Ok(new { message = "✅ Thuốc đã được sử dụng trở lại!", med });
        }
    }
}

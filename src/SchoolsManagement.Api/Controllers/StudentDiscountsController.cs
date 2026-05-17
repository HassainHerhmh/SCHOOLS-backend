using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/student-discounts")]
[AllowAnonymous]
public class StudentDiscountsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public StudentDiscountsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> ListDiscounts(CancellationToken ct)
    {
        var rows = await _db.StudentDiscounts.AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync(ct);
        return Ok(rows.Select(DiscountDto));
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateDiscount([FromBody] UpsertDiscountRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
        {
            return BadRequest(new { message = "اسم الخصم مطلوب." });
        }

        if (body.Amount is null or < 0)
        {
            return BadRequest(new { message = "قيمة الخصم مطلوبة." });
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new StudentDiscountRecord
        {
            Id = Guid.NewGuid(),
            Name = body.Name.Trim(),
            Amount = body.Amount.Value,
            Description = body.Description,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.StudentDiscounts.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(DiscountDto(entity));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDiscount(Guid id, [FromBody] UpsertDiscountRequest body, CancellationToken ct)
    {
        var entity = await _db.StudentDiscounts.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(body.Name))
        {
            entity.Name = body.Name.Trim();
        }

        if (body.Amount is not null)
        {
            entity.Amount = body.Amount.Value;
        }

        if (body.Description is not null)
        {
            entity.Description = body.Description;
        }

        if (body.IsActive.HasValue)
        {
            entity.IsActive = body.IsActive.Value;
        }

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDiscount(Guid id, CancellationToken ct)
    {
        var entity = await _db.StudentDiscounts.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        _db.StudentDiscounts.Remove(entity);
        var apps = await _db.StudentDiscountApplications.Where(a => a.DiscountId == id).ToListAsync(ct);
        _db.StudentDiscountApplications.RemoveRange(apps);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("applications")]
    public async Task<ActionResult<IEnumerable<object>>> ListApplications(
        [FromQuery] Guid? discountId,
        [FromQuery] DateTimeOffset? start,
        [FromQuery] DateTimeOffset? end,
        CancellationToken ct)
    {
        var q = _db.StudentDiscountApplications.AsNoTracking();
        if (discountId.HasValue)
        {
            q = q.Where(a => a.DiscountId == discountId.Value);
        }

        if (start.HasValue)
        {
            q = q.Where(a => a.AppliedAt >= start.Value);
        }

        if (end.HasValue)
        {
            q = q.Where(a => a.AppliedAt <= end.Value);
        }

        var apps = await q.OrderByDescending(a => a.AppliedAt).ToListAsync(ct);
        var studentIds = apps.Select(a => a.StudentId).Distinct().ToList();
        var students = await _db.StudentRecords.AsNoTracking()
            .Where(s => studentIds.Contains(s.Id))
            .ToListAsync(ct);
        var map = students.ToDictionary(s => s.Id);

        return Ok(apps.Select(a => new
        {
            id = a.Id,
            student_id = a.StudentId,
            discount_id = a.DiscountId,
            discount_name = a.DiscountName,
            amount = a.Amount,
            applied_at = a.AppliedAt,
            notes = a.Notes,
            created_by = a.CreatedBy,
            student = map.TryGetValue(a.StudentId, out var st)
                ? new
                {
                    id = st.Id,
                    name = st.Name,
                    level = st.Level,
                    section = st.Section,
                    phone = st.Phone,
                    parent_phone = st.ParentPhone
                }
                : null
        }));
    }

    [HttpPost("apply")]
    public async Task<ActionResult<object>> Apply([FromBody] ApplyDiscountRequest body, CancellationToken ct)
    {
        var discount = await _db.StudentDiscounts.FirstOrDefaultAsync(d => d.Id == body.DiscountId, ct);
        if (discount is null)
        {
            return BadRequest(new { message = "الخصم غير موجود." });
        }

        var student = await _db.StudentRecords.FirstOrDefaultAsync(s => s.Id == body.StudentId, ct);
        if (student is null)
        {
            return BadRequest(new { message = "الطالب غير موجود." });
        }

        var discountAmount = discount.Amount;
        var ps = student.PaidSchoolFees ?? 0;
        var pu = student.PaidUniformFees ?? 0;
        var pbb = student.PaidBooksFees ?? 0;
        var pb = student.PaidBusFees ?? 0;
        var paidCash = ps + pu + pbb + pb;
        var totalAmount = student.SchoolFees + student.UniformFees + student.BooksFees + student.BusFees;

        var app = new StudentDiscountApplicationRecord
        {
            Id = Guid.NewGuid(),
            StudentId = body.StudentId,
            DiscountId = body.DiscountId,
            DiscountName = discount.Name,
            Amount = discountAmount,
            AppliedAt = DateTimeOffset.UtcNow,
            Notes = body.Notes ?? $"تطبيق خصم {discount.Name} على رسوم المدرسة",
            CreatedBy = body.CreatedBy ?? "المدير"
        };
        _db.StudentDiscountApplications.Add(app);

        var totalDiscount = (await _db.StudentDiscountApplications.AsNoTracking()
            .Where(a => a.StudentId == body.StudentId)
            .SumAsync(a => (decimal?)a.Amount, ct) ?? 0) + discountAmount;
        var newRemaining = Math.Max(0, totalAmount - paidCash - totalDiscount);

        student.PaidSchoolFees = ps;
        student.PaidUniformFees = pu;
        student.PaidBooksFees = pbb;
        student.PaidBusFees = pb;
        student.PaidAmount = paidCash;
        student.RemainingAmount = newRemaining;
        student.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(new
        {
            discount = DiscountDto(discount),
            student,
            newPaidAmount = paidCash,
            newRemainingAmount = newRemaining
        });
    }

    [HttpGet("stats")]
    public async Task<ActionResult<object>> Stats([FromQuery] int year, CancellationToken ct)
    {
        var start = new DateTimeOffset(new DateTime(year, 1, 1), TimeSpan.Zero);
        var end = new DateTimeOffset(new DateTime(year, 12, 31, 23, 59, 59), TimeSpan.Zero);
        var applications = await _db.StudentDiscountApplications.AsNoTracking()
            .Where(a => a.AppliedAt >= start && a.AppliedAt <= end)
            .ToListAsync(ct);

        var uniqueStudents = applications.Select(a => a.StudentId).Distinct().Count();
        var typeMap = new Dictionary<string, (int Count, decimal Total)>();
        decimal totalAmount = 0;
        foreach (var app in applications)
        {
            totalAmount += app.Amount;
            if (!typeMap.TryGetValue(app.DiscountName, out var stat))
            {
                stat = (0, 0);
            }

            stat.Count++;
            stat.Total += app.Amount;
            typeMap[app.DiscountName] = stat;
        }

        return Ok(new
        {
            totalStudentsWithDiscounts = uniqueStudents,
            totalDiscountAmount = totalAmount,
            discountTypes = typeMap.Select(kv => new
            {
                name = kv.Key,
                count = kv.Value.Count,
                totalAmount = kv.Value.Total
            })
        });
    }

    [HttpGet("students-with-discounts")]
    public async Task<ActionResult<IEnumerable<object>>> StudentsWithDiscounts([FromQuery] int year, CancellationToken ct)
    {
        var start = new DateTimeOffset(new DateTime(year, 1, 1), TimeSpan.Zero);
        var end = new DateTimeOffset(new DateTime(year, 12, 31, 23, 59, 59), TimeSpan.Zero);
        var applications = await _db.StudentDiscountApplications.AsNoTracking()
            .Where(a => a.AppliedAt >= start && a.AppliedAt <= end)
            .ToListAsync(ct);
        var studentIds = applications.Select(a => a.StudentId).Distinct().ToList();
        var students = await _db.StudentRecords.AsNoTracking()
            .Where(s => studentIds.Contains(s.Id))
            .ToListAsync(ct);

        var result = new List<object>();
        foreach (var student in students)
        {
            var discounts = applications.Where(a => a.StudentId == student.Id).Select(a => new
            {
                discount_name = a.DiscountName,
                amount = a.Amount,
                applied_at = a.AppliedAt,
                notes = a.Notes
            }).ToList();
            result.Add(new
            {
                id = student.Id,
                name = student.Name,
                level = student.Level,
                section = student.Section,
                phone = student.Phone,
                parent_phone = student.ParentPhone,
                discounts
            });
        }

        return Ok(result);
    }

    private static object DiscountDto(StudentDiscountRecord d) => new
    {
        id = d.Id,
        name = d.Name,
        amount = d.Amount,
        description = d.Description,
        is_active = d.IsActive,
        created_at = d.CreatedAt,
        updated_at = d.UpdatedAt
    };
}

public class UpsertDiscountRequest
{
    public string? Name { get; set; }
    public decimal? Amount { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class ApplyDiscountRequest
{
    public Guid StudentId { get; set; }
    public Guid DiscountId { get; set; }
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
}

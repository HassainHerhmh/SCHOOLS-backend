using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class StudentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public StudentsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StudentRecord>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _db.StudentRecords
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StudentRecord>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var student = await _db.StudentRecords.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (student is null)
        {
            return NotFound();
        }

        return Ok(student);
    }

    [HttpPost]
    public async Task<ActionResult<StudentRecord>> Create([FromBody] UpsertStudentRequest body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
        {
            return BadRequest(new { message = "الاسم مطلوب." });
        }

        var total = body.SchoolFees + body.UniformFees + body.BooksFees + body.BusFees;
        var paid = body.PaidAmount;
        var remaining = total - paid;

        var now = DateTimeOffset.UtcNow;
        var entity = new StudentRecord
        {
            Id = Guid.NewGuid(),
            Name = body.Name.Trim(),
            Phone = body.Phone,
            ParentPhone = body.ParentPhone,
            Email = body.Email,
            Level = body.Level ?? "",
            Section = body.Section ?? "",
            SchoolFees = body.SchoolFees,
            UniformFees = body.UniformFees,
            BooksFees = body.BooksFees,
            BusFees = body.BusFees,
            TotalAmount = total,
            PaidAmount = paid,
            RemainingAmount = remaining,
            PaidSchoolFees = null,
            PaidUniformFees = null,
            PaidBooksFees = null,
            PaidBusFees = null,
            Gender = string.IsNullOrWhiteSpace(body.Gender) ? null : body.Gender,
            Status = "active",
            BusSiteId = body.BusSiteId,
            BusSiteName = body.BusSiteName,
            BusDriverId = body.BusDriverId,
            BusDriverName = body.BusDriverName,
            BusLocationUrl = NormalizeLocationUrl(body.BusLocationUrl),
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.StudentRecords.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StudentRecord>> Update(Guid id, [FromBody] UpsertStudentRequest body, CancellationToken cancellationToken)
    {
        var entity = await _db.StudentRecords.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(body.Name))
        {
            return BadRequest(new { message = "الاسم مطلوب." });
        }

        var total = body.SchoolFees + body.UniformFees + body.BooksFees + body.BusFees;
        var paid = body.PaidAmount;
        var remaining = total - paid;

        entity.Name = body.Name.Trim();
        entity.Phone = body.Phone;
        entity.ParentPhone = body.ParentPhone;
        entity.Email = body.Email;
        entity.Level = body.Level ?? "";
        entity.Section = body.Section ?? "";
        entity.SchoolFees = body.SchoolFees;
        entity.UniformFees = body.UniformFees;
        entity.BooksFees = body.BooksFees;
        entity.BusFees = body.BusFees;
        entity.TotalAmount = total;
        entity.PaidAmount = paid;
        entity.RemainingAmount = remaining;
        entity.Gender = string.IsNullOrWhiteSpace(body.Gender) ? null : body.Gender;
        entity.BusSiteId = body.BusSiteId;
        entity.BusSiteName = body.BusSiteName;
        entity.BusDriverId = body.BusDriverId;
        entity.BusDriverName = body.BusDriverName;
        entity.BusLocationUrl = NormalizeLocationUrl(body.BusLocationUrl);
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpPut("{id:guid}/payment")]
    public async Task<ActionResult<StudentRecord>> UpdatePayment(Guid id, [FromBody] UpdateStudentPaymentRequest body, CancellationToken cancellationToken)
    {
        var entity = await _db.StudentRecords.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.PaidSchoolFees = body.PaidSchoolFees;
        entity.PaidUniformFees = body.PaidUniformFees;
        entity.PaidBooksFees = body.PaidBooksFees;
        entity.PaidBusFees = body.PaidBusFees;
        entity.PaidAmount = body.PaidAmount;
        entity.RemainingAmount = body.RemainingAmount;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.StudentRecords.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        _db.StudentRecords.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string? NormalizeLocationUrl(string? url)
    {
        var trimmed = url?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}

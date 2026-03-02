namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

public class DOService : IDOService
{
    private readonly AppDBContext _db;

    public DOService(AppDBContext db)
    {
        _db = db;
    }

    public async Task<List<DO>> GetAllAsync()
    {
        return await _db.DOs
            .Include(x => x.Details)
            .Where(x => !x.IsDelete)
            .ToListAsync();
    }

    public async Task<DO?> GetByIdAsync(string id)
    {
        return await _db.DOs
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.DoId == id && !x.IsDelete);
    }

    public async Task CreateAsync(DO dto, List<DODetail> details, string createdBy)
    {
        dto.DoId = Guid.NewGuid().ToString();
        dto.CreatedBy = createdBy;
        dto.CreatedAt = DateTime.UtcNow;
        dto.Status = "DRAFT";
        dto.IsDelete = false;

        foreach (var detail in details)
        {
            detail.DoDetailId = Guid.NewGuid().ToString();
            detail.DoId = dto.DoId;
        }

        _db.DOs.Add(dto);
        _db.DODetails.AddRange(details);

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var doData = await _db.DOs.FindAsync(id);

        if (doData == null || doData.IsDelete)
            throw new Exception("DO tidak ditemukan");

        doData.IsDelete = true;
        await _db.SaveChangesAsync();
    }
}
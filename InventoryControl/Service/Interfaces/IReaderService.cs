using InventoryControl.DTO;

namespace InventoryControl.Service.Interfaces;

public interface IReaderService
{
    Task<List<ReaderResponseDto>> GetAllAsync();
    Task<ReaderResponseDto> GetByIdAsync(string id);
    Task CreateAsync(ReaderDto dto, string createdBy);
    Task UpdateAsync(string id, ReaderDto dto, string updatedBy);
    Task DeleteAsync(string id);
}
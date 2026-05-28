using InventoryControl.DTO;

namespace InventoryControl.Service.Interfaces;

public interface ITransactionService
{
    Task<List<TransactionHistoryDto>> GetHistory(DateTime? fromDate, DateTime? toDate, string? txType, string? keyword);
    Task<TransactionHistoryDetailResponseDto?> GetDetail(string id);
    Task<byte[]> ExportExcel(DateTime? fromDate, DateTime? toDate, string? txType, string? keyword, string exportBy);
    Task<byte[]> ExportCsv(DateTime? fromDate, DateTime? toDate, string? txType, string? keyword, string exportBy);
}

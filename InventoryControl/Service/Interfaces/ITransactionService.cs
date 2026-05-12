using InventoryControl.DTO;

namespace InventoryControl.Service.Interfaces;

public interface ITransactionService
{
    Task<List<TransactionHistoryDto>> GetHistory(DateTime? fromDate, DateTime? toDate, string? txType, string? keyword);
    Task<byte[]> ExportExcel(DateTime? fromDate, DateTime? toDate, string? txType, string? keyword);
    Task<byte[]> ExportCsv(DateTime? fromDate, DateTime? toDate, string? txType, string? keyword);
}

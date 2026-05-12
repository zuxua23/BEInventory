namespace InventoryControl.Service.Interfaces;

using InventoryControl.DTO;

public interface ILogService
{
    List<ActivityLogDto>
        GetRecentActivities(
            int take = 10
        );
}
namespace InventoryControl.Service.Interfaces;

using InventoryControl.DTO;
using InventoryControl.Entity;

public interface IPermissionService
{
        Task Create(RoleRequestDto dto, string user);
        Task Update(string id, RoleRequestDto dto, string user);
        Task<RoleResponseDto> GetById(string id);
        Task<object> GetModules();
        Task<List<Role>> GetAll();
        Task Delete(string id);

}
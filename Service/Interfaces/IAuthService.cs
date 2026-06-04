using InventoryControl.DTO;
using InventoryControl.Entity;

namespace InventoryControl.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResultDto> ValidateUserAsync(LoginDTO dto);
        Task<string> GenerateTokenAsync(LoginResultDto user);
    }

}

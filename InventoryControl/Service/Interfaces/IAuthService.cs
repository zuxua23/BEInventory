using InventoryControl.DTO;
using InventoryControl.Entity;

namespace InventoryControl.Services.Interfaces
{
    public interface IAuthService
    {
        Task<string> LoginAsync(LoginDTO dto);
        Task<User> LoginWebAsync(LoginDTO dto);
        Task LogoutAsync(string userId);
    }

}

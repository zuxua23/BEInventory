
namespace InventoryControl.Utility;


public class UuidGen
{
    public static string GenerateUuid(string format = "D")
    {
        return Guid.NewGuid().ToString(format);
    }
}
namespace InventoryControl.Utility;

public class AppRestartService
{
    private readonly IHostApplicationLifetime _lifetime;

    public AppRestartService(IHostApplicationLifetime lifetime)
    {
        _lifetime = lifetime;
    }

    public void Restart()
    {
        Task.Run(() =>
        {
            Thread.Sleep(1000); 
            _lifetime.StopApplication();
        });
    }
}
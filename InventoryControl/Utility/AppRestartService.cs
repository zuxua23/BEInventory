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
        SystemLogger.Warn(
        "Application restart requested."
        );
        Task.Run(() =>
        {
            Thread.Sleep(1000);
            SystemLogger.Warn(
             "Application shutdown initiated."
            );
            _lifetime.StopApplication();
        });
    }
}
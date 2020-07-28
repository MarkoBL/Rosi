namespace Rosi.Core
{
    public interface ILogger
    {
        string Logname => GetType().Name;
    }
}

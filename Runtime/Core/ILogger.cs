namespace Rosi.Runtime.Core
{
    public interface ILogger
    {
        string Logname => GetType().Name;
    }
}

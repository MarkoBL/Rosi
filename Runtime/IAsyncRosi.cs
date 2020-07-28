using System.Threading.Tasks;

public interface IAsyncRosi
{
    Task<int> Run(IRuntime runtime);
}

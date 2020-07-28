class Script : IRosi
{
    public int Run(IRuntime runtime)
    {
        System.Console.WriteLine("Hello World!");
        return 0;
    }

    static async System.Threading.Tasks.Task<int> Main()
    {
        // Set main script manually. Only required, if you set any options in the script or if you need to access the script root directory.
        return await new Rosi.Runtime(typeof(Script), "../../../Script.cs").RunAsync();
    }
}

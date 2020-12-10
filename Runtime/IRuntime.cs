using System;
using Rosi.Core;
using Rosi.Scriban;

public interface IRuntime
{
    Config Config { get; }
    ScribanRuntime Scriban { get; }

    T GetValue<T>(string name);
    bool TryGetValue<T>(string name, out T value);
    void SetValue(string name, object value);

}
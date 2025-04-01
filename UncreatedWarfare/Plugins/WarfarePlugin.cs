using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Uncreated.Warfare.Plugins;
public class WarfarePlugin
{
    public AssemblyName AssemblyName { get; }
    public string AssemblyLocation { get; }
    public Assembly LoadedAssembly { get; }
    public IConfigurationRoot? Configuration { get; internal set; }
    public WarfarePlugin(AssemblyName assemblyName, string assemblyLocation, Assembly loadedAssembly)
    {
        AssemblyName = assemblyName;
        AssemblyLocation = assemblyLocation;
        LoadedAssembly = loadedAssembly;
    }
}
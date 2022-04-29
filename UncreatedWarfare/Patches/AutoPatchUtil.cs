using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare;
internal class AutoPatchUtil
{
    internal Harmony Harmony;
    internal void PatchAll()
    {
        Harmony = new Harmony("net.uncreated.warfare");
        Type[] array = Assembly.GetCallingAssembly().GetTypes();
        for (int i = 0; i < array.Length; i++)
        {
            Type type = array[i];
            MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            for (int m = 0; m < methods.Length; m++)
            {
                UCPatchAttribute? attr = methods[m].GetCustomAttribute<UCPatchAttribute>();
                if (attr != null)
                {
                    MethodInfo method = methods[m];

                    L.LogDebug("Patched " + attr.DisplayName);
                }
            }
        }
    }
    internal void UnpatchAll()
    {

    }
}

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
internal sealed class UCPatchAttribute : Attribute
{
    private readonly string displayName;

    public UCPatchAttribute(string displayName)
    {
        this.displayName = displayName;
    }

    public string DisplayName => displayName;
}
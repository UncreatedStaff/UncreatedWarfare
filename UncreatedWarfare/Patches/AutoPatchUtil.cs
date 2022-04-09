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
        Type[] array = Assembly.GetExecutingAssembly().GetTypes();
        for (int i = 0; i < array.Length; i++)
        {
            Type type = array[i];
            
        }
    }
    internal void UnpatchAll()
    {

    }
}

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
internal sealed class UCPatchAttribute : Attribute
{
    public UCPatchAttribute() { }
}
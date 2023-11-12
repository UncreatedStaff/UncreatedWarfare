using System;
using System.Reflection;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Singletons;

public abstract class ConfigSingleton<TConfig, TData> : BaseReloadSingleton where TConfig : Config<TData> where TData : JSONConfigData, new()
{
    private readonly string? _folder;
    private readonly string? _file;
    private TConfig _config;
    public TConfig ConfigurationFile => _config;
    public TData Config => _config.Data;
    protected ConfigSingleton(string reloadKey) : base(reloadKey) { }
    protected ConfigSingleton(string? reloadKey, string folder, string fileName) : base(reloadKey)
    {
        _folder = folder;
        _file = fileName;
    }
    protected ConfigSingleton(string folder, string fileName) : this(null, folder, fileName)
    {
        _folder = folder;
        _file = fileName;
    }
    private void CreateConfig()
    {
        ConstructorInfo[] ctors = typeof(TConfig).GetConstructors();
        bool hasEmpty = false;
        bool hasDefault = true;
        for (int i = 0; i < ctors.Length; ++i)
        {
            ParameterInfo[] ctorParams = ctors[i].GetParameters();
            if (ctorParams.Length == 0)
            {
                hasEmpty = true;
                break;
            }
            else if (ctorParams.Length == 2 && ctorParams[0].ParameterType == typeof(string) && ctorParams[1].ParameterType == typeof(string))
                hasDefault = true;
        }

        if (hasEmpty)
        {
            _config = (TConfig)Activator.CreateInstance(typeof(TConfig), Array.Empty<object>());
        }
        else if (hasDefault)
        {
            if (_folder is null || _file is null)
                throw new InvalidOperationException(typeof(TConfig).Name + " with " + typeof(TData).Name + " must have either an empty constructor or the ConfigSingleton constructor with folder and fileName should be used.");
            _config = (TConfig)Activator.CreateInstance(typeof(TConfig), new object[] { _folder, _file });
        }
        if (_config is null)
            throw new InvalidOperationException(typeof(TConfig).Name + " with " + typeof(TData).Name + " must have either an empty constructor or a " +
                "(string folder, string fileName) constuctor and the ConfigSingleton constructor with folder and fileName should be used.");
        if (ReloadKey is not null)
            ReloadCommand.DeregisterConfigForReload(ReloadKey);
    }
    public override void Load()
    {
        CreateConfig();
    }
    public override void Reload()
    {
        CreateConfig();
    }
    public override void Unload()
    {
        _config = null!;
    }
}
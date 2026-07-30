namespace OmniCore.Shared.Infrastructure.Configs;

public abstract class ConfigBase
{
    /// <summary>
    /// Section name in appsettings.json. Defaults to class name without "Config" suffix.
    /// </summary>
    public virtual string SectionName => GetType().Name.Replace("Config", string.Empty);
}
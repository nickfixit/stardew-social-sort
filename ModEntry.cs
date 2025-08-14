using System;

// Real SMAPI interface definitions based on source code
namespace StardewModdingAPI
{
    public enum LogLevel { Trace, Debug, Info, Warn, Error }
    
    public interface IModHelper 
    {
        // Core helper interface - implementation handled by SMAPI
    }
    
    public interface IMonitor
    {
        void Log(string message, LogLevel level = LogLevel.Debug);
    }
    
    public interface IManifest
    {
        // Manifest interface - implementation handled by SMAPI
    }
    
    public interface IMod : IDisposable
    {
        IModHelper Helper { get; }
        IMonitor Monitor { get; }
        IManifest ModManifest { get; }
        void Entry(IModHelper helper);
        object? GetApi();
        object? GetApi(IModInfo mod);
    }
    
    public interface IModInfo
    {
        // ModInfo interface - implementation handled by SMAPI
    }

    public abstract class Mod : IMod, IDisposable
    {
        public IModHelper Helper { get; protected set; } = null!;
        public IMonitor Monitor { get; protected set; } = null!;
        public IManifest ModManifest { get; protected set; } = null!;
        
        public abstract void Entry(IModHelper helper);
        
        public virtual object? GetApi() => null;
        public virtual object? GetApi(IModInfo mod) => null;
        
        public virtual void Dispose() { }
    }
}

namespace CustomSocialSort
{
    public class ModEntry : StardewModdingAPI.Mod
    {
        public override void Entry(StardewModdingAPI.IModHelper helper)
        {
            Helper = helper;
            Monitor.Log("Custom Social Sort loaded successfully!", StardewModdingAPI.LogLevel.Info);
        }
    }
}
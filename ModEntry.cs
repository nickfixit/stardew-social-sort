using System;

// Minimal internal definitions for SMAPI compatibility
namespace StardewModdingAPI
{
    public enum LogLevel { Trace, Debug, Info, Warn, Error }
    
    public interface IModHelper { }
    
    public interface IMonitor
    {
        void Log(string message, LogLevel level = LogLevel.Debug);
    }

    public abstract class Mod
    {
        protected IModHelper Helper = null!;
        protected IMonitor Monitor = null!;
        public abstract void Entry(IModHelper helper);
    }
}

namespace CustomSocialSort
{
    public class ModEntry : StardewModdingAPI.Mod
    {
        public override void Entry(StardewModdingAPI.IModHelper helper)
        {
            Monitor.Log("Custom Social Sort loaded successfully!", StardewModdingAPI.LogLevel.Info);
        }
    }
}
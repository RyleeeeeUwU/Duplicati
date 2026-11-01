using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace Duplicati.Configuration
{
    /// <summary>
    /// Stored by BSIPA at UserData/Duplicati.json. Properties must be virtual for the generated store.
    /// </summary>
    internal class PluginConfig
    {
        /// <summary>Log what would be removed without touching any files.</summary>
        public virtual bool DryRun { get; set; } = false;

        /// <summary>
        /// false (default): duplicates are moved to UserData/Duplicati/Removed so they can be restored.
        /// true: duplicates are deleted permanently.
        /// </summary>
        public virtual bool PermanentlyDelete { get; set; } = false;
    }
}

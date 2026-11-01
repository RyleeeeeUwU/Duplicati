namespace Duplicati.Core
{
    /// <summary>
    /// Minimal logging abstraction so the scanning logic has no dependency on BSIPA.
    /// </summary>
    public interface ILog
    {
        void Info(string message);
        void Warn(string message);
    }
}

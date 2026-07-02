namespace JMAPI.Interfaces
{
    /// <summary>
    /// Tracks which database (live or test) the app is currently serving
    /// requests from. Admin-only, global for all users - see
    /// Controllers/DatabaseController.cs.
    /// </summary>
    public interface IActiveDatabaseProvider
    {
        bool IsTestMode { get; }
        void SetTestMode(bool isTestMode);
    }
}

using JMAPI.Interfaces;

namespace JMAPI.Services
{
    /// <summary>
    /// In-memory, file-persisted flag for whether the app is currently
    /// pointed at the live or test database. A singleton so every request
    /// (across all users) sees the same mode - switching is global and
    /// takes effect on the very next request, no restart needed. Persisted
    /// to a small file so the mode survives a container restart.
    /// </summary>
    public class ActiveDatabaseProvider : IActiveDatabaseProvider
    {
        private readonly string _stateFilePath;
        private readonly object _lock = new();
        private volatile bool _isTestMode;

        public ActiveDatabaseProvider(IConfiguration config, IHostEnvironment env)
        {
            var stateDirectory = config["State:Directory"] ?? env.ContentRootPath;
            Directory.CreateDirectory(stateDirectory);
            _stateFilePath = Path.Combine(stateDirectory, "active-database.txt");

            _isTestMode = File.Exists(_stateFilePath) &&
                File.ReadAllText(_stateFilePath).Trim().Equals("test", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsTestMode => _isTestMode;

        public void SetTestMode(bool isTestMode)
        {
            lock (_lock)
            {
                _isTestMode = isTestMode;
                File.WriteAllText(_stateFilePath, isTestMode ? "test" : "live");
            }
        }
    }
}

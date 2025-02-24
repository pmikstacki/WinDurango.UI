namespace WinDurango.UI.Settings
{
    public interface IConfig
    {
        void Reset();
        void Backup();
        void Generate();
        void Save();
        void Set(string setting, object value);
    }
}
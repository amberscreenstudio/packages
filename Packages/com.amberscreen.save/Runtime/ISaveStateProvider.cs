// Pluggable save slice. Games register one provider per stable key.
namespace AmberScreen.Save
{
    public interface ISaveStateProvider
    {
        string Key { get; }
        object Capture();
        void Restore(object state);
    }
}

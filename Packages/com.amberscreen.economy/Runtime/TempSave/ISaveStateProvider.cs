namespace AmberScreen.Economy.TempSave
{
    // replace with com.amberscreen.save
    /// <summary>
    /// Capture/Restore a keyed blob. Temporary home in Economy until the shared save package lands.
    /// Lives in <c>AmberScreen.Economy.TempSave</c> so it cannot clash with <c>AmberScreen.Save</c>.
    /// </summary>
    public interface ISaveStateProvider
    {
        string Key { get; }
        string Capture();
        void Restore(string payload);
    }
}

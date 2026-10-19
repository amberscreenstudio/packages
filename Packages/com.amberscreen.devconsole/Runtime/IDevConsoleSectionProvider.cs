namespace AmberScreen.DevConsole
{
    /// <summary>
    /// Discovered once and invoked to register console entries.
    /// </summary>
    public interface IDevConsoleSectionProvider
    {
        void Register();
    }
}

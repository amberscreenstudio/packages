namespace AmberScreen.PhoneUI
{
    /// <summary>
    /// Messenger-style ticks for player messages.
    /// Sent = one grey, Delivered = two grey, Read = two blue.
    /// Hosts own per-recipient upgrade rules (who counts for Delivered / Read).
    /// </summary>
    public enum MessageReceipt
    {
        None = 0,
        Sent = 1,
        Delivered = 2,
        Read = 3
    }
}

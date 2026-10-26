using System.Collections.Generic;

namespace AmberScreen.PhoneUI
{
    public interface IContactDirectory
    {
        IReadOnlyList<ContactInfo> Contacts { get; }
        ContactInfo GetContact(string id);
        ContactInfo Player { get; }
    }
}

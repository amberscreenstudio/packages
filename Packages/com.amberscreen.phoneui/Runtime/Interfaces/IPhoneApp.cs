using UnityEngine;

namespace AmberScreen.PhoneUI
{
    public interface IPhoneApp
    {
        string Id { get; }
        string DisplayName { get; }
        Sprite Icon { get; }
        void Open(RectTransform host);
        void Close();
    }
}

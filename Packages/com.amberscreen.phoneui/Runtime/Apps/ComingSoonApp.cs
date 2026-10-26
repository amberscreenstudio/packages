using TMPro;
using UnityEngine;

namespace AmberScreen.PhoneUI
{
    public sealed class ComingSoonApp : IPhoneApp
    {
        readonly string _id;
        readonly string _name;
        readonly Sprite _icon;
        RectTransform _view;

        public ComingSoonApp(string id, string name, Sprite icon = null)
        {
            _id = id;
            _name = name;
            _icon = icon;
        }

        public string Id => _id;
        public string DisplayName => _name;
        public Sprite Icon => _icon;

        public void Open(RectTransform host)
        {
            Close();
            _view = UiFactory.Panel("ComingSoon_" + _id, host, PhoneUITheme.Active.ComingSoonBg);
            var label = UiFactory.Label("Msg", _view, _name + "\nComing soon", PhoneDp.TitleText, FontStyles.Bold);
            label.alignment = TextAlignmentOptions.Center;
            UiFactory.Stretch(label.rectTransform);
        }

        public void Close()
        {
            if (_view != null)
            {
                UiFactory.SafeDestroy(_view.gameObject);
                _view = null;
            }
        }
    }
}

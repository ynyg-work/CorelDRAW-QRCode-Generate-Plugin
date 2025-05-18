using System;
using System.Globalization;
using System.Resources;
using System.Windows.Markup;

namespace QRCodeGenerator
{
    public class LocExtension : MarkupExtension
    {
        private static ResourceManager _resMgr = Resources.Strings.ResourceManager;

        // 对应 .resx 里的 Name
        public string Key { get; set; }

        public LocExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var culture = CultureInfo.CurrentUICulture;
            var text = _resMgr.GetString(Key, culture);
            return text ?? $"!{Key}!";
        }
    }
}
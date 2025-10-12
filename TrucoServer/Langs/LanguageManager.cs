using System.Globalization;
using System.Threading;

namespace TrucoServer.Langs
{
    public static class LanguageManager
    {
        public static void SetLanguage(string languageCode)
        {
            var culture = new CultureInfo(languageCode);
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
        }
    }
}

using System.Text.RegularExpressions;

namespace BlackNight.MailDaemon.Core
{
    public static class Extensions
    {
        public static bool ValidateEmail(this string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            var isNumber = new Regex(@"[\w\d-\.]+@([\w\d-]+(\.[\w\-]+)+)");
            var m = isNumber.Match(email);

            return m.Success;
        }
    }
}

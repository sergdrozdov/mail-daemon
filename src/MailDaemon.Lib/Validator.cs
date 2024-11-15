using System.Text.RegularExpressions;

namespace MailDaemon.Lib
{
    public class Validator : IValidator
    {
        public bool IsMailAddressValid(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            var pattern = new Regex(@"[\w\d-\.]+@([\w\d-]+(\.[\w\-]+)+)");
            var m = pattern.Match(email);

            return m.Success;
        }
    }
}

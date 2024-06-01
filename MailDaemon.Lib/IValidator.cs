namespace MailDaemon.Lib
{
    public interface IValidator
    {
        bool IsMailAddressValid(string email);
    }
}

namespace ECommerceMVC.Helpers
{
    public interface IEmailService
    {
        void SendEmail(string to, string subject, string body);
    }

}

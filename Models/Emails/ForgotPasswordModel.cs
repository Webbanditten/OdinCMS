namespace KeplerCMS.Models.Emails
{
    public class ForgotPasswordModel : GenericEmailModel
    {
        public string ResetPassword { get; set; }
        public string ResetPasswordUri { get; set; }
    }
}

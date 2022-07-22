using System;

namespace KeplerCMS.Models
{
    public class ResetPasswordViewModel
    {
        public bool Valid { get; set; }

        public string Code { get; set; }
    }
}

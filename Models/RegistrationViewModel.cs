using System;

namespace KeplerCMS.Models
{
    public class RegistrationViewModel
    {
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string FigureData { get; set; }

        public string Gender { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string AcceptTOS { get; set; }
    }
}

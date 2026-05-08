using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Areas.Housekeeping.Models.Views
{
    public class PollCreateViewModel
    {
        [Required]
        public string Headline { get; set; }

        [Required]
        public string Description { get; set; }

        public string ThankYou { get; set; }

        public List<PollQuestionViewModel> Questions { get; set; } = new List<PollQuestionViewModel>();

        // Trigger settings
        public int? TriggerRoomId { get; set; }
        public int? TriggerTimeFrom { get; set; }
        public int? TriggerTimeTo { get; set; }
    }

    public class PollQuestionViewModel
    {
        [Required]
        public string Text { get; set; }

        public PollQuestionTypes Type { get; set; } = PollQuestionTypes.CHOICE;

        public int MinSelect { get; set; } = 1;
        public int MaxSelect { get; set; } = 1;
        public int Order { get; set; }

        public List<string> Options { get; set; } = new List<string>();
    }

    public class PollResultsViewModel
    {
        public Poll Poll { get; set; }
        public List<QuestionResultViewModel> QuestionResults { get; set; } = new List<QuestionResultViewModel>();
        public int TotalRespondents { get; set; }
        public int AcceptedOffers { get; set; }
        public int RejectedOffers { get; set; }
    }

    public class QuestionResultViewModel
    {
        public Question Question { get; set; }
        public List<OptionResultViewModel> OptionResults { get; set; } = new List<OptionResultViewModel>();
        public List<string> TextAnswers { get; set; } = new List<string>();
        public int TotalAnswers { get; set; }
    }

    public class OptionResultViewModel
    {
        public QuestionOption Option { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
}

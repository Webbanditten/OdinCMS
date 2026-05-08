using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    public enum PollQuestionTypes
    {
        CHOICE,
        MULTI_CHOICE,
        TEXT
    }

    [Table("polls")]
    public class Poll
    {
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("headline")]
        public string Headline { get; set; }

        [Required]
        [Column("description")]
        public string Description { get; set; }

        [Column("thank_you")]
        public string ThankYou { get; set; }

        [Column("enabled")]
        public bool Enabled { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public List<Question> Questions { get; set; }
        public List<PollTrigger> Triggers { get; set; }
    }

    [Table("polls_answers")]
    public class Answer
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("poll_question_id")]
        public int QuestionId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("poll_id")]
        public int PollId { get; set; }

        [Column("value")]
        public string Value { get; set; }

        // Navigation
        [ForeignKey("QuestionId")]
        public Question Question { get; set; }
    }

    [Table("polls_questions")]
    public class Question
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("poll_id")]
        public int PollId { get; set; }

        [Column("type")]
        public PollQuestionTypes Type { get; set; }

        [Required]
        [Column("text")]
        public string Text { get; set; }

        [Column("min_select")]
        public int MinSelect { get; set; }

        [Column("max_select")]
        public int MaxSelect { get; set; } = 1;

        [Column("order")]
        public int Order { get; set; }

        // Navigation
        [ForeignKey("PollId")]
        public Poll Poll { get; set; }
        public List<QuestionOption> Options { get; set; }
        public List<Answer> Answers { get; set; }
    }

    [Table("polls_questions_options")]
    public class QuestionOption
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("poll_question_id")]
        public int QuestionId { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; }

        [Column("order")]
        public int Order { get; set; }

        // Navigation
        [ForeignKey("QuestionId")]
        public Question Question { get; set; }
    }

    [Table("polls_triggers")]
    public class PollTrigger
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("poll_id")]
        public int PollId { get; set; }

        [Column("room")]
        public int? Room { get; set; }

        [Column("time_from")]
        public int? TimeFrom { get; set; }

        [Column("time_to")]
        public int? TimeTo { get; set; }

        // Navigation
        [ForeignKey("PollId")]
        public Poll Poll { get; set; }
    }

    [Table("polls_offers")]
    public class PollOffer
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("poll_id")]
        public int PollId { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    public enum PollQuestionTypes {
        TEXT,
        CHOICE
    }

    [Table("polls")]
    public class Poll
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("headline")]
        public string Headline { get; set; }
        [Column("description")]
        public string Description { get; set; }
        [Column("thank_you")]
        public string ThankYou { get; set; }
    }

    [Table("polls_answers")]
    public class Answer
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("poll_question_id")]
        public int QuestionId { get; set; }
        [Column("value")]
        public string Value { get; set; }
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

        [Column("text")]
        public string Text { get; set; }
        [Column("min_select")]
        public string MinSelect { get; set; }
        [Column("max_select")]
        public string MaxSelect { get; set; }

    }
     [Table("polls_questions_options")]
    public class QuestionOption
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("poll_question_id")]
        public int QuestionId { get; set; }
        [Column("name")]
        public string Name { get; set; }
    }

     [Table("polls_triggers")]
    public class Trigger
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("poll_id")]
        public int PollId { get; set; }
        [Column("room")]
        public int Room { get; set; }
        [Column("time_from")]
        public int TimeFrom { get; set; }
        [Column("time_to")]
        public int TimeTo { get; set; }
    }
}

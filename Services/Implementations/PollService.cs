using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Services.Implementations
{
    public class PollService : IPollService
    {
        private readonly DataContext _context;

        public PollService(DataContext context)
        {
            _context = context;
        }

        public async Task<List<Poll>> GetAll()
        {
            return await _context.Polls
                .Include(p => p.Questions)
                .Include(p => p.Triggers)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Poll> Get(int id)
        {
            return await _context.Polls
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Options)
                .Include(p => p.Triggers)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Poll> Create(PollCreateViewModel model)
        {
            var poll = new Poll
            {
                Headline = model.Headline,
                Description = model.Description,
                ThankYou = model.ThankYou ?? "",
                Enabled = true,
                Questions = new List<Question>()
            };

            if (model.Questions != null)
            {
                for (int i = 0; i < model.Questions.Count; i++)
                {
                    var qvm = model.Questions[i];
                    var question = new Question
                    {
                        Text = qvm.Text,
                        Type = qvm.Type,
                        MinSelect = qvm.MinSelect,
                        MaxSelect = qvm.MaxSelect,
                        Order = i,
                        Options = new List<QuestionOption>()
                    };

                    if (qvm.Type != PollQuestionTypes.TEXT && qvm.Options != null)
                    {
                        for (int j = 0; j < qvm.Options.Count; j++)
                        {
                            if (!string.IsNullOrWhiteSpace(qvm.Options[j]))
                            {
                                question.Options.Add(new QuestionOption
                                {
                                    Name = qvm.Options[j],
                                    Order = j
                                });
                            }
                        }
                    }

                    poll.Questions.Add(question);
                }
            }

            await _context.Polls.AddAsync(poll);
            await _context.SaveChangesAsync();

            // Create trigger if specified
            if (model.TriggerRoomId.HasValue || model.TriggerTimeFrom.HasValue || model.TriggerTimeTo.HasValue)
            {
                var trigger = new PollTrigger
                {
                    PollId = poll.Id,
                    Room = model.TriggerRoomId,
                    TimeFrom = model.TriggerTimeFrom,
                    TimeTo = model.TriggerTimeTo
                };
                await _context.PollsTriggers.AddAsync(trigger);
                await _context.SaveChangesAsync();
            }

            return poll;
        }

        public async Task<Poll> Update(int id, PollCreateViewModel model)
        {
            var poll = await _context.Polls
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Options)
                .Include(p => p.Triggers)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (poll == null) return null;

            poll.Headline = model.Headline;
            poll.Description = model.Description;
            poll.ThankYou = model.ThankYou ?? "";

            // Remove existing questions and options (cascade will handle options)
            _context.PollsQuestions.RemoveRange(poll.Questions);

            // Re-add questions from the model
            poll.Questions = new List<Question>();
            if (model.Questions != null)
            {
                for (int i = 0; i < model.Questions.Count; i++)
                {
                    var qvm = model.Questions[i];
                    var question = new Question
                    {
                        PollId = poll.Id,
                        Text = qvm.Text,
                        Type = qvm.Type,
                        MinSelect = qvm.MinSelect,
                        MaxSelect = qvm.MaxSelect,
                        Order = i,
                        Options = new List<QuestionOption>()
                    };

                    if (qvm.Type != PollQuestionTypes.TEXT && qvm.Options != null)
                    {
                        for (int j = 0; j < qvm.Options.Count; j++)
                        {
                            if (!string.IsNullOrWhiteSpace(qvm.Options[j]))
                            {
                                question.Options.Add(new QuestionOption
                                {
                                    Name = qvm.Options[j],
                                    Order = j
                                });
                            }
                        }
                    }

                    poll.Questions.Add(question);
                }
            }

            // Update trigger
            _context.PollsTriggers.RemoveRange(poll.Triggers);
            if (model.TriggerRoomId.HasValue || model.TriggerTimeFrom.HasValue || model.TriggerTimeTo.HasValue)
            {
                poll.Triggers = new List<PollTrigger>
                {
                    new PollTrigger
                    {
                        PollId = poll.Id,
                        Room = model.TriggerRoomId,
                        TimeFrom = model.TriggerTimeFrom,
                        TimeTo = model.TriggerTimeTo
                    }
                };
            }

            await _context.SaveChangesAsync();
            return poll;
        }

        public async Task<bool> Remove(int id)
        {
            var poll = await _context.Polls.FindAsync(id);
            if (poll == null) return false;

            _context.Polls.Remove(poll);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleEnabled(int id)
        {
            var poll = await _context.Polls.FindAsync(id);
            if (poll == null) return false;

            poll.Enabled = !poll.Enabled;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PollResultsViewModel> GetResults(int pollId)
        {
            var poll = await _context.Polls
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Options)
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(p => p.Id == pollId);

            if (poll == null) return null;

            var offers = await _context.PollsOffers
                .Where(o => o.PollId == pollId)
                .ToListAsync();

            var result = new PollResultsViewModel
            {
                Poll = poll,
                TotalRespondents = offers.Count(o => o.Status == "ACCEPTED"),
                AcceptedOffers = offers.Count(o => o.Status == "ACCEPTED"),
                RejectedOffers = offers.Count(o => o.Status == "REJECTED"),
                QuestionResults = new List<QuestionResultViewModel>()
            };

            foreach (var question in poll.Questions.OrderBy(q => q.Order))
            {
                var qResult = new QuestionResultViewModel
                {
                    Question = question,
                    TotalAnswers = question.Answers?.Count ?? 0
                };

                if (question.Type == PollQuestionTypes.TEXT)
                {
                    qResult.TextAnswers = question.Answers?
                        .Select(a => a.Value)
                        .ToList() ?? new List<string>();
                }
                else
                {
                    // For choice questions, count votes per option
                    var answerValues = question.Answers?.Select(a => a.Value).ToList() ?? new List<string>();

                    foreach (var option in question.Options.OrderBy(o => o.Order))
                    {
                        var count = answerValues.Count(v => v == option.Id.ToString());
                        qResult.OptionResults.Add(new OptionResultViewModel
                        {
                            Option = option,
                            Count = count,
                            Percentage = qResult.TotalAnswers > 0
                                ? (double)count / qResult.TotalAnswers * 100
                                : 0
                        });
                    }
                }

                result.QuestionResults.Add(qResult);
            }

            return result;
        }
    }
}

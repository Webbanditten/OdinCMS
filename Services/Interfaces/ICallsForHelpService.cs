using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Services.Interfaces
{
    public interface ICallsForHelpService
    {
        Task<IEnumerable<CallsForHelp>> GetByDate(DateTime date);
        Task<IEnumerable<CallsForHelp>> GetPending();
    }
}

using System.Threading.Tasks;

namespace KeplerCMS.Services.CampaignActions
{
    public interface ICampaignActionHandler
    {
        /// <summary>
        /// Takes a snapshot of the current state for the given action data.
        /// Returns JSON representing the current state that can be restored later.
        /// </summary>
        Task<string> TakeSnapshot(string actionData);

        /// <summary>
        /// Applies the campaign action (makes the change).
        /// </summary>
        Task Apply(string actionData);

        /// <summary>
        /// Reverts the campaign action using the stored snapshot data.
        /// </summary>
        Task Revert(string snapshotData);

        /// <summary>
        /// Returns a unique key identifying what setting/resource this action targets.
        /// Used for conflict detection between overlapping campaigns.
        /// </summary>
        string GetConflictKey(string actionData);
    }
}

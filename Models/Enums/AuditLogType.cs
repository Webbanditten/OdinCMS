using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
namespace KeplerCMS.Models
{
    public enum AuditLogType
    {
        create_rank,
        edit_rank,
        delete_rank,
        give_badge,
        remove_badge,
        create_news,
        edit_news,
        delete_news,
        send_messenger_campaign,
        alert_user,
        ban_user,
        unban_user,
        kick_user,
        file_edit,
        file_deleted,
        file_upload
    }
}

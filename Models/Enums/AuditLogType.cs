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
        file_upload,
        create_poll,
        edit_poll,
        delete_poll,
        create_voucher,
        edit_voucher,
        delete_voucher,
        take_room_photo,
        save_promo_picture,
        trade_ban,
        trade_unban,
        create_ad,
        edit_ad,
        delete_ad,
        edit_public_rooms,
        edit_room_categories,
        help_ticket_reply,
        help_ticket_close,
        website_report_close,
        edit_user,
        give_credits,
        give_club,
        give_furni,
        user_note,
        password_reset_mail,
        mass_ban,
        mass_unban,
        mass_alert,
        edit_group,
        delete_group,
        delete_tag,
        delete_stickie
    }
}

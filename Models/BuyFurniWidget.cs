using System.Text.RegularExpressions;

namespace KeplerCMS.Models
{
    /// <summary>
    /// A furni for sale placed in page or news text: {buy_furni_123} (123 = catalogue_items.id). It shows the furni, its
    /// price and a buy button (one click, site.js .buy-btn → /furnipurchase/purchase_confirmation). The editors insert it
    /// with the "Buy furni" button (wwwroot/housekeeping/js/editor-shop.js). What players can't buy in the game's catalogue
    /// shows without a button, and the purchase checks it again (FurniShopService).
    /// </summary>
    public static class BuyFurniWidget
    {
        public static readonly Regex Code = new Regex(@"\{buy_furni_(\d{1,9})\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }
}

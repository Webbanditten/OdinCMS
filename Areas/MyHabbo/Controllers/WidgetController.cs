using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using System.Threading.Tasks;
using KeplerCMS.Services.Interfaces;

namespace KeplerCMS.Areas.MyHabbo
{
    [Area("MyHabbo")]
    public class WidgetController : Controller
    {
        public readonly IHomeService _homeService;
        public WidgetController(IHomeService homeService)
        {
            _homeService = homeService;
        }


        [HttpPost]
        [LoggedInFilter(false)]
        public async Task<IActionResult> Edit(int skinId, int widgetId)
        {
            var userId = int.Parse(User.Identity.Name);
            var updatedItem = await _homeService.EditItem(widgetId, skinId, userId);

            Response.Headers.Add("x-json", "{\"id\":\"" + updatedItem.Item.Id + "\",\"cssClass\":\"" + updatedItem.Item.Skin + "\",\"type\":\"widget\"}");
            return View("~/Areas/MyHabbo/Views/Items/Widget.cshtml", updatedItem);
        }
    }
    /////////////////// ADD 
    // widgetId: 31263, privileged: true, zindex: 240


    // Response er det samme som edit 










    // edit
    /*
skinId: 3
widgetId: 31253

     */
    /*









<div class="movable widget BadgesWidget" id="widget-31263" style="left: 269px; top: 380px; z-index: 214">
<div class="w_skin_metalskin">
   <div class="widget-corner" id="widget-31263-handle">
	   <div class="widget-headline"><h3>

<img src="https://cdn.classichabbo.com/web-gallery/images/myhabbo/icon_edit.gif" width="19" height="18" class="edit-button" id="widget-31263-edit" />
<script language="JavaScript" type="text/javascript">
Event.observe("widget-31263-edit", "click", function(e) { openEditMenu(e, 31263, "widget", "widget-31263-edit"); }, false);
</script>

	   <span class="header-left">&nbsp;</span><span class="header-middle">Badges & Achievements</span><span class="header-right">&nbsp;</span></h3>
	   </div>	
   </div>
   <div class="widget-body">
	   <div class="widget-content">
		   <div id="badgelist-content">	
	   <ul class="clearfix">

		   <li style="background-image: url(https://cdn.classichabbo.com/c_images/album1584/ACH_TraderPass1.gif)"></li>

		   <li style="background-image: url(https://cdn.classichabbo.com/c_images/album1584/Z63.gif)"></li>

		   <li style="background-image: url(https://cdn.classichabbo.com/c_images/album1584/DK.gif)"></li>

		   <li style="background-image: url(https://cdn.classichabbo.com/c_images/album1584/ACH_RoomEntry1.gif)"></li>

		   <li style="background-image: url(https://cdn.classichabbo.com/c_images/album1584/ACH_Graduate1.gif)"></li>

		   <li style="background-image: url(https://cdn.classichabbo.com/c_images/album1584/ACH_AllTimeHotelPresence1.gif)"></li>

		   <li style="background-image: url(https://cdn.classichabbo.com/c_images/album1584/ACH_RegistrationDuration2.gif)"></li>

		   <li style="background-image: url(https://cdn.classichabbo.com/c_images/album1584/HC1.gif)"></li>

		   <li style="background-image: url(https://cdn.classichabbo.com/c_images/album1584/ACH_AvatarLooks1.gif)"></li>

	   </ul>


   <div id="badge-list-paging">
	 9 - 1 / 1<br />


   First | &lt;&lt; |



   &gt;&gt; | Last

   <input type="hidden" id="badgeListPageNumber" value="1" />
   <input type="hidden" id="badgeListTotalPages" value="1" />
 <script type="text/javascript">
   document.observe('dom:loaded', function() {
	 window.badgesWidget31263 = new BadgesWidget('7975', '31263');
   });
 </script>
   </div>
   </div>
	   <div class="clear"></div>

	   </div>
   </div>
</div>
</div>



	*/





    ////////////////// Remove
    /// widgetId: 31263
}

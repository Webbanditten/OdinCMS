function refreshCredits() {
    j.ajax({
        method: "GET",
        url: habboReqPath + "/api/me/credits",
    })
        .done(function (msg) {
            updateHabboCreditAmounts(msg);
    });
}
j(document).ready(function () {
    j(".buy-btn").click(function (e) {
        e.preventDefault();
        var dialog = createDialog("purchase_dialog", "Bekræft køb", 9001, 0, -1000, closePurchase);
        appendDialogBody(dialog, "<p style=\"text-align:center\"><img src=\"/images/progress_bubbles.gif\" alt=\"\" width=\"29\" height=\"5\" /></p><div style=\"clear\"></div>", true);
        moveDialogToCenter(dialog);
        showOverlay();
        new Ajax.Request(
            habboReqPath + "/furnipurchase/purchase_confirmation",
            {
                method: "post", parameters: "product=" + j(this).data("product"), onComplete: function (req, json) {
                    setDialogBody(dialog, req.responseText);
                }
            });

    });

    j(".roomForward").click(function (e) {
        e.preventDefault();
        roomForward(this, j(this).data("roomid"), j(this).data("type"), false); return false;

    });
});

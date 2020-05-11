function refreshCredits() {
    j.ajax({
        method: "GET",
        url: habboReqPath + "/api/me/credits",
    })
        .done(function (msg) {
            updateHabboCreditAmounts(msg);
    });
}
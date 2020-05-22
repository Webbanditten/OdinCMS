function refreshCredits() {
    j.ajax({
        method: "GET",
        url: habboReqPath + "/api/me/credits",
    })
        .done(function (msg) {
            updateHabboCreditAmounts(msg);
    });
}
function refreshOnlineCount() {
    j.ajax({
        method: "GET",
        url: habboReqPath + "/api/hotel/online",
    })
        .done(function (msg) {
            j(".online-count").text(msg);
        });
}
function f() {
    document.getElementById('sol').style.visibility = 'visible';
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
    j(".openClient").click(function (e) {
        e.preventDefault();
        openOrFocusHabbo(this); return false;
    });

    j(".roomForward").click(function (e) {
        e.preventDefault();
        roomForward(this, j(this).data("roomid"), j(this).data("type"), false); return false;

    });


    // riddles

    function riddles() {
    var riddles = new Array('Pletter dit gulv, men er ingen plet, hænger ved jorden, men flyver dog let, synlig i sol, men ikke i regn, lever bag alle i hver en egn. Hvem er jeg?',
        'Hvad får en kannibal hvis den kommer for sent til frokost?',
        'Hvorfor må hunde ikke være med i fodbold?',
        'Hvilke tanter er mest kendt blandt voksne?',
        'Hvordan døde Kaptajn Klo?',
        'Hvorfor flyver fuglene til de varme lande?',
        'Hvorfor spiser franskmænd snegle?',
        'Hvorfor gemmer griseungerne sig bag deres mor?');
    var answers = new Array('En skygge',
        'En kold skulder!',
        'De fælder for meget',
        'Kontanter',
        'Han pillede næse med den forkerte hånd',
        'Fordi de ikke gider gå!',
        'Fordi de ikke kan lide fastfood!',
        'Fordi de er flove over at deres mor er et svin!');
    var authors = new Array('Marcuscool',
        'bobba,marster',
        'this7',
        '???DJ',
        'soren9',
        'AkiNr',
        '4130-boy',
        'Simon-:P');
    var toplimit = riddles.length;
    var number = Math.floor(Math.random() * toplimit);

    j('#riddle').html('<img src="/c_images/album1280/motoreZ.gif" style="float:right;margin-left:8px;"><div id="riddle">' +
    '<i>Indsendt af: <b><a href="/home/' + authors[number] + '">' + authors[number] + '</a></b></i><br><br>' +
    riddles[number] +
    '<br><br>' +
    '<input type="button" value="Løsning" onClick="f();"><br><br>' +
    '</div>' +

    '<div id="sol" style="visibility:hidden;">' +
    '<span style="font-weight:bold;">Svar:</span> ' + answers[number] +
        '</div>');
    }


    // tips
    function tips() {
        var tips = new Array('Når du indretter dit hotelværelse kan du holde ALT inde for at flytte dine møbler hurtigere!',
            'Ser du ofte habboer der siger: "H..bo .vo. el.er.?" - altså en masse punktummer? Det er fordi du står for langt væk. Prøv at gå lidt tættere på, så du kan høre hvad de siger.',
            'Gå aldrig ind på Habbo snydehjemmesider, de tager bare dine ting og du får ikke noget.',
            'Hvis dit rum er kedeligt, så køb noget tapet og gulv. Det hjælper.',
            'Hvis du er i et rum med mange mennesker, og der er en der til dig men du ikke kan finde ham/hende, så trykker du bare på beskeden og der kommer en gul pil over den som har skrevet det ;)',
            'Vil du gerne vide hvem der er inde i et fællesrum? Så prøv og hold "Shift" knappen nede og tryk på rummet, så kan du se hvem der er derinde :)',
            'Køb aldrig Habbo Mønter uden at få lov af dine forældre',
            'Stol aldrig på dem der tilbyder job for ting',
            'Hvis du er HC så kan du skrive :chooser så kan du se alle i rummet, og :furni så kan du se alle tingene',
            'Hvis du skal råbe noget hurtigt, og du ikke har tid til at gå ned og trykke "Råb", så kan du holde Shift i bund, og sende din besked, så råber din Habbo den ene besked :)',
            'Brug altid en gyldig e-mail i Habbo. Hvis du ikke kan huske koden til din mail, eller du har indtastet en ugyldig e-mail, så kan du desværre heller ikke skifte adgangskode til Habbo',
            'Giv ALDRIG din kode ud til nogen lige meget hvad der sker. Om så det er din bedste ven kan han finde på at tage dine ting.',
            'Hvis en ven sender en besked til dig som du syntes er irriterende, så kan man nede i venstre hjørne rappotere beskeden, men det må kun bruges i nødstilfælde, f.eks. hvis en chikanerer dig',
            'Billeder kan gøre ethvert rum sjovt!',
            'Hvis en person gemmer sig bag en mur i SnowStorm eller er langt væk, så bare hold Shift-knappen nede mens du kaster, så kan du kaste længere og over ting.',
            'Du kan godt få hjælp af dem der ikke er HX. Det er nemlig ikke kun HX der kan hjælpe.',
            'Hvis du ikke ved hvordan man laver hjerter el. lign. så se under: Habbo.dk > Hjælp og sikkerhed > FAQ > Tips og tricks til din Habbo.',
            'Slet aldrig dit rum før du har fået alle tingene ud',
            'Hvis du hurtigt vil finde en online på din Habbo-konsol, så tryk på det bogstav brugeren begynder med. Så kommer du ned til brugeren.',
            'Prøv at dobbeltklikke på grillen i Picnic Parken, så får du en hamburger. Eller prøv det samme med sodavands-kassen, så får du en sodavand.');
        var authors = new Array('JoyBit',
            'Vicken',
            'steen.2200',
            'kamaro',
            'Royal-Duck',
            'Enima',
            'idamarie',
            'besse999',
            '--2pac--',
            'Selvestekim',
            'SINGEL-HS',
            '-PsP-',
            '-HaxH-',
            'meal!deal',
            'theripper',
            'Body.Guard',
            'R.Carlos',
            'Morten44',
            '4130-boy',
            'Silverfan-4ever');
        var toplimit = tips.length;
        var number = Math.floor(Math.random() * toplimit);

        j('#tips').append('<img src="/c_images/album1280/reporter_02_guest.gif" width="35" height="87" id="galleryImage" border="0" alt="" style="float:right;margin-left:8px;">');
        j('#tips').append('<i>Indsendt af: <b><a href="/home/' + authors[number] + '">' + authors[number] + '</a></b></i><br><br>');
        j('#tips').append(tips[number]);


    }
    tips();
    riddles();

    setInterval(function () {
        refreshOnlineCount();
    }, 5000);
    refreshOnlineCount();
});

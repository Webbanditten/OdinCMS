// Shared TinyMCE setup for housekeeping (news, pages, campaign news action). Pages load it through
// Areas/Housekeeping/Views/Shared/_EditorScripts.cshtml, after TinyMCE 8 (self-hosted under the GPL) and the Habbo
// plugins: Furni and Habbo (editor-images.js), Buy furni (editor-shop.js), Room (editor-rooms.js) and Codes (editor-codes.js).
//
// initHabboEditor(selector, settings)
//   settings.boxCodes: the Codes dialog also offers box widgets such as {staff} and {credits}. Page boxes render
//   them; news doesn't, so leave it off there.
(function () {
    // TinyMCE's default toolbar (for the plugins loaded below); the Habbo buttons go after it.
    var defaultToolbar = 'undo redo | styles | bold italic | alignleft aligncenter alignright alignjustify | outdent indent';
    var habboPlugins = ['habbofurni', 'habboavatar', 'habboshop', 'habboroom', 'habbocodes'];

    window.initHabboEditor = function (selector, settings) {
        var habbo = habboPlugins.filter(function (name) { return !!tinymce.PluginManager.get(name); });
        return tinymce.init({
            selector: selector,
            license_key: 'gpl',
            promotion: false,
            relative_urls: false,
            convert_urls: false,
            remove_script_host: false,
            // TinyMCE 6+ dropped forced_root_block: '' (the old setting). A <div> root block has no default
            // margins, and linebreak mode makes Enter insert <br> like the old force_br_newlines did, so
            // articles render the same on the site.
            forced_root_block: 'div',
            newline_behavior: 'linebreak',
            // TinyMCE 5's print, paste and imagetools plugins no longer exist (paste is core now).
            plugins: ['advlist autolink lists link image charmap preview anchor searchreplace visualblocks code fullscreen insertdatetime media table wordcount'].concat(habbo).join(' '),
            toolbar: defaultToolbar + (habbo.length ? ' | ' + habbo.join(' ') : ''),
            // Wrap instead of hiding buttons behind "...", so the Habbo buttons stay visible in narrow editors.
            toolbar_mode: 'wrap',
            habbo_box_codes: !!(settings && settings.boxCodes)
        });
    };
})();

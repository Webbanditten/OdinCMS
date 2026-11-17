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

    // The media library (media-library.js), when loaded: an "Image" button that inserts a picked image, the image
    // dialog's browse button, and pasted or dropped images uploaded to it instead of staying in the text as data.
    if (window.HabboMedia && !tinymce.PluginManager.get('habboimage')) {
        tinymce.PluginManager.add('habboimage', function (editor) {
            editor.ui.registry.addButton('habboimage', {
                text: 'Image',
                icon: 'image',
                tooltip: 'Insert an image from the media library (or upload one)',
                onAction: function () {
                    HabboMedia.open({
                        title: 'Insert an image',
                        onSelect: function (item) {
                            editor.insertContent('<img src="' + editor.dom.encode(item.url) + '" alt="" />');
                        }
                    });
                }
            });
            return { getMetadata: function () { return { name: 'Image library' }; } };
        });
    }

    window.initHabboEditor = function (selector, settings) {
        var habbo = ['habboimage'].concat(habboPlugins).filter(function (name) { return !!tinymce.PluginManager.get(name); });
        var media = window.HabboMedia ? {
            file_picker_types: 'image',
            file_picker_callback: function (callback, value) {
                HabboMedia.open({ title: 'Choose an image', current: value, onSelect: function (item) { callback(item.url, { alt: '' }); } });
            },
            automatic_uploads: true,
            images_upload_handler: function (blobInfo) {
                return HabboMedia.upload([new File([blobInfo.blob()], blobInfo.filename(), { type: blobInfo.blob().type })], 'content')
                    .then(function (data) { return data.items[0].url; });
            }
        } : {};
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
            habbo_box_codes: !!(settings && settings.boxCodes),
            ...media
        });
    };
})();

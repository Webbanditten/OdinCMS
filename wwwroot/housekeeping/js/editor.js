// Shared TinyMCE setup for housekeeping (news, pages, campaign news action).
// Requires ~/housekeeping/js/tinymce/tinymce.min.js (TinyMCE 8, self-hosted under the GPL).
window.initHabboEditor = function (selector) {
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
        plugins: 'advlist autolink lists link image charmap preview anchor searchreplace visualblocks code fullscreen insertdatetime media table wordcount'
    });
};

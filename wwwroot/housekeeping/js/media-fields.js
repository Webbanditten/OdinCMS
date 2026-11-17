// Image fields with a media library button (media-library.js):
//   <input id="Img"> <button type="button" data-media-for="Img" data-media-category="promo">Choose image</button>
//   <img data-media-preview-for="Img">   (follows the field, hidden while it is empty)
(function () {
    function preview(input) {
        document.querySelectorAll('[data-media-preview-for="' + input.id + '"]').forEach(function (img) {
            img.style.display = input.value ? '' : 'none';
            if (input.value) img.src = input.value;
        });
    }
    document.querySelectorAll('[data-media-for]').forEach(function (button) {
        var input = document.getElementById(button.dataset.mediaFor);
        if (!input) return;
        button.addEventListener('click', function () {
            HabboMedia.open({
                title: button.dataset.mediaTitle || 'Choose an image',
                category: button.dataset.mediaCategory,
                current: input.value,
                onSelect: function (item) {
                    input.value = item.url;
                    input.dispatchEvent(new Event('input', { bubbles: true }));
                    input.dispatchEvent(new Event('change', { bubbles: true }));
                }
            });
        });
        input.addEventListener('input', function () { preview(input); });
        preview(input);
    });
})();

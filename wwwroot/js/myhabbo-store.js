/*
 * Habbo Home web store, inventory and note editor, laid out like habbo.com's first Home editor (2006; structure after
 * jeanropke/Neptune's port of it). The edit toolbar opens one item type at a time in a grey dialog: the inventory
 * first, "Køb ..." turns the same dialog into that type's store and "Åbn inventar" turns it back. Notes have their own
 * editor, which also sells them. Loaded on demand by homeview.js (ScriptLoader "myhabbo-store").
 *
 * Server side: Areas/MyHabbo/Controllers/StoreController.cs and NoteEditorController.cs. Placing items on the page is
 * homeedit.js (doPlaceImageOnPage, doPlaceWidget, doChangeBg, initMovableItems).
 */
var WebStore = {

  created: false,
  dialog: null,
  selectedType: null,
  selectedItem: null,
  selectedCategory: null,
  selectedCategoryId: null,
  loadingCategory: false,
  previewItems: null,
  previewItemPointer: 0,
  storeOpened: false,
  backgroundPreviewWarning: false,
  originalBg: null,

  /** type: "stickers", "widgets" or "backgrounds" */
  open: function(type) {
    if (!WebStore.created) {
      WebStore._create();
    }
    if (WebStore.selectedType != type) {
      WebStore.Inventory.inventoryOpened = false;
      WebStore.storeOpened = false;
    }
    WebStore.originalBg = $("mypage-bg").className;
    Overlay.show();
    WebStore.dialog.show();
    WebStore._loadMainContent(type, "inventory");
    Dialog.moveDialogToCenter(WebStore.dialog);
  },

  close: function() {
    if (!WebStore.dialog) {
      return;
    }
    WebStore.dialog.style.left = "-1500px";
    WebStore.dialog.hide();
    Overlay.hide();

    WebStore.Inventory.newItems = [];
    WebStore.Inventory.updateNewItemCount();
    if (WebStore.originalBg != null) {
      $("mypage-bg").className = WebStore.originalBg;
    }
    WebStore.loadingCategory = false;
    WebStore.Inventory.loadingCategory = false;
  },

  /** habbo.com's grey dialog (dialog-grey in style.css), which the 2006 store and note editor use. */
  createDialog: function(dialogId, header, zIndex, left, top, exitCallback) {
    var overlay = $("overlay");
    var headerBar = [Builder.node("div", [Builder.node("h3", [Builder.node("span", header || "")])])];
    if (exitCallback) {
      var exitButton = Builder.node("a", { href: "#", className: "dialog-grey-exit" }, [
        Builder.node("img", { src: habboStaticFilePath + "/images/dialogs/grey-exit.gif", width: 12, height: 12, alt: "" })
      ]);
      Event.observe(exitButton, "click", exitCallback, false);
      headerBar.push(exitButton);
    }
    var dialog = overlay.parentNode.insertBefore(Builder.node("div", { id: dialogId, className: "dialog-grey" }, [
      Builder.node("div", { className: "dialog-grey-top dialog-grey-handle" }, headerBar),
      Builder.node("div", { className: "dialog-grey-content" }, [
        Builder.node("div", { id: dialogId + "-body", className: "dialog-grey-body" }, [Builder.node("div", { className: "clear" })])
      ]),
      Builder.node("div", { className: "dialog-grey-bottom" }, [Builder.node("div")])
    ]), overlay.nextSibling);
    dialog.style.zIndex = zIndex || 9001;
    dialog.style.left = (left || -1000) + "px";
    dialog.style.top = (top || 0) + "px";
    if (typeof Draggable != "undefined") {
      new Draggable(dialog, { handle: "dialog-grey-handle", starteffect: Prototype.emptyFunction, endeffect: Prototype.emptyFunction, zindex: 9100 });
    }
    return dialog;
  },

  _create: function() {
    WebStore.dialog = WebStore.createDialog("purchase-main-dialog", "", 9001, -1500, 0, function(e) {
      if (e) { Event.stop(e); }
      WebStore.close();
    });
    WebStore.created = true;
  },

  /** tab: "inventory" or "store" */
  _loadMainContent: function(type, tab) {
    WebStore.selectedType = type;
    if (tab == "inventory") {
      if (WebStore.Inventory.inventoryOpened) {
        WebStore.hideWebStoreDivs();
        WebStore.Inventory.reloadIfNeeded();
        WebStore.Inventory.newItems = [];
        WebStore.Inventory.updateNewItemCount();
      } else {
        WebStore.Inventory.load(type);
      }
      return;
    }

    if (WebStore.storeOpened) {
      WebStore.Inventory.hideInventoryDivs();
      return;
    }
    WebStore.Inventory.hideInventoryDivs();
    Element.wait($("webstore-content"));
    new Ajax.Request(habboReqPath + "/myhabbo/store/main", {
      method: "post", parameters: { type: type },
      onComplete: function(req, json) {
        if (WebStore._checkResponse(req.responseText)) {
          $("webstore-content").innerHTML = req.responseText;
          WebStore.storeOpened = true;
          WebStore.selectedItem = null;
          WebStore._clearPreview();

          var firstEl = $A($("webstore-categories").getElementsByTagName("li")).first();
          if (firstEl) {
            WebStore.selectedCategory = firstEl;
            WebStore.selectedCategoryId = firstEl.id.split("-")[1];
            firstEl.className = "subcategory-selected";
          }

          if (json && json[0]) {
            WebStore.previewItems = json;
            WebStore._showPreview();
            var el = $A($("webstore-item-list").getElementsByTagName("li")).first();
            if (el && el.id) { WebStore._selectItem(el); }
          }
          WebStore.Inventory.updateNewItemCount();
          WebStore._setEventHandlers();
        }
      }
    });
  },

  // explicit display values: the stylesheet hides #webstore-content until the store is opened
  hideWebStoreDivs: function() {
    if ($("webstore-content")) { $("webstore-content").style.display = "none"; }
    if ($("inventory-content")) { $("inventory-content").style.display = "block"; }
  },

  _setEventHandlers: function() {
    if ($("webstore-items")) {
      $("webstore-items").onclick = WebStore._handleItemClick.bindAsEventListener(WebStore);
    }
    if ($("webstore-preview")) {
      $("webstore-preview").onclick = WebStore._handlePreviewClick.bindAsEventListener(WebStore);
    }
    if ($("webstore-close")) {
      $("webstore-close").onclick = function(e) { Event.stop(e); WebStore.close(); };
    }
    if ($("webstore-categories")) {
      $("webstore-categories").onclick = WebStore._handleCategoryClick.bindAsEventListener(WebStore);
    }
    WebStore.Inventory._setEventHandlers();
  },

  _handleCategoryClick: function(e) {
    Event.stop(e);
    if (!WebStore.loadingCategory) {
      var el = Event.findElement(e, "li");
      if (el && el.id && el.id.indexOf("category-") == 0) {
        var temp = el.id.split("-");
        WebStore._changeCategory(temp[1], el);
        $("webstore-content-container").className = temp[2];
      }
    }
  },

  _changeCategory: function(categoryId, categoryEl) {
    if (WebStore.selectedCategory) {
      WebStore.selectedCategory.className = "";
    }
    WebStore.selectedCategory = categoryEl;
    categoryEl.className = "subcategory-selected";
    WebStore.openCategory(categoryId);
  },

  openCategory: function(categoryId, force, productId) {
    if ((!WebStore.loadingCategory && categoryId != WebStore.selectedCategoryId) || force) {
      WebStore.loadingCategory = true;
      WebStore._resetState();
      Element.wait($("webstore-items"));
      WebStore._loadCategory(categoryId, productId);
    }
  },

  _loadCategory: function(categoryId, productId) {
    WebStore.selectedCategoryId = categoryId;
    new Ajax.Request(habboReqPath + "/myhabbo/store/items", {
      method: "post", parameters: { categoryId: categoryId },
      onComplete: function(req, json) {
        WebStore.loadingCategory = false;
        if (WebStore._checkResponse(req.responseText)) {
          $("webstore-content-container").innerHTML = req.responseText;

          var productEls = $A($("webstore-item-list").getElementsByTagName("li")).findAll(function(el) { return el.id; });
          var selected = productEls.find(function(el) { return productId && el.id.split("-").last() == productId; }) || productEls.first();
          if (selected) {
            WebStore._selectItem(selected);
            WebStore._loadPreview(selected.id.split("-").last());
          } else {
            WebStore._showDefaultPreview();
          }
          WebStore._setEventHandlers();
        }
      }
    });
  },

  _handleItemClick: function(e) {
    var el = Event.findElement(e, "li");
    if (el && el.id && el != WebStore.selectedItem) {
      WebStore._selectItem(el);
      WebStore._loadPreview(el.id.split("-").last());
    }
    Event.stop(e);
  },

  _selectItem: function(el) {
    if (WebStore.selectedItem && WebStore.selectedItem != el) {
      Element.removeClassName(WebStore.selectedItem, "selected");
    }
    Element.addClassName(el, "selected");
    WebStore.selectedItem = el;
  },

  _handlePreviewClick: function(e) {
    var el = Event.findElement(e, "a");
    if (!el || !el.id) {
      return;
    }
    if (el.id == "webstore-purchase") {
      Event.stop(e);
      if (WebStore.selectedItem) {
        WebStore._purchaseConfirm(WebStore.selectedItem.id.split("-").last());
      }
    } else if (el.id == "webstore-preview-bg") {
      Event.stop(e);
      if (WebStore.backgroundPreviewWarning) {
        WebStore._previewBg();
        return;
      }
      var warning = WebStore.createDialog("webstore-warning", "", 9003);
      Dialog.setAsWaitDialog(warning);
      Overlay.move(9002);
      Dialog.moveDialogToCenter(warning);
      new Ajax.Request(habboReqPath + "/myhabbo/store/background_warning", {
        method: "post",
        onComplete: function(req) {
          if (WebStore._checkResponse(req.responseText)) {
            Dialog.setDialogBody(warning, req.responseText);
            WebStore.backgroundPreviewWarning = true;
            Event.observe($("webstore-warning-ok"), "click", function(e) {
              Event.stop(e);
              Element.remove(warning);
              Overlay.move(9000);
              WebStore._previewBg();
            });
          }
        }
      });
    }
  },

  /** Shows the previewed background on the page for a moment (the overlay goes see-through). */
  _previewBg: function() {
    var item = WebStore.previewItems && WebStore.previewItems[WebStore.previewItemPointer];
    if (!item || !item.bgCssClass) {
      return;
    }
    var overlay = $("overlay");
    overlay.style.backgroundColor = "transparent";
    $("mypage-bg").className = item.bgCssClass;
    window.setTimeout(function() {
      if (WebStore.dialog.style.left != "-1500px") {
        overlay.style.backgroundColor = "black";
      }
    }, 2500);
  },

  _resetState: function() {
    WebStore.selectedItem = null;
    WebStore._showDefaultPreview();
  },

  _loadPreview: function(productId) {
    WebStore._clearPreview();
    Element.wait($("webstore-preview"));
    $("webstore-preview").show();
    new Ajax.Request(habboReqPath + "/myhabbo/store/preview", {
      method: "post", parameters: { productId: productId },
      onComplete: function(req, json) {
        if (WebStore._checkResponse(req.responseText)) {
          WebStore.previewItems = json;
          WebStore._showPreview(req.responseText);
        }
      }
    });
  },

  _showPreview: function(previewHtml) {
    var previewDiv = $("webstore-preview");
    if (!previewDiv) {
      return;
    }
    if (previewHtml) { previewDiv.innerHTML = previewHtml; }

    if (WebStore.previewItems && $("webstore-preview-box")) {
      var preview = $("webstore-preview-box").appendChild(Builder.node("div", { id: "webstore-preview-pre" }));
      if (WebStore.previewItems.length == 1 && WebStore.previewItems[0].itemCount > 1) {
        preview.appendChild(Builder.node("div", { id: "webstore-preview-count", className: "webstore-item-count" }));
      }
      WebStore._setPreviewItem();
    }
    previewDiv.show();
  },

  _showDefaultPreview: function() {
    WebStore._clearPreview();
    var previewDiv = $("webstore-preview");
    if (previewDiv) {
      previewDiv.hide();
      previewDiv.innerHTML = "";
    }
    if ($("webstore-preview-default")) {
      $("webstore-preview-default").show();
    }
  },

  _setPreviewItem: function() {
    var currItem = WebStore.previewItems[WebStore.previewItemPointer];
    var pre = $("webstore-preview-pre");
    pre.className = currItem.previewCssClass;
    pre.title = currItem.titleKey;
    if ($("webstore-preview-count")) {
      $("webstore-preview-count").innerHTML = "<div>x" + currItem.itemCount + "</div>";
    }
    if (currItem.bgCssClass) {
      if (!$("webstore-preview-bgpreview") && $("webstore-preview-bg-text")) {
        pre.appendChild(Builder.node("div", { id: "webstore-preview-bgpreview" }, [
          Builder.node("a", { href: "#", className: "toolbutton", id: "webstore-preview-bg" }, [
            Builder.node("span", $("webstore-preview-bg-text").innerHTML)
          ])
        ]));
      }
    } else if ($("webstore-preview-bgpreview")) {
      Element.remove($("webstore-preview-bgpreview"));
    }
  },

  _clearPreview: function() {
    WebStore.previewItems = null;
    WebStore.previewItemPointer = 0;
    if ($("webstore-preview-box")) {
      $("webstore-preview-box").innerHTML = "";
    }
  },

  closeConfirmation: function(e) {
    if (e) { Event.stop(e); }
    if ($("webstore-confirm")) {
      Element.remove($("webstore-confirm"));
      Overlay.move(9000);
    }
  },

  _purchaseConfirm: function(productId) {
    Overlay.move(9002);
    var dialog = WebStore.createDialog("webstore-confirm", "", 9003, -1500, 0, WebStore.closeConfirmation);
    Dialog.setAsWaitDialog(dialog);
    Dialog.moveDialogToCenter(dialog);

    new Ajax.Request(habboReqPath + "/myhabbo/store/purchase_confirm", {
      method: "post", parameters: { productId: productId },
      onComplete: function(req) {
        if (WebStore._checkResponse(req.responseText)) {
          Dialog.setDialogBody(dialog, req.responseText);
          if ($("webstore-confirm-submit")) {
            Event.observe($("webstore-confirm-submit"), "click", function(e) {
              Event.stop(e);
              WebStore._purchase(productId, dialog);
            });
          }
          if ($("webstore-confirm-cancel")) {
            Event.observe($("webstore-confirm-cancel"), "click", WebStore.closeConfirmation);
          }
        }
      }
    });
  },

  _purchase: function(productId, dialog) {
    Dialog.setAsWaitDialog(dialog);
    new Ajax.Request(habboReqPath + "/myhabbo/store/purchase_" + WebStore.selectedType, {
      method: "post", parameters: { selectedId: productId },
      onComplete: function(req) {
        if (!WebStore._checkResponse(req.responseText)) {
          return;
        }
        if (req.responseText.strip() != "OK") {
          // Refused (owned already, not enough coins): the answer comes with its own OK button.
          Dialog.setDialogBody(dialog, req.responseText);
          if ($("webstore-confirm-cancel")) {
            Event.observe($("webstore-confirm-cancel"), "click", WebStore.closeConfirmation);
          }
          return;
        }
        WebStore.Inventory.newItems.push(productId);
        Overlay.move(9000);
        Element.remove(dialog);

        // the store shows the new purse; the inventory reloads when it is shown again
        WebStore.openCategory(WebStore.selectedCategoryId, true, productId);
        WebStore.Inventory.waitingForReload = true;
        WebStore.Inventory.updateNewItemCount();
      }
    });
  },

  _checkResponse: function(responseText) {
    if (responseText.strip() == "REFRESH") {
      WebStore.NoteEditor.close();
      WebStore.close();
      Overlay.show();
      window.location.replace(window.location.href);
      return false;
    }
    return true;
  }
};

WebStore.Inventory = {

  newItems: [],
  inventoryOpened: false,
  selectedItem: null,
  loadingCategory: false,
  previewItem: null,
  waitingForReload: false,

  /** "Åbn inventar (n)": the purchases since the inventory was last shown. */
  updateNewItemCount: function() {
    var newDiv = $("webstore-inventory-new");
    if (!newDiv) {
      return;
    }
    var itemCount = WebStore.Inventory.newItems.length;
    if (itemCount > 0) {
      newDiv.innerHTML = " (" + itemCount + ")";
      newDiv.className = "new";
      newDiv.style.display = "block";
    } else {
      newDiv.innerHTML = "";
      newDiv.className = "";
      newDiv.style.display = "none";
    }
  },

  load: function(type) {
    Dialog.setAsWaitDialog(WebStore.dialog);
    new Ajax.Request(habboReqPath + "/myhabbo/store/inventory", {
      method: "post", parameters: { type: type },
      onComplete: function(req, json) {
        if (WebStore._checkResponse(req.responseText)) {
          Dialog.setDialogBody(WebStore.dialog, req.responseText);
          WebStore.Inventory.inventoryOpened = true;
          WebStore.Inventory.waitingForReload = false;
          WebStore.Inventory.selectedItem = null;
          WebStore.Inventory.previewItem = json;
          WebStore.Inventory._showPreview();
          var el = WebStore.Inventory._firstSelectable();
          if (el) { WebStore.Inventory._selectItem(el); }
          WebStore._setEventHandlers();
          Dialog.moveDialogToCenter(WebStore.dialog);
        }
      }
    });
  },

  /** Reloads the item list (after a purchase or an item taken off the page). */
  loadCategory: function(type) {
    if (WebStore.Inventory.loadingCategory) {
      return;
    }
    Element.wait($("inventory-items"));
    WebStore.Inventory._clearPreview();
    WebStore.Inventory.loadingCategory = true;
    new Ajax.Request(habboReqPath + "/myhabbo/store/inventory_items", {
      method: "post", parameters: { type: type },
      onComplete: function(req) {
        WebStore.Inventory.loadingCategory = false;
        if (WebStore._checkResponse(req.responseText)) {
          $("inventory-items").innerHTML = req.responseText;
          WebStore.Inventory.selectedItem = null;
          var el = WebStore.Inventory._firstSelectable();
          if (el) {
            WebStore.Inventory._selectItem(el);
            var temp = el.id.split("-");
            WebStore.Inventory._loadPreview(temp.last(), type);
          } else {
            WebStore.Inventory._showDefaultPreview();
          }
          WebStore._setEventHandlers();
        }
      }
    });
  },

  reloadIfNeeded: function() {
    if (WebStore.Inventory.waitingForReload && WebStore.selectedType) {
      WebStore.Inventory.waitingForReload = false;
      WebStore.Inventory.loadCategory(WebStore.selectedType);
    }
  },

  hideInventoryDivs: function() {
    if ($("inventory-content")) { $("inventory-content").style.display = "none"; }
    if ($("webstore-content")) { $("webstore-content").style.display = "block"; }
  },

  _firstSelectable: function() {
    if (!$("inventory-item-list")) {
      return null;
    }
    return $A($("inventory-item-list").getElementsByTagName("li")).find(function(el) {
      return el.id && !Element.hasClassName(el, "webstore-widget-disabled");
    });
  },

  _setEventHandlers: function() {
    if ($("inventory-items")) {
      $("inventory-items").onclick = WebStore.Inventory._handleItemClick.bindAsEventListener(WebStore.Inventory);
    }
    if ($("inventory-preview")) {
      $("inventory-preview").onclick = WebStore.Inventory._handlePreviewClick.bindAsEventListener(WebStore.Inventory);
    }
    if ($("purchase-stickers")) {
      $("purchase-stickers").onclick = function(e) { Event.stop(e); WebStore._loadMainContent(WebStore.selectedType, "store"); };
    }
    if ($("inventory-close")) {
      $("inventory-close").onclick = function(e) { Event.stop(e); WebStore.close(); };
    }
    if ($("load-inventory")) {
      $("load-inventory").onclick = function(e) { Event.stop(e); WebStore._loadMainContent(WebStore.selectedType, "inventory"); };
    }
  },

  _handleItemClick: function(e) {
    Event.stop(e);
    var el = Event.findElement(e, "li");
    if (el && el.id && el != WebStore.Inventory.selectedItem && !Element.hasClassName(el, "webstore-widget-disabled")) {
      WebStore.Inventory._selectItem(el);
      WebStore.Inventory._loadPreview(el.id.split("-").last(), WebStore.selectedType);
    }
  },

  _handlePreviewClick: function(e) {
    var el = Event.findElement(e, "a");
    if (el && el.id == "inventory-place") {
      Event.stop(e);
      WebStore.Inventory._place();
    }
  },

  _selectItem: function(el) {
    if (WebStore.Inventory.selectedItem && WebStore.Inventory.selectedItem != el) {
      Element.removeClassName(WebStore.Inventory.selectedItem, "selected");
    }
    Element.addClassName(el, "selected");
    WebStore.Inventory.selectedItem = el;
  },

  _loadPreview: function(itemId, type) {
    WebStore.Inventory._clearPreview();
    Element.wait($("inventory-preview"));
    new Ajax.Request(habboReqPath + "/myhabbo/store/inventory_preview", {
      method: "post", parameters: { itemId: itemId, type: type },
      onComplete: function(req, json) {
        if (WebStore._checkResponse(req.responseText)) {
          WebStore.Inventory.previewItem = json;
          WebStore.Inventory._showPreview(req.responseText);
        }
      }
    });
  },

  _showPreview: function(previewHtml) {
    var previewDiv = $("inventory-preview");
    if (!previewDiv) {
      return;
    }
    if (previewHtml) { previewDiv.innerHTML = previewHtml; }

    var item = WebStore.Inventory.previewItem;
    if (item && item.length > 0 && $("inventory-preview-box")) {
      var preview = $("inventory-preview-box").appendChild(Builder.node("div", { id: "inventory-preview-pre" }));
      if (item[5] && item[5] > 1) {
        preview.appendChild(Builder.node("div", { id: "inventory-preview-count", className: "webstore-item-count" }));
      }
      WebStore.Inventory._setPreviewItem();
      $("inventory-preview-container").show();
    }
    previewDiv.show();
  },

  _clearPreview: function() {
    WebStore.Inventory.previewItem = null;
    if ($("inventory-preview-box")) {
      $("inventory-preview-box").innerHTML = "";
    }
  },

  _showDefaultPreview: function() {
    WebStore.Inventory._clearPreview();
    if ($("inventory-preview-container")) {
      $("inventory-preview-container").hide();
    }
  },

  _setPreviewItem: function() {
    var item = WebStore.Inventory.previewItem;
    var pre = $("inventory-preview-pre");
    pre.className = item[0];
    pre.title = item[2];
    if ($("inventory-preview-count")) {
      $("inventory-preview-count").innerHTML = "<div>x" + item[5] + "</div>";
    }
  },

  _place: function() {
    var selected = WebStore.Inventory.selectedItem;
    if (!selected) {
      return;
    }
    var temp = selected.id.split("-");
    if (WebStore.selectedType == "stickers") {
      doPlaceImageOnPage(temp.last());
    } else if (WebStore.selectedType == "backgrounds") {
      if (!WebStore.Inventory.previewItem) {
        return;
      }
      WebStore.originalBg = WebStore.Inventory.previewItem[1];
      doChangeBg(WebStore.Inventory.previewItem[1], temp.last());
    } else if (WebStore.selectedType == "widgets") {
      doPlaceWidget(temp[3], temp[2] == "p");
    }
    WebStore.Inventory._closeAfterPlace();
  },

  _closeAfterPlace: function() {
    WebStore.close();
    WebStore.Inventory._clearPreview();
    if ($("inventory-items")) {
      Element.wait($("inventory-items"));
    }
    WebStore.Inventory.waitingForReload = true;
  }
};

/** The avatar sticker editor of later Homes isn't offered; homeedit.js's previewEsticker still calls this. */
WebStore.StickerEditor = {
  preview: function() {}
};

WebStore.NoteEditor = {

  dialog: null,
  noteParams: null,
  purchaseTitle: "",
  placeAfterPurchase: false,

  /** From the toolbar's Noter button. */
  open: function() {
    WebStore.NoteEditor.noteParams = null;
    Overlay.show();
    WebStore.NoteEditor.dialog = WebStore.createDialog("webstore-noteeditor", "", 9003, -1500, 0, WebStore.NoteEditor.close);
    Dialog.setAsWaitDialog(WebStore.NoteEditor.dialog);
    Dialog.moveDialogToCenter(WebStore.NoteEditor.dialog);
    WebStore.NoteEditor._loadEditor();
  },

  close: function(e) {
    if (e) { Event.stop(e); }
    WebStore.NoteEditor._removePurchaseDialog();
    if ($("webstore-noteeditor")) {
      Element.remove($("webstore-noteeditor"));
      Overlay.hide();
    }
    WebStore.NoteEditor.noteParams = null;
  },

  _loadEditor: function(noteParams, backFromPreview) {
    WebStore.NoteEditor.noteParams = noteParams || "";
    new Ajax.Request(habboReqPath + "/myhabbo/noteeditor/editor", {
      method: "post", parameters: WebStore.NoteEditor.noteParams,
      onComplete: function(req) {
        if (!WebStore._checkResponse(req.responseText)) {
          return;
        }
        Dialog.setDialogBody(WebStore.NoteEditor.dialog, req.responseText);
        req.responseText.evalScripts();

        var maxLength = $F("webstore-notes-maxlength");
        var limitCallback = function() {
          try {
            var currentLength = $F("webstore-notes-text").length;
            $("webstore-notes-counter").innerHTML = maxLength - currentLength;
            if (currentLength > 0) { WebStore.NoteEditor._enableContinue(); }
            else { WebStore.NoteEditor._disableContinue(); }
          } catch (ex) {}
        };
        if (backFromPreview) {
          window.setTimeout(function() {
            $("webstore-notes-text").focus();
            limitCallback();
          }, 100);
        }
        Utils.limitTextarea($("webstore-notes-text"), maxLength, limitCallback);
        if (maxLength < 0) {
          WebStore.NoteEditor._enableContinue();
        }

        if ($("note-editor-purchase")) {
          WebStore.NoteEditor.purchaseTitle = $("note-editor-purchase").innerText || $("note-editor-purchase").textContent;
          Event.observe($("note-editor-purchase"), "click", function(e) {
            Event.stop(e);
            WebStore.NoteEditor.placeAfterPurchase = false;
            WebStore.NoteEditor.openPurchaseDialog(false);
          });
        }
        if ($("webstore-confirm-cancel")) { Event.observe($("webstore-confirm-cancel"), "click", WebStore.NoteEditor.close); }
        if ($("webstore-notes-continue")) {
          Event.observe($("webstore-notes-continue"), "click", function(e) {
            Event.stop(e);
            var el = Event.findElement(e, "a");
            if (!Element.hasClassName(el, "disabled-button")) {
              WebStore.NoteEditor.noteParams = Form.serialize($("webstore-notes-form"));
              WebStore.NoteEditor._loadPreview();
            }
          });
        }
      }
    });
  },

  _loadPreview: function() {
    Dialog.setAsWaitDialog(WebStore.NoteEditor.dialog);
    new Ajax.Request(habboReqPath + "/myhabbo/noteeditor/preview", {
      method: "post", parameters: WebStore.NoteEditor.noteParams,
      onComplete: function(req) {
        if (!WebStore._checkResponse(req.responseText)) {
          return;
        }
        if (req.responseText.strip() == "BACK") {
          WebStore.NoteEditor._loadEditor(WebStore.NoteEditor.noteParams, true);
          return;
        }
        Dialog.setDialogBody(WebStore.NoteEditor.dialog, req.responseText);
        if ($("webstore-notes-add")) {
          Event.observe($("webstore-notes-add"), "click", function(e) {
            Event.stop(e);
            WebStore.NoteEditor._place();
          });
        }
        if ($("webstore-notes-edit")) {
          Event.observe($("webstore-notes-edit"), "click", function(e) {
            Event.stop(e);
            WebStore.NoteEditor._loadEditor(WebStore.NoteEditor.noteParams, true);
          });
        }
      }
    });
  },

  _place: function() {
    if (!WebStore.NoteEditor.noteParams) {
      return;
    }
    new Ajax.Request(habboReqPath + "/myhabbo/noteeditor/place", {
      method: "post", parameters: WebStore.NoteEditor.noteParams,
      onComplete: function(req, json) {
        if (!WebStore._checkResponse(req.responseText)) {
          return;
        }
        var answer = req.responseText.strip();
        if (answer == "NONOTES") {
          // none left: offer to buy notes, then place this one
          WebStore.NoteEditor.placeAfterPurchase = true;
          WebStore.NoteEditor.openPurchaseDialog(true);
        } else if (answer == "BACK") {
          WebStore.NoteEditor._loadEditor(WebStore.NoteEditor.noteParams, true);
        } else if (answer != "" && answer != "ERROR") {
          WebStore.NoteEditor.close();
          $("playground").insert(req.responseText);
          var note = $("stickie-" + json);
          Element.hide(note);
          note.style.top = "10px";
          note.style.left = "10px";
          Effect.BlindDown(note, { scaleX: true, scaleY: true });
          initMovableItems();
          Utils.setAllEmbededObjectsVisibility("hidden");
        } else {
          WebStore.NoteEditor.close();
        }
      }
    });
  },

  /** "Køb noter": the notes product's purchase question above the editor. none: the editor had no note left. */
  openPurchaseDialog: function(none) {
    WebStore.NoteEditor._removePurchaseDialog();
    var dialog = WebStore.createDialog("webstore-notes-purchase", WebStore.NoteEditor.purchaseTitle, 9005, -1500, 0, WebStore.NoteEditor.closePurchaseDialog);
    Dialog.setAsWaitDialog(dialog);
    Dialog.moveDialogToCenter(dialog);
    Overlay.move(9004);
    new Ajax.Request(habboReqPath + "/myhabbo/noteeditor/purchase", {
      method: "post", parameters: { none: !!none },
      onComplete: function(req) {
        if (!WebStore._checkResponse(req.responseText)) {
          return;
        }
        Dialog.setDialogBody(dialog, req.responseText);
        if ($("webstore-confirm-cancel")) { Event.observe($("webstore-confirm-cancel"), "click", WebStore.NoteEditor.closePurchaseDialog); }
        if ($("webstore-confirm-submit")) {
          Event.observe($("webstore-confirm-submit"), "click", function(e) {
            Event.stop(e);
            var productId = $F("webstore-confirm-product");
            Dialog.setAsWaitDialog(dialog);
            new Ajax.Request(habboReqPath + "/myhabbo/store/purchase_notes", {
              method: "post", parameters: { selectedId: productId },
              onComplete: function(req) {
                if (!WebStore._checkResponse(req.responseText)) {
                  return;
                }
                if (req.responseText.strip() != "OK") {
                  Dialog.setDialogBody(dialog, req.responseText);
                  if ($("webstore-confirm-cancel")) { Event.observe($("webstore-confirm-cancel"), "click", WebStore.NoteEditor.closePurchaseDialog); }
                  return;
                }
                var placeNow = WebStore.NoteEditor.placeAfterPurchase;
                WebStore.NoteEditor.closePurchaseDialog();
                if (placeNow) {
                  WebStore.NoteEditor._place();
                }
              }
            });
          });
        }
      }
    });
  },

  closePurchaseDialog: function(e) {
    if (e) { Event.stop(e); }
    WebStore.NoteEditor._removePurchaseDialog();
    WebStore.NoteEditor.placeAfterPurchase = false;
  },

  _removePurchaseDialog: function() {
    if ($("webstore-notes-purchase")) {
      Element.remove($("webstore-notes-purchase"));
      Overlay.move(9000);
    }
  },

  _enableContinue: function() {
    Element.removeClassName($("webstore-notes-continue"), "disabled-button");
  },

  _disableContinue: function() {
    Element.addClassName($("webstore-notes-continue"), "disabled-button");
  }
};

ScriptLoader.notify("myhabbo-store");

mergeInto(LibraryManager.library, {

  ShowKoreanInput: function(defaultValuePtr, gameObjectNamePtr, callbackMethodPtr) {
    var defaultValue   = UTF8ToString(defaultValuePtr);
    var gameObjectName = UTF8ToString(gameObjectNamePtr);
    var callbackMethod = UTF8ToString(callbackMethodPtr);

    var old = document.getElementById("unity-korean-input");
    if (old && old.parentNode) old.parentNode.removeChild(old);

    var input = document.createElement("input");
    input.id    = "unity-korean-input";
    input.type  = "text";
    input.value = defaultValue;
    input.lang  = "ko";
    input.setAttribute("inputmode", "text");
    input.style.cssText = [
      "position:fixed",
      "top:0", "left:0",
      "width:100%", "height:100%",
      "opacity:0.01",
      "z-index:9999",
      "font-size:16px",
      "background:transparent",
      "border:none",
      "outline:none",
      "caret-color:transparent",
      "color:transparent"
    ].join(";");

    document.body.appendChild(input);

    requestAnimationFrame(function() {
      input.focus();
      var len = input.value.length;
      input.setSelectionRange(len, len);
    });

    var removed = false;
    function safeRemove() {
      if (removed) return;
      removed = true;
      if (input.parentNode) input.parentNode.removeChild(input);
    }

    var isComposing = false;
    var composingData = "";

    input.addEventListener("compositionstart", function() {
      isComposing = true;
      composingData = "";
    });

    input.addEventListener("compositionupdate", function(e) {
      composingData = e.data;
      var confirmedText = input.value.slice(0, input.value.length - composingData.length);
      SendMessage(gameObjectName, "OnComposing", confirmedText + "|" + composingData);
    });

    input.addEventListener("compositionend", function() {
      isComposing = false;
      composingData = "";
      SendMessage(gameObjectName, callbackMethod, input.value);
    });

    input.addEventListener("input", function(e) {
      if (isComposing || e.isComposing) return;
      SendMessage(gameObjectName, callbackMethod, input.value);
    });

    input.addEventListener("keydown", function(e) {
      if (e.key === "Enter") {
        isComposing = false;
        composingData = "";
        SendMessage(gameObjectName, "OnInputConfirmed", input.value);
        safeRemove();
        return;
      }
      if (e.key === "Escape") {
        isComposing = false;
        composingData = "";
        SendMessage(gameObjectName, "OnInputCancelled", "");
        safeRemove();
        return;
      }

      // ★ 그 외 모든 키(단축키 포함)를 Unity로 포워딩
      // JS input이 포커스를 가지고 있어서 Unity가 못 받는 키를
      // canvas에 직접 재발송
      var canvas = document.querySelector("canvas");
      if (canvas) {
        var newEvent = new KeyboardEvent(e.type, {
          key:        e.key,
          code:       e.code,
          keyCode:    e.keyCode,
          which:      e.which,
          ctrlKey:    e.ctrlKey,
          shiftKey:   e.shiftKey,
          altKey:     e.altKey,
          metaKey:    e.metaKey,
          bubbles:    true,
          cancelable: true
        });
        canvas.dispatchEvent(newEvent);
      }
    });

    input.addEventListener("blur", function() {
      isComposing = false;
      composingData = "";
      SendMessage(gameObjectName, callbackMethod, input.value);
      safeRemove();
    });
  },

  HideKoreanInput: function() {
    var input = document.getElementById("unity-korean-input");
    if (input && input.parentNode) input.parentNode.removeChild(input);
  }

});

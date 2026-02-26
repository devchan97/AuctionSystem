mergeInto(LibraryManager.library, {
    // window.open()으로 열어야 나중에 window.close()가 동작함
    // Application.OpenURL()은 OS에 위임하므로 JS가 열지 않은 창 → close() 차단
    JS_OpenPopup: function(urlPtr) {
        var url = UTF8ToString(urlPtr);
        window.open(url, '_blank', 'width=480,height=640,noopener=0');
    }
});

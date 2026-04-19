window.setAudioPlaybackRate = function (elementId, rate) {
    var audio = document.getElementById(elementId);
    if (audio) {
        audio.playbackRate = rate;
    }
};

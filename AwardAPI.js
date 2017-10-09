var award_scaleform = null;
var display_scaleform = false;
var scaleform_time = 0;
var display_time = 4000;

var colors = [107, 108, 109, 110];

function configureScaleform(data) {
    if (award_scaleform == null) return;

    if (data.TXDLib.length > 0 && data.TXDName.length > 0) API.drawGameTexture(data.TXDLib, data.TXDName, 0, 0, 0, 0, 0, 0, 0, 0, 0); // just to be sure

    if (!award_scaleform.IsLoaded)
    {
        API.after(100, "configureScaleform", data);
        return;
    }

    award_scaleform.CallFunction("SHOW_AWARD_AND_MESSAGE", "Award", data.Name, data.TXDLib, data.TXDName, data.Description, colors[data.TXDColor]);
    API.playSoundFrontEnd("Mission_Pass_Notify", "DLC_HEISTS_GENERAL_FRONTEND_SOUNDS");

    display_scaleform = true;
    scaleform_time = API.getGlobalTime();
}

API.onUpdate.connect(function() {
    if (award_scaleform == null || !display_scaleform) return;

    if (API.getGlobalTime() - scaleform_time < display_time) {
        award_scaleform.Render2D();
    } else {
        award_scaleform.Dispose();

        award_scaleform = null;
        display_scaleform = false;
        scaleform_time = 0;
    }
});

API.onServerEventTrigger.connect(function(event, args) {
    if (event == "Award_Unlocked")
    {
        var data = JSON.parse(args[0]);

        award_scaleform = API.requestScaleform("mp_award_freemode");
        API.after(100, "configureScaleform", data);

        if (data.TXDLib.length > 0 && data.TXDName.length > 0) API.drawGameTexture(data.TXDLib, data.TXDName, 0, 0, 0, 0, 0, 0, 0, 0, 0); // load txd lib
    }
});
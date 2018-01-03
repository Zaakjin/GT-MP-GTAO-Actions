/// <reference path="types-gt-mp/Definitions/index.d.ts" />
var actions_menu = null;
var action_key_last_press = API.getGameTime();
var action_key_pressed_times = 0;
var key_status = 0;
var action_key = 171;
var action_gamepad_key1 = 28;
var action_gamepad_key2 = 29;
var triggered = false;
var trigger_delay = 300;

API.onServerEventTrigger.connect((eventName, args) => {
    switch (eventName) {
    case "ReceiveActions":
        if (actions_menu == null) {
            const data = JSON.parse(args[0]);

            actions_menu = API.createMenu("Actions", "~b~Select a action.", 0, 0, 6);
            for (let i = 0; i < data.length; i++) actions_menu.AddItem(API.createMenuItem(data[i], ""));
            actions_menu.MenuItems[0].SetLeftBadge(BadgeStyle.Tick);

            actions_menu.OnItemSelect.connect((menu, item, index) => {
                API.triggerServerEvent("SetAction", index);
            });

            actions_menu.RefreshIndex();
            actions_menu.Visible = true;
        }
        break;

    case "SetCurrentActionIndex":
        if (actions_menu == null) return;

        for (let i = 0; i < actions_menu.MenuItems.Count; i++) actions_menu.MenuItems[i].SetLeftBadge(BadgeStyle.None);
        actions_menu.MenuItems[args[0]].SetLeftBadge(BadgeStyle.Tick);
        break;
    case "RequestAnimationLength":
        API.loadAnimationDict(args[0]);
        let animTime = API.getAnimTotalTime(args[0], args[1]);
        animTime += 0.0000001;
        API.triggerServerEvent("RequestAnimationLength", animTime);
        break;
    }
});
API.onUpdate.connect(() => {
    if (!API.isChatOpen() && API.getPlayerVehicleSeat(API.getLocalPlayer()) === -3 && (API.isControlJustPressed(action_key) || (API.isControlJustPressed(action_gamepad_key1) && API.isControlJustPressed(action_gamepad_key2)))) {
        if (API.getGameTime() - action_key_last_press < trigger_delay) {
            action_key_pressed_times++;
        } else {
            action_key_pressed_times = 1;
        }
        action_key_last_press = API.getGameTime();
    }
    if (action_key_pressed_times > 0 && API.getGameTime() - action_key_last_press > trigger_delay) {
        if (action_key_pressed_times === 1) {
            if ((API.isControlPressed(action_key) || (API.isControlPressed(action_gamepad_key1) && API.isControlPressed(action_gamepad_key2))) && !triggered) {
                API.triggerServerEvent("StartUpperAnim");
                triggered = true;
            } else if (!(API.isControlPressed(action_key) || (API.isControlPressed(action_gamepad_key1) && API.isControlPressed(action_gamepad_key2)))) {
                if (triggered) {
                    API.triggerServerEvent("StopUpperAnim");
                    triggered = false;
                } else {
                    API.triggerServerEvent("StartUpperAnim");
                    API.after(50, "stopAnim");
                }
                action_key_pressed_times = 0;
            }
        } else if (action_key_pressed_times > 1) {
            API.triggerServerEvent("PlayCelebAnim");
            action_key_pressed_times = 0;
        }
    }
});

function stopAnim() {
    API.triggerServerEvent("StopUpperAnim");
}

API.onKeyDown.connect((e, key) => {
    if (key.KeyCode === Keys.F5) {
        if (API.isChatOpen()) return;

        if (actions_menu == null) {
            API.triggerServerEvent("RequestActions");
        } else {
            actions_menu.Visible = !actions_menu.Visible;
        }

    }
});
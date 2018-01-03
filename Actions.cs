using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GrandTheftMultiplayer.Server.API;
using GrandTheftMultiplayer.Server.Constant;
using GrandTheftMultiplayer.Server.Elements;
using GrandTheftMultiplayer.Server.Managers;
using Newtonsoft.Json;

namespace OnlineActions
{
    [Flags]
    public enum AnimationFlag
    {
        Normal,
        Loop = 1,
        StayInEndFrame = 2,
        UpperBodyOnly = 16,
        AllowControl = 32,
        CancelableWithMovement = 128,
        Inverted = 256,
        InvertedArms = 8192,
        RagdollOnCollision = 4194304
    }

    public class Action
    {
        public Action()
        {
        }

        public Action(string name, Animation upperEnterAnimation, Animation upperLoopAnimation, Animation upperExitAnimation, Animation femaleCelebAnimation, Animation maleCelebAnimation)
        {
            Name = name;
            UpperEnterAnimation = upperEnterAnimation;
            UpperLoopAnimation = upperLoopAnimation;
            UpperExitAnimation = upperExitAnimation;
            FemaleCelebAnimation = femaleCelebAnimation;
            MaleCelebAnimation = maleCelebAnimation;
        }

        public string Name { get; set; }

        public Animation UpperEnterAnimation { get; set; }
        public Animation UpperLoopAnimation { get; set; }
        public Animation UpperExitAnimation { get; set; }

        public Animation FemaleCelebAnimation { get; set; }

        public Animation MaleCelebAnimation { get; set; }
    }

    public class Animation
    {
        public Animation()
        {
        }

        public Animation(string dictionary, string name, int length)
        {
            Dictionary = dictionary;
            Name = name;
            Length = length;
        }

        public string Dictionary { get; set; }
        public string Name { get; set; }
        public int Length { get; set; }
    }

    public class ActionController : Script
    {
        private readonly Dictionary<Client, bool> ActionActive = new Dictionary<Client, bool>();
        private List<Action> Actions = new List<Action>();
        private string savePath;
        private string savePathGenerator;
        private readonly Dictionary<Client, int> SelectedAction = new Dictionary<Client, int>();

        public ActionController()
        {
            API.onClientEventTrigger += Actions_onClientEventTrigger;
            API.onResourceStart += Actions_onResourceStart;
            API.onPlayerConnected += Actions_onPlayerConnected;
            API.onPlayerDisconnected += Actions_onPlayerDisconnected;
        }

        private void Actions_onPlayerDisconnected(Client player, string reason)
        {
            ActionActive.Remove(player);
            SelectedAction.Remove(player);
        }

        private void Actions_onPlayerConnected(Client player)
        {
            ActionActive.Add(player, false);
            SelectedAction.Add(player, 0);
        }

        private void Actions_onResourceStart()
        {
            savePath = API.getResourceFolder() + Path.DirectorySeparatorChar + "actions" + Path.DirectorySeparatorChar + "actions.json";
            savePathGenerator = API.getResourceFolder() + Path.DirectorySeparatorChar + "actions" + Path.DirectorySeparatorChar + "actions_generated.json";
            foreach (var player in API.getAllPlayers())
            {
                SelectedAction.Add(player, 0);
                ActionActive.Add(player, false);
            }
            using (var fs = new FileStream(savePath, FileMode.Open))
            {
                var array = new byte[fs.Length];
                fs.Read(array, 0, array.Length);
                var str = new UTF8Encoding(true).GetString(array);
                Actions = JsonConvert.DeserializeObject<List<Action>>(str);
            }
        }

        private void Actions_onClientEventTrigger(Client player, string eventName, params object[] args)
        {
            switch (eventName)
            {
                case "StartUpperAnim":
                    ActionActive[player] = true;
                    API.playPlayerAnimation(player, (int) AnimationFlag.UpperBodyOnly, Actions[SelectedAction[player]].UpperEnterAnimation.Dictionary, Actions[SelectedAction[player]].UpperEnterAnimation.Name);
                    API.sleep(Actions[SelectedAction[player]].UpperEnterAnimation.Length - 200);
                    ActionActive[player] = false;
                    API.playPlayerAnimation(player, (int) (AnimationFlag.Loop | AnimationFlag.UpperBodyOnly), Actions[SelectedAction[player]].UpperLoopAnimation.Dictionary, Actions[SelectedAction[player]].UpperLoopAnimation.Name);
                    break;
                case "StopUpperAnim":
                    while (!ActionActive.ContainsKey(player) || ActionActive[player])
                        API.sleep(5);
                    API.playPlayerAnimation(player, (int) AnimationFlag.UpperBodyOnly, Actions[SelectedAction[player]].UpperExitAnimation.Dictionary, Actions[SelectedAction[player]].UpperExitAnimation.Name);
                    API.sleep(Actions[SelectedAction[player]].UpperExitAnimation.Length + 50);
                    API.stopPlayerAnimation(player);
                    break;
                case "PlayCelebAnim":
                    if ((PedHash) player.model == PedHash.FreemodeFemale01)
                        API.playPlayerAnimation(player, (int) AnimationFlag.Normal, Actions[SelectedAction[player]].FemaleCelebAnimation.Dictionary, Actions[SelectedAction[player]].FemaleCelebAnimation.Name);
                    else
                        API.playPlayerAnimation(player, (int) AnimationFlag.Normal, Actions[SelectedAction[player]].MaleCelebAnimation.Dictionary, Actions[SelectedAction[player]].MaleCelebAnimation.Name);
                    break;
                case "RequestActions":
                    player.triggerEvent("ReceiveActions", API.toJson(Actions.Select(m => m.Name)));
                    break;

                case "SetAction":
                    if (args.Length < 1) return;

                    var id = Convert.ToInt32(args[0]);
                    if (id < 0 || id >= Actions.Count) return;
                    SelectedAction[player] = id;
                    player.triggerEvent("SetCurrentActionIndex", id);
                    player.sendChatMessage("Action set to: ~y~" + Actions[id].Name + ".");
                    break;
            }
        }

        [Command]
        public void genactions(Client sender)
        {
            var emotions_names = new[] {"finger", "rock", "salute", "wank", "blow_kiss", "air_shagging", "dock", "knuckle_crunch", "slow_clap", "face_palm", "thumbs_up", "jazz_hands", "nose_pick", "air_guitar", "wave", "surrender", "shush", "photography", "dj", "air_synth", "no_way", "chicken_taunt", "chin_brush", "finger_kiss", "peace", "you_loco", "freakout", "thumb_on_ears", "v_sign"};

            var ActionsGen = new List<Action>();
            foreach (var emotionName in emotions_names)
            {
                var UpperDictionary = "anim@mp_player_intupper" + emotionName;
                var FemaleCelebDctionary = "anim@mp_player_intcelebrationfemale@" + emotionName;
                var MaleCelebDctionary = "anim@mp_player_intcelebrationmale@" + emotionName;
                ActionsGen.Add(new Action(
                    emotionName,
                    new Animation(UpperDictionary, "enter", Helper.GetAnimationTimeFromPlayer(sender, UpperDictionary, "enter")),
                    new Animation(UpperDictionary, "idle_a", Helper.GetAnimationTimeFromPlayer(sender, UpperDictionary, "idle_a")),
                    new Animation(UpperDictionary, "exit", Helper.GetAnimationTimeFromPlayer(sender, UpperDictionary, "exit")),
                    new Animation(FemaleCelebDctionary, emotionName, Helper.GetAnimationTimeFromPlayer(sender, FemaleCelebDctionary, emotionName)),
                    new Animation(MaleCelebDctionary, emotionName, Helper.GetAnimationTimeFromPlayer(sender, MaleCelebDctionary, emotionName))
                ));
            }
            if (!Directory.Exists(API.getResourceFolder() + Path.DirectorySeparatorChar + "emotions")) Directory.CreateDirectory(API.getResourceFolder() + Path.DirectorySeparatorChar + "emotions");
            using (var fs = new FileStream(savePathGenerator, FileMode.Create))
            {
                byte[] info;
                try
                {
                    info = new UTF8Encoding(true).GetBytes(JsonConvert.SerializeObject(ActionsGen, Formatting.Indented));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                fs.Write(info, 0, info.Length);
            }
        }
    }
}
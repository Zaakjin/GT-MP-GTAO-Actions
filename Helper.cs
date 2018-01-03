using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GrandTheftMultiplayer.Server.API;
using GrandTheftMultiplayer.Server.Elements;

namespace OnlineActions
{
    internal class Helper : Script
    {
        private static readonly Dictionary<Client, Dictionary<string, object>> AnswersDictionary = new Dictionary<Client, Dictionary<string, object>>();

        public Helper()
        {
            API.onClientEventTrigger += RequestAnswerHandler;
        }

        private void RequestAnswerHandler(Client sender, string eventName, params object[] args)
        {
            if (!AnswersDictionary.ContainsKey(sender)) return;
            if (!AnswersDictionary[sender].ContainsKey(eventName)) return;
            AnswersDictionary[sender][eventName] = args[0];
        }

        public static T RequestAnswer<T>(Client client, string eventName, params object[] arguments)
        {
            if (!AnswersDictionary.ContainsKey(client)) AnswersDictionary.Add(client, new Dictionary<string, object>());
            if (AnswersDictionary[client].ContainsKey(eventName)) AnswersDictionary[client][eventName] = null;
            else AnswersDictionary[client].Add(eventName, null);

            API.shared.triggerClientEvent(client, eventName, arguments);

            var task = Task<T>.Factory.StartNew(() =>
            {
                while (AnswersDictionary[client][eventName] == null)
                    API.shared.sleep(1);
                return (T) AnswersDictionary[client][eventName];
            });
            return task.Result;
        }

        public static int GetAnimationTimeFromPlayer(Client player, string dict, string name)
        {
            try
            {
                return Convert.ToInt32(RequestAnswer<float>(player, "RequestAnimationLength", dict, name) * 1000);
            }
            catch (Exception e)
            {
                return 0;
            }
        }
    }
}
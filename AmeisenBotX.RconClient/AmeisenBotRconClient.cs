﻿using AmeisenBotX.RconClient.Enums;
using AmeisenBotX.RconClient.Messages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace AmeisenBotX.RconClient
{
    public class AmeisenBotRconClient
    {
        public AmeisenBotRconClient(string endpoint, string name, string wowRace, string wowGender, string wowClass, string wowRole, string image = "", string guid = "", bool validateCertificate = false)
        {
            Endpoint = endpoint;

            KeepaliveEnpoint = new Uri($"{Endpoint}/api/keepalive");
            RegisterEnpoint = new Uri($"{Endpoint}/api/register");
            DataEnpoint = new Uri($"{Endpoint}/api/data");
            ImageEnpoint = new Uri($"{Endpoint}/api/image");
            ActionEnpoint = new Uri($"{Endpoint}/api/action");

            if (!validateCertificate)
            {
                HttpClientHandler handler = new HttpClientHandler
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true
                };

                HttpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(1) };
            }
            else
            {
                HttpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(1) };
            }

            Guid = guid.Length > 0 ? guid : System.Guid.NewGuid().ToString();

            RegisterMessage = new RegisterMessage()
            {
                Guid = Guid,
                Name = name,
                Race = wowRace,
                Gender = wowGender,
                Class = wowClass,
                Role = wowRole,
                Image = image
            };
        }

        public string Endpoint { get; set; }

        public string Guid { get; set; }

        public bool NeedToRegister { get; private set; } = true;

        public List<ActionType> PendingActions { get; private set; } = new List<ActionType>();

        public RegisterMessage RegisterMessage { get; }

        private Uri ActionEnpoint { get; }

        private Uri DataEnpoint { get; }

        private HttpClient HttpClient { get; set; }

        private Uri ImageEnpoint { get; }

        private Uri KeepaliveEnpoint { get; }

        private Uri RegisterEnpoint { get; }

        public bool KeepAlive()
        {
            using StringContent content = new StringContent(JsonConvert.SerializeObject(new KeepAliveMessage() { Guid = Guid }), Encoding.UTF8, "application/json");
            HttpResponseMessage dataResponse = HttpClient.PostAsync(KeepaliveEnpoint, content).Result;

            if (dataResponse.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                NeedToRegister = true;
                return false;
            }
        }

        public bool PullPendingActions()
        {
            HttpResponseMessage dataResponse = HttpClient.GetAsync(ActionEnpoint).Result;

            if (dataResponse.IsSuccessStatusCode)
            {
                PendingActions = JsonConvert.DeserializeObject<List<ActionType>>(dataResponse.Content.ReadAsStringAsync().Result);
                return true;
            }
            else
            {
                NeedToRegister = true;
                return false;
            }
        }

        public bool Register()
        {
            using StringContent content = new StringContent(JsonConvert.SerializeObject(RegisterMessage), Encoding.UTF8, "application/json");
            HttpResponseMessage registerResponse = HttpClient.PostAsync(RegisterEnpoint, content).Result;

            NeedToRegister = false;

            if (registerResponse.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SendData(DataMessage dataMessage)
        {
            if (dataMessage == null)
            {
                return false;
            }

            dataMessage.Guid = Guid;

            using StringContent content = new StringContent(JsonConvert.SerializeObject(dataMessage), Encoding.UTF8, "application/json");
            HttpResponseMessage dataResponse = HttpClient.PostAsync(DataEnpoint, content).Result;

            if (dataResponse.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                NeedToRegister = true;
                return false;
            }
        }

        public bool SendImage(string image)
        {
            using StringContent content = new StringContent(JsonConvert.SerializeObject(new ImageMessage() { Guid = Guid, Image = image }), Encoding.UTF8, "application/json");
            HttpResponseMessage dataResponse = HttpClient.PostAsync(ImageEnpoint, content).Result;

            if (dataResponse.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                NeedToRegister = true;
                return false;
            }
        }
    }
}
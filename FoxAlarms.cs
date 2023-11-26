using Life;
using Life.AreaSystem;
using Life.CheckpointSystem;
using Life.DB;
using Life.MainMenuSystem;
using Life.Network;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static System.Net.WebRequestMethods;
using UIPanel = Life.UI.UIPanel;

namespace FoxAlarms
{
    public class FoxAlarms : Plugin
    {

        public string UrlWebHook { get; set; }
        public int AlarmPrice { get; set; }
        public List<int> SocietyConcerned { get; set; }
        public UIPanel menuAlarmPro;
        public string alarmSystemName = "VeryFox v1.2";

        public CacheArea _cache;

        public FoxAlarms(IGameAPI api) : base(api)
        {
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();

            UrlWebHook = "https://discord.com/api/webhooks/1178106678143615037/Ayhh28Gok3hGMCoGTZu3CqyrjR7lVmvL6vUGq8dVT91b3mYGaPwGuuJ6ubkz4gCG6-RD";

            var configFilePath = Path.Combine(pluginsPath, "FoxAlarms/config.json");

            Config configuration = ChargerConfiguration(configFilePath);

            AlarmPrice = configuration.AlarmPrice;
            SocietyConcerned = configuration.SecuritySociety;

            SChatCommand commandAlarmPro = new SChatCommand("/alarmPro", "Menu de gestion des alarmes pour professionnel.", "/alarmPro", (player, argsCmd) =>
            {
                SpawnMenuAlarmPro(player);
            });

            _cache = new CacheArea();
        }


        public override void OnPlayerInput(Player player, KeyCode keyCode, bool onUI)
        {
            base.OnPlayerInput(player, keyCode, onUI);

            if (keyCode == KeyCode.P && !onUI)
            {
                SpawnMenuAlarmPro(player);
            }
        }

        static Config ChargerConfiguration(string cheminFichierConfig)
        {
            string jsonConfig = System.IO.File.ReadAllText(cheminFichierConfig);
            return JsonConvert.DeserializeObject<Config>(jsonConfig);
        }

        private void SpawnMenuAlarmPro(Player player)
        {
            if (IsSecuritySociety(player))
            {
                menuAlarmPro = new UIPanel($"FoxAlarm", UIPanel.PanelType.Tab)
                                    .AddTabLine($"Installer une alarme ({AlarmPrice})", (ui) =>
                                    {
                                        if (player.biz.Bank >= AlarmPrice)
                                        {
                                            player.biz.Bank -= AlarmPrice;
                                            player.Notify(alarmSystemName, "Installation de l'alarme effectué. Le prix a été facturé à votre entreprise.", NotificationManager.Type.Success);
                                            RegisterCheckpoint(player, new Vector3(player.character.LastPosX, player.character.LastPosY, player.character.LastPosZ));
                                            //RegisterCheckpoint(player.GetClosestPlayer(), new Vector3(player.character.LastPosX, player.character.LastPosY, player.character.LastPosZ));
                                        }
                                        else
                                        {
                                            player.Notify(alarmSystemName, "Impossible d'installer l'alarme, votre entreprise ne dispose pas des fonds néccéssaire.", NotificationManager.Type.Error);
                                        }
                                    })
                                    .AddTabLine("Test l'alarme", (ui) =>
                                    {
                                        SendWebhook(UrlWebHook, "Un agent fait un test d'alarme au terrain : " + player.setup.areaId);
                                        player.ClosePanel(ui);
                                    })
                                    .AddTabLine("DevTest", (ui) =>
                                    {

                                    })
                                    .AddButton("Fermer", (ui) =>
                                    {
                                        player.ClosePanel(ui);
                                    })
                                    .AddButton("Sélectionner", (ui) =>
                                    {
                                        ui.SelectTab();
                                    });

                player.ShowPanelUI(menuAlarmPro);
            }
        }

        private bool IsSecuritySociety(Player player)
        {
            if (player.HasBiz())
            {
                if (SocietyConcerned.Contains(player.biz.Id))
                {
                    return true;
                }
            }
            return false;
        }

        public override void OnPlayerEnterArea(Player player, AreaBox area)
        {
            base.OnPlayerEnterArea(player, area);
            if (!player.serviceAdmin)
            {
                if (!_cache.IsAreaInCache((int) area.areaId))
                {
                    SendSignalArea(player, area);
                    _cache.AjouterNumero((int) area.areaId);
                }
            }
        }

        private async void SendSignalArea(Player player, AreaBox area)
        {
            List<Areas> list = await (from m in LifeDB.db.Table<Areas>()
                                      where m.AreaId == area.areaId
                                      select m).ToListAsync();
            var areaInDB = list.First();

            string permissions = areaInDB.Permissions;

            OwnershipData ownershipData = JsonConvert.DeserializeObject<OwnershipData>(permissions);

            var proprio = await Life.DB.LifeDB.FetchCharacterName(ownershipData.Owner.CharacterId);

            SendWebhook(UrlWebHook, $" ATTENTION ! Une personne à bien pénétré dans la propriété de {proprio} ({area.areaId})");
        }

        static async Task SendWebhook(string webhookUrl, string content)
        {
            using (HttpClient client = new HttpClient())
            {
                var payload = new
                {
                    content = content
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

                var data = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(webhookUrl, data);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Erreur lors de l'envoi du webhook. Statut : {response.StatusCode}");
                }
            }
        }

        public void RegisterCheckpoint(Player player, Vector3 victor)
        {
            NCheckpoint policeArmuryCheckpoint = new NCheckpoint(player.netId, victor, (checkpoint) =>
            {
                UIPanel onPointAlarmPanel = new UIPanel("Armury", UIPanel.PanelType.Tab)
                .SetText("Récupérez ou rendez votre équipement. Attention, tout équipement perdu ne sera redonné.")
                .AddTabLine("Status :", (ui) =>
                {
                    player.Notify(alarmSystemName, "Votre alarme est : <status>");
                })
                .AddTabLine("Allumer l'alarme", (ui) =>
                {
                    player.ClosePanel(ui);
                })
                .AddTabLine("Désactiver l'alarme", (ui) =>
                {
                    player.ClosePanel(ui);
                })
                .AddButton("Sélectionner", (ui) =>
                {
                    ui.SelectTab();
                    player.ClosePanel(ui);
                })
                .AddButton("Fermer", (ui) =>
                {
                    player.ClosePanel(ui);
                });

                if (IsSecuritySociety(player))
                {
                    onPointAlarmPanel.AddTabLine("Désinstaller l'alarme", (ui) =>
                    {
                        player.ClosePanel(ui);
                    });
                }

                player.ShowPanelUI(onPointAlarmPanel);
            });

            player.CreateCheckpoint(policeArmuryCheckpoint);
        }
    }

    public class OwnershipData
    {
        [JsonProperty("owner")]
        public OwnerData Owner { get; set; }

        [JsonProperty("coOwners")]
        public CoOwnerData[] CoOwners { get; set; }
    }

    public class OwnerData
    {
        [JsonProperty("groupId")]
        public int GroupId { get; set; }

        [JsonProperty("characterId")]
        public int CharacterId { get; set; }
    }

    public class CoOwnerData
    {
        // Si vous avez des propriétés spécifiques pour les co-owners, ajoutez-les ici
    }

    class Config
    {
        public int AlarmPrice { get; set; }
        public List<int> SecuritySociety { get; set; }
    }
}

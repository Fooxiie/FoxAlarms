using FoxAlarms.DataBase;
using Life;
using Life.AreaSystem;
using Life.BizSystem;
using Life.CheckpointSystem;
using Life.DB;
using Life.InventorySystem;
using Life.MainMenuSystem;
using Life.Network;
using Life.PermissionSystem;
using Mirror;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using static System.Net.WebRequestMethods;
using UIPanel = Life.UI.UIPanel;

namespace FoxAlarms
{
    public class FoxAlarms : Plugin
    {

        public int AlarmPrice { get; set; }
        public List<int> SocietyConcerned { get; set; }
        public string messageNotifIntervention { get; set; }
        public string logDiscordAdress { get; set; }
        public string logDiscordSecret { get; set; }
        public int accessAlarmAuth { get; set; }

        public UIPanel menuAlarmPro;
        public string alarmSystemName = "VeryFox v1.2";

        public static string DbPath = "FoxAlarms/data.db";

        public List<Intervention> interventions = new List<Intervention>();

        public List<NCheckpoint> checkpoints = new List<NCheckpoint>();

        public FoxAlarms(IGameAPI api) : base(api)
        {
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();

            InitDataBase();

            var configFilePath = Path.Combine(pluginsPath, "FoxAlarms/config.json");

            Config configuration = ChargerConfiguration(configFilePath);

            AlarmPrice = configuration.AlarmPrice;
            SocietyConcerned = configuration.SecuritySociety;
            messageNotifIntervention = configuration.messageNotifIntervention;
            logDiscordAdress = configuration.logDiscordAdress;
            logDiscordSecret = configuration.logDiscordSecret;
            accessAlarmAuth = configuration.accessAlarmAuth;
            SetupCommand();

            NetworkAreaManager.instance.doors.Callback += Doors_Callback;
        }

        private void Doors_Callback(SyncList<DoorState>.Operation op, int itemIndex, DoorState oldItem, DoorState newItem) // trigger door open
        {
            InteractableDoor interactableDoor = LifeManager.instance.doors[newItem.guid];
            if (!interactableDoor.isGate && interactableDoor.isLockable && !interactableDoor.isAutomaticDoor)
            {
                if (oldItem.isLocked == true && newItem.isLocked == false)
                {
                    SendSignalArea((uint)LifeManager.instance.doors[newItem.guid].areaId);
                }
            }
        }

        private void SetupCommand()
        {
            SChatCommand commandAlarmPro = new SChatCommand("/alarmPro", "Menu de gestion des alarmes pour professionnel.", "/alarmPro", (player, argsCmd) =>
            {
                SpawnMenuAlarmPro(player);
            });

            SChatCommand creditCommand = new SChatCommand("/creditsFox", "Commande de crédit obligatoire du au coté OpenSource du plugins (Ne pas retirer)", "/creditsFox", (player, argCmd) =>
            {
                UIPanel panelCredit = new UIPanel("FoxAlarms developped by Fooxiie & Rémi (the modo), all right reserved", UIPanel.PanelType.Text);
                panelCredit.text = "Ce plugin du métier sécurité a été développer par Fooxiie.";
                panelCredit.AddButton("Fermer", (ui) =>
                {
                    player.ClosePanel(ui);
                });
                player.ShowPanelUI(panelCredit);
            });

            commandAlarmPro.Register();
            creditCommand.Register();
        }

        private async void InitDataBase()
        {
            await LeManipulateurDeLaDonnee.Init(Path.Combine(pluginsPath, DbPath));
        }

        public override void OnPlayerInput(Player player, KeyCode keyCode, bool onUI)
        {
            base.OnPlayerInput(player, keyCode, onUI);

            if (keyCode == KeyCode.P && !onUI)
            {
                SpawnMenuAlarmPro(player);
            }
        }

        public override void OnPlayerSpawnCharacter(Player player, NetworkConnection conn, Characters character)
        {
            base.OnPlayerSpawnCharacter(player, conn, character);
            LoadAllChecKPoints(player);
        }

        private async void LoadAllChecKPoints(Player player)
        {
            List<DataBase.AlarmModel> alarmModels = await LeManipulateurDeLaDonnee.GetAlarms();
            foreach (var item in alarmModels)
            {
                RegisterCheckpoint(player, new Vector3(item.posX, item.posY, item.posZ));
            }
        }

        static Config ChargerConfiguration(string cheminFichierConfig)
        {
            string jsonConfig = System.IO.File.ReadAllText(cheminFichierConfig);
            return JsonConvert.DeserializeObject<Config>(jsonConfig);
        }

        private async Task<Areas> WishAreaIAm(uint areaId)
        {
            List<Areas> list = await (from m in LifeDB.db.Table<Areas>()
                                      where m.AreaId == areaId
                                      select m).ToListAsync();
            var areaInDB = list.First();
            return areaInDB;
        }

        private void CreateAlarm(Player player)
        {
            if (player.biz.Bank >= AlarmPrice)
            {
                UIPanel panelNameAlarm = new UIPanel("Nommez le destinataire de l'alarme", UIPanel.PanelType.Input);
                panelNameAlarm.inputPlaceholder = "Nom prenom ou nom entreprise";
                panelNameAlarm.AddButton("Nommer", (ui) =>
                {
                    string inputText = ui.inputText;

                    // Display une image de work pour attendre
                    player.setup.TargetShowCenterText(alarmSystemName, "Vous installez une alarme..", 4);
                    player.biz.Bank -= AlarmPrice;
                    player.Notify(alarmSystemName, "Installation de l'alarme effectué. Le prix a été facturé à votre entreprise.", NotificationManager.Type.Success);

                    LeManipulateurDeLaDonnee.registerAlarm(player.setup.areaId,
                        player.setup.transform.position.x,
                        player.setup.transform.position.y,
                        player.setup.transform.position.z,
                        inputText.Trim());

                    // Give le checkpoint à tous les gens autour dans le cas ou ils sont de la secu ou du terrain
                    foreach (var aroundPlayer in Nova.server.Players)
                    {
                        RegisterCheckpoint(aroundPlayer, new Vector3(player.setup.transform.position.x, player.setup.transform.position.y, player.setup.transform.position.z));
                    }
                });

                player.ShowPanelUI(panelNameAlarm);
            }
            else
            {
                player.Notify(alarmSystemName, "Impossible d'installer l'alarme, votre entreprise ne dispose pas des fonds néccéssaire.", NotificationManager.Type.Error);
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

        private async void SendSignalArea(uint areaId)
        {
            AlarmModel alarm = await LeManipulateurDeLaDonnee.GetAlarm((int)areaId);

            if (alarm != null && alarm.status == 1)
            {
                StartCooldown(alarm);
            }
        }

        private async void StartCooldown(AlarmModel alarm)
        {
            await Task.Delay(10000);

            ReportIntrusion(alarm.areaId, alarm);
        }

        private async void ReportIntrusion(int areaId, AlarmModel alarm)
        {
            AlarmModel AlarmUpdated = await LeManipulateurDeLaDonnee.GetAlarm((int)areaId);
            if (AlarmUpdated.status != 0)
            {
                List<Areas> list = await (from m in LifeDB.db.Table<Areas>()
                                          where m.AreaId == areaId
                                          select m).ToListAsync();
                var areaInDB = list.First();

                string permissions = areaInDB.Permissions;

                OwnershipData ownershipData = JsonConvert.DeserializeObject<OwnershipData>(permissions);

                var proprio = await LifeDB.FetchCharacter(ownershipData.Owner.CharacterId);

                var clientName = alarm.name;

                SendWebhook(logDiscordAdress, $"{messageNotifIntervention} {clientName}");

                SendSMS(proprio, clientName);

                foreach (Player player in Nova.server.Players)
                {
                    if (player.HasBiz())
                    {
                        if (SocietyConcerned.Contains(player.biz.Id))
                        {
                            Nova.server.CreateInter(clientName,
                                $"Alerte une alarme VeryFox à été délenché !",
                                new Vector3(alarm.posX, alarm.posY, alarm.posZ),
                                player.biz.Id,
                                player);
                            return;
                        }
                    }
                }
            }
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
            NCheckpoint checkpointAlarm = new NCheckpoint(player.netId, victor, async (checkpoint) =>
            {
                AlarmModel alarm = await LeManipulateurDeLaDonnee.GetAlarm((int)player.setup.areaId);

                int tentative = 2;

                UIPanel askPassword = new UIPanel("Entrer le code de l'alarme", UIPanel.PanelType.Input)
                .AddButton("Confirmer", (action) =>
                {
                    string passwordEntered = action.inputText;

                    if (passwordEntered == "securite" && accessAlarmAuth == 1 && IsSecuritySociety(player))
                    {
                        MenuAlarmAuthentificated(player, alarm);
                    }
                    else
                    {
                        if (alarm.password == passwordEntered) // Si le code est bon
                        {
                            MenuAlarmAuthentificated(player, alarm);
                        }
                        else
                        {
                            tentative -= 1;

                            if (tentative == 0)
                            {
                                MakeAlarmRingForPlayer(player);
                            }
                            else
                            {
                                player.Notify(alarmSystemName, $"Mauvais code ! X_X il te reste {tentative} essais !", NotificationManager.Type.Error);
                            }
                        }
                    }
                })
                .AddButton("Retour", (action) =>
                {
                    player.ClosePanel(action);
                });

                player.ShowPanelUI(askPassword);
            });

            player.CreateCheckpoint(checkpointAlarm);
            checkpoints.Add(checkpointAlarm);
        }

        #region Menus
        private void SpawnMenuAlarmPro(Player player)
        {
            if (IsSecuritySociety(player))
            {
                menuAlarmPro = new UIPanel($"FoxAlarm", UIPanel.PanelType.Tab)
                                    .AddTabLine($"Installer une alarme ({AlarmPrice})", (ui) =>
                                    {
                                        player.ClosePanel(ui);

                                        CreateAlarm(player);
                                    })
                                    .AddTabLine("Déplacer l'alarme", (ui) =>
                                    {
                                        MooveAlarmPosition(player);
                                        player.ClosePanel(ui);
                                    })
                                    .AddTabLine("Intervenir sur site", (ui) =>
                                    {
                                        InterventionOnSite(player);
                                        player.ClosePanel(ui);
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

        private void MenuAlarmAuthentificated(Player player, AlarmModel alarm)
        {
            UIPanel onPointAlarmPanel = new UIPanel(alarmSystemName, UIPanel.PanelType.Tab);
            onPointAlarmPanel
                .AddTabLine("Status : " + (alarm.status == 1 ? "<color=#6aa84f>Activé</color>" : "<color=#fb4039>Désactivé</color>"), (ui) =>
                {
                    player.Notify(alarmSystemName, "Votre alarme est : " + (alarm.status == 1 ? "Activé" : "Désactivé"));
                })
                .AddTabLine("Allumer l'alarme", (ui) =>
                {
                    player.Notify(alarmSystemName, "Vous avez activée votre alarme. Veuillez sortir afin de pas déclencher l'alarme", NotificationManager.Type.Info);
                    player.ClosePanel(ui);
                    alarm.status = 1;
                    LeManipulateurDeLaDonnee.UpdateAlarm(alarm);
                    player.ClosePanel(ui);
                })
                .AddTabLine("Désactiver l'alarme", (ui) =>
                {
                    player.Notify(alarmSystemName, "Vous avez désactivé votre alarme !", NotificationManager.Type.Info);
                    alarm.status = 0;
                    LeManipulateurDeLaDonnee.UpdateAlarm(alarm);
                    player.ClosePanel(ui);
                })
                .AddTabLine("Modifier le code", (ui) =>
                {
                    EditAlarmCode(player, alarm);
                })
                .AddButton("Fermer", (ui) =>
                {
                    player.ClosePanel(ui);
                })
                .AddButton("Sélectionner", (ui) =>
                {
                    ui.SelectTab();
                });

            onPointAlarmPanel.subtitle = "Developed by Fooxiie & RémiGDV (the modo)";

            if (IsSecuritySociety(player))
            {
                onPointAlarmPanel.AddTabLine("Désinstaller l'alarme", (ui) =>
                {
                    LeManipulateurDeLaDonnee.DeleteAlarm(alarm);

                    ReWriteCheckPoints();

                    player.ClosePanel(ui);
                });
                onPointAlarmPanel.AddTabLine("Test l'alarme", (ui) =>
                {

                    SendWebhook(logDiscordAdress, "Un agent fait un test d'alarme au terrain : " + alarm.name);
                    player.ClosePanel(ui);
                })
                .AddTabLine("Modifier le nom", (ui) =>
                {
                    player.ClosePanel(ui);
                    UIPanel panelNameAlarm = new UIPanel("Nommez le destinataire de l'alarme", UIPanel.PanelType.Input);
                    panelNameAlarm.inputPlaceholder = "Nom prenom ou nom entreprise";
                    panelNameAlarm.AddButton("Nommer", (subui) =>
                    {

                        string inputText = subui.inputText;

                        alarm.name = inputText;

                        LeManipulateurDeLaDonnee.UpdateAlarm(alarm);

                        player.ClosePanel(subui);
                    });

                    player.ShowPanelUI(panelNameAlarm);
                });
            }

            player.ShowPanelUI(onPointAlarmPanel);
        }

        private void EditAlarmCode(Player player, AlarmModel alarm)
        {
            UIPanel editPassword = new UIPanel("Modification du code de l'alarme", UIPanel.PanelType.Input)
                .AddButton("Fermer", (ui) =>
                {
                    player.ClosePanel(ui);
                })
                .AddButton("Valider", (ui) =>
                {
                    var code = ui.inputText;
                    Regex regex = new Regex(@"^\d{4}$");
                    if (regex.IsMatch(code))
                    {
                        alarm.password = code;
                        LeManipulateurDeLaDonnee.UpdateAlarm(alarm);
                    }
                    player.ClosePanel(ui);
                });
            editPassword.inputPlaceholder = "Code à 4 chiffres";
            player.ShowPanelUI(editPassword);
        }
        #endregion

        #region Ring Alarm
        private async void MakeAlarmRingForPlayer(Player player)
        {
            player.setup.StartCoroutine(CallAlarmFor(player));
        }

        IEnumerator CallAlarmFor(Player player)
        {
            for (int i = 0; i < 10; i++)
            {
                player.setup.TargetPlayClairon(50);

                yield return new WaitForSeconds(0.5f);
            }
        }
        #endregion

        private async void InterventionOnSite(Player player)
        {
            var targetAreaID = player.setup.areaId;
            AlarmModel alarm = await LeManipulateurDeLaDonnee.GetAlarm((int)targetAreaID);

            if (alarm != null)
            {
                LifeArea area = Nova.a.areas.Where((x) => x.areaId == targetAreaID).First();

                area.AddCoOwner(new Entity
                {
                    characterId = player.character.Id
                });


                player.setup.StartCoroutine(removeTemporaryCoOwning(player, area));

                SendWebhook(logDiscordSecret, player.GetFullName() + " est intervenu chez : " + alarm.name);
            }
            else
            {
                player.Notify(alarmSystemName, "Terrain non pris en charge par votre entreprise.", NotificationManager.Type.Error);
            }
        }

        IEnumerator removeTemporaryCoOwning(Player player, LifeArea area)
        {
            yield return new WaitForSeconds(300f);
            LifeArea newArea = Nova.a.areas.Where((x) => x.areaId == area.areaId).First();
            foreach (var coOwner in newArea.permissions.coOwners)
            {
                if (coOwner.characterId == player.character.Id)
                {
                    newArea.DeleteCoOwner(coOwner);
                }
            }
        }

        private async void MooveAlarmPosition(Player player)
        {
            AlarmModel alarm = await LeManipulateurDeLaDonnee.GetAlarm((int)player.setup.areaId);

            if (alarm != null)
            {
                alarm.posX = player.setup.transform.position.x;
                alarm.posY = player.setup.transform.position.y;
                alarm.posZ = player.setup.transform.position.z;

                LeManipulateurDeLaDonnee.UpdateAlarm(alarm);

                RegisterCheckpoint(player, new Vector3(player.setup.transform.position.x, player.setup.transform.position.y, player.setup.transform.position.z));

                ReWriteCheckPoints();

                checkpoints.Clear();
                player.Notify(alarmSystemName, "Alarme déplacé !", NotificationManager.Type.Info);
            }
            else
            {
                player.Notify(alarmSystemName, "Aucune alarme disponible à déplacer.", NotificationManager.Type.Warning);
            }
        }

        private void ReWriteCheckPoints()
        {
            foreach (var p in Nova.server.GetAllInGamePlayers())
            {
                foreach (var myCheckpoint in checkpoints)
                {
                    p.DestroyCheckpoint(myCheckpoint);
                }
                LoadAllChecKPoints(p);
            }
        }

        private async void SendSMS(Characters proprio, string target)
        {

            LifeDB.SendSMS(proprio.Id, alarmSystemName, proprio.PhoneNumber, Nova.UnixTimeNow(), $"Votre alarme a été activé, votre fournisseur a été averti pour le terrain de {target}.");

            foreach (var player in Nova.server.GetAllInGamePlayers())
            {
                if (player.character.Id == proprio.Id)
                {
                    player.setup.TargetUpdateSMS();
                    player.Notify(alarmSystemName, "Vous avez reçu une nouvelle alerte par SMS !", NotificationManager.Type.Info);
                }
            }
        }
    }

    #region class Data
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
        [JsonProperty("groupId")]
        public int GroupId { get; set; }

        [JsonProperty("characterId")]
        public int CharacterId { get; set; }
    }

    class Config
    {
        public int AlarmPrice { get; set; }
        public List<int> SecuritySociety { get; set; }
        public string messageNotifIntervention { get; set; }
        public string logDiscordAdress { get; set; }
        public string logDiscordSecret { get; set; }
        public int accessAlarmAuth { get; set; }
    }
    #endregion
}

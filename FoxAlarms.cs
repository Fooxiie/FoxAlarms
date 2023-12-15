using FoxAlarms.DataBase;
using Life;
using Life.AreaSystem;
using Life.CheckpointSystem;
using Life.DB;
using Life.Network;
using Life.PermissionSystem;
using Mirror;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Life.UI;
using MyMenu.Entities;
using UnityEngine;
using UIPanel = Life.UI.UIPanel;

namespace FoxAlarms
{
    public class FoxAlarms : Plugin
    {
        private int AlarmPrice { get; set; }
        private string messageNotifIntervention { get; set; }
        private string logDiscordAdress { get; set; }
        private string logDiscordSecret { get; set; }
        private int accessAlarmAuth { get; set; }

        private UIPanel menuAlarmPro;
        private string alarmSystemName = "VeryFox v1.2";

        private static string DbPath = "FoxAlarms/data.db";

        private List<NCheckpoint> checkpoints = new List<NCheckpoint>();
        private Section _section;

        public FoxAlarms(IGameAPI api) : base(api)
        {
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();

            InitDataBase();

            var configFilePath = Path.Combine(pluginsPath, "FoxAlarms/config.json");

            var configuration = ChargerConfiguration(configFilePath);

            AlarmPrice = configuration.AlarmPrice;
            messageNotifIntervention = configuration.messageNotifIntervention;
            logDiscordAdress = configuration.logDiscordAdress;
            logDiscordSecret = configuration.logDiscordSecret;
            accessAlarmAuth = configuration.accessAlarmAuth;
            SetupCommand();

            NetworkAreaManager.instance.doors.Callback += Doors_Callback;

            _section = new Section(Section.GetSourceName(), "VeryFox", "v2.0", "Fooxiie", onlyAdmin: true);
            Action<UIPanel> action = ui => SpawnMenuAlarmPro(_section.GetPlayer(ui));

            _section.Line = new UITabLine("AucuneIncidence", action);
            _section.Insert();
        }

        private void Doors_Callback(SyncList<DoorState>.Operation op, int itemIndex, DoorState oldItem,
            DoorState newItem) // trigger door open
        {
            var interactableDoor = LifeManager.instance.doors[newItem.guid];
            if (interactableDoor.isGate || !interactableDoor.isLockable || interactableDoor.isAutomaticDoor) return;
            if (oldItem.isLocked == true && newItem.isLocked == false)
            {
                SendSignalArea((uint)LifeManager.instance.doors[newItem.guid].areaId);
            }
        }

        private void SetupCommand()
        {
            var commandAlarmPro = new SChatCommand("/alarmPro",
                "Menu de gestion des alarmes pour professionnel.", "/alarmPro",
                (player, argsCmd) => { SpawnMenuAlarmPro(player); });


            var creditCommand = new SChatCommand("/creditsFox",
                "Commande de crédit obligatoire du au coté OpenSource du plugins (Ne pas retirer)", "/creditsFox",
                (player, argCmd) =>
                {
                    var panelCredit =
                        new UIPanel("FoxAlarms developped by Fooxiie & Rémi (the modo), all right reserved",
                            UIPanel.PanelType.Text)
                        {
                            text = "Ce plugin du métier sécurité a été développer par Fooxiie."
                        };
                    panelCredit.AddButton("Fermer", (ui) => { player.ClosePanel(ui); });
                    player.ShowPanelUI(panelCredit);
                });

            commandAlarmPro.Register();
            creditCommand.Register();
        }

        private async void InitDataBase()
        {
            var pluginsDirectory = Path.Combine(pluginsPath, "FoxAlarms");
            if (!Directory.Exists(pluginsDirectory))
            {
                Directory.CreateDirectory(pluginsDirectory);
            }

            if (!File.Exists(Path.Combine(pluginsDirectory, "config.json")))
            {
                File.WriteAllText(Path.Combine(pluginsDirectory, "config.json"),
                    "{\n    \"AlarmPrice\": 1000,\n    \"messageNotifIntervention\": \"Alerte ! Une alarme a été déclenché chez\",\n    \"logDiscordSecret\": \"\",\n    \"logDiscordAdress\": \"\",\n    \"accessAlarmAuth\": 1\n}");
            }

            await LeManipulateurDeLaDonnee.Init(Path.Combine(pluginsPath, DbPath));
        }

        public override void OnPlayerSpawnCharacter(Player player, NetworkConnection conn, Characters character)
        {
            base.OnPlayerSpawnCharacter(player, conn, character);
            LoadAllChecKPoints(player);
        }

        private async void LoadAllChecKPoints(Player player)
        {
            var alarmModels = await LeManipulateurDeLaDonnee.GetAlarms();
            foreach (var item in alarmModels)
            {
                RegisterCheckpoint(player, new Vector3(item.posX, item.posY, item.posZ));
            }
        }

        private static Config ChargerConfiguration(string cheminFichierConfig)
        {
            var jsonConfig = System.IO.File.ReadAllText(cheminFichierConfig);
            return JsonConvert.DeserializeObject<Config>(jsonConfig);
        }

        private async Task<Areas> WishAreaIAm(uint areaId)
        {
            var list = await (from m in LifeDB.db.Table<Areas>()
                where m.AreaId == areaId
                select m).ToListAsync();
            var areaInDB = list.First();
            return areaInDB;
        }

        private void CreateAlarm(Player player)
        {
            if (player.biz.Bank >= AlarmPrice)
            {
                var panelNameAlarm = new UIPanel("Nommez le destinataire de l'alarme", UIPanel.PanelType.Input)
                {
                    inputPlaceholder = "Nom prenom ou nom entreprise"
                };
                panelNameAlarm.AddButton("Nommer", (ui) =>
                {
                    var inputText = ui.inputText;

                    // Display une image de work pour attendre
                    player.setup.TargetShowCenterText(alarmSystemName, "Vous installez une alarme..", 4);
                    player.biz.Bank -= AlarmPrice;
                    player.Notify(alarmSystemName,
                        "Installation de l'alarme effectué. Le prix a été facturé à votre entreprise.",
                        NotificationManager.Type.Success);

                    var transform = player.setup.transform;
                    var position = transform.position;
                    LeManipulateurDeLaDonnee.registerAlarm(player.setup.areaId,
                        position.x,
                        position.y,
                        position.z,
                        inputText.Trim());

                    // Give le checkpoint à tous les gens autour dans le cas ou ils sont de la secu ou du terrain
                    foreach (var aroundPlayer in Nova.server.Players)
                    {
                        var transform1 = player.setup.transform;
                        var position1 = transform1.position;
                        RegisterCheckpoint(aroundPlayer,
                            new Vector3(position1.x, position1.y,
                                position1.z));
                    }
                });

                player.ShowPanelUI(panelNameAlarm);
            }
            else
            {
                player.Notify(alarmSystemName,
                    "Impossible d'installer l'alarme, votre entreprise ne dispose pas des fonds néccéssaire.",
                    NotificationManager.Type.Error);
            }
        }

        private bool IsSecuritySociety(Player player)
        {
            return player.HasBiz() && _section.BizIdAllowed.Contains(player.biz.Id);
        }

        private async void SendSignalArea(uint areaId)
        {
            var alarm = await LeManipulateurDeLaDonnee.GetAlarm((int)areaId);

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
            var alarmUpdated = await LeManipulateurDeLaDonnee.GetAlarm((int)areaId);
            if (alarmUpdated.status == 0) return;
            var list = await (from m in LifeDB.db.Table<Areas>()
                where m.AreaId == areaId
                select m).ToListAsync();

            var areaInDB = list.First();

            var permissions = areaInDB.Permissions;

            var ownershipData = JsonConvert.DeserializeObject<OwnershipData>(permissions);

            var proprio = await LifeDB.FetchCharacter(ownershipData.Owner.CharacterId);

            var clientName = alarm.name;

            await SendWebhook(logDiscordAdress, $"{messageNotifIntervention} {clientName}");

            SendSms(proprio, clientName);

            foreach (var player in Nova.server.Players.Where(player => player.HasBiz())
                         .Where(player => _section.BizIdAllowed.Contains(player.biz.Id)))
            {
                Nova.server.CreateInter(clientName,
                    $"Alerte une alarme VeryFox à été délenché !",
                    new Vector3(alarm.posX, alarm.posY, alarm.posZ),
                    player.biz.Id,
                    player);
                return;
            }
        }

        private static async Task SendWebhook(string webhookUrl, string content)
        {
            using (var client = new HttpClient())
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

        private void RegisterCheckpoint(Player player, Vector3 victor)
        {
            var checkpointAlarm = new NCheckpoint(player.netId, victor, async (checkpoint) =>
            {
                var alarm = await LeManipulateurDeLaDonnee.GetAlarm((int)player.setup.areaId);

                var tentative = 2;

                var askPassword = new UIPanel("Entrer le code de l'alarme", UIPanel.PanelType.Input)
                    .AddButton("Confirmer", (action) =>
                    {
                        var passwordEntered = action.inputText;

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
                                    player.Notify(alarmSystemName,
                                        $"Mauvais code ! X_X il te reste {tentative} essais !",
                                        NotificationManager.Type.Error);
                                }
                            }
                        }
                    })
                    .AddButton("Retour", (action) => { player.ClosePanel(action); });

                player.ShowPanelUI(askPassword);
            });

            player.CreateCheckpoint(checkpointAlarm);
            checkpoints.Add(checkpointAlarm);
        }

        #region Menus

        private void SpawnMenuAlarmPro(Player player)
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
                .AddButton("Fermer", (ui) => { player.ClosePanel(ui); })
                .AddButton("Sélectionner", (ui) => { ui.SelectTab(); });

            player.ShowPanelUI(menuAlarmPro);
        }

        private void MenuAlarmAuthentificated(Player player, AlarmModel alarm)
        {
            var onPointAlarmPanel = new UIPanel(alarmSystemName, UIPanel.PanelType.Tab);
            onPointAlarmPanel
                .AddTabLine(
                    "Status : " + (alarm.status == 1
                        ? "<color=#6aa84f>Activé</color>"
                        : "<color=#fb4039>Désactivé</color>"),
                    (ui) =>
                    {
                        player.Notify(alarmSystemName,
                            "Votre alarme est : " + (alarm.status == 1 ? "Activé" : "Désactivé"));
                    })
                .AddTabLine("Allumer l'alarme", (ui) =>
                {
                    player.Notify(alarmSystemName,
                        "Vous avez activée votre alarme. Veuillez sortir afin de pas déclencher l'alarme",
                        NotificationManager.Type.Info);
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
                .AddTabLine("Modifier le code", (ui) => { EditAlarmCode(player, alarm); })
                .AddButton("Fermer", player.ClosePanel)
                .AddButton("Sélectionner", (ui) => { ui.SelectTab(); });

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
                        var panelNameAlarm =
                            new UIPanel("Nommez le destinataire de l'alarme", UIPanel.PanelType.Input)
                            {
                                inputPlaceholder = "Nom prenom ou nom entreprise"
                            };
                        panelNameAlarm.AddButton("Nommer", (subui) =>
                        {
                            var inputText = subui.inputText;

                            if (inputText == null) return;
                            alarm.name = inputText;

                            LeManipulateurDeLaDonnee.UpdateAlarm(alarm);

                            player.ClosePanel(subui);
                        });

                        player.ShowPanelUI(panelNameAlarm);
                    });
            }

            player.ShowPanelUI(onPointAlarmPanel);
        }

        private static void EditAlarmCode(Player player, AlarmModel alarm)
        {
            var editPassword = new UIPanel("Modification du code de l'alarme", UIPanel.PanelType.Input)
                .AddButton("Fermer", player.ClosePanel)
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
            for (var i = 0; i < 10; i++)
            {
                player.setup.TargetPlayClairon(50);

                yield return new WaitForSeconds(0.5f);
            }
        }

        #endregion

        private async void InterventionOnSite(Player player)
        {
            var targetAreaID = player.setup.areaId;
            var alarm = await LeManipulateurDeLaDonnee.GetAlarm((int)targetAreaID);

            if (alarm != null)
            {
                var area = Nova.a.areas.First(x => x.areaId == targetAreaID);

                area.AddCoOwner(new Entity
                {
                    characterId = player.character.Id
                });


                player.setup.StartCoroutine(removeTemporaryCoOwning(player, area));

                await SendWebhook(logDiscordSecret, player.GetFullName() + " est intervenu chez : " + alarm.name);
            }
            else
            {
                player.Notify(alarmSystemName, "Terrain non pris en charge par votre entreprise.",
                    NotificationManager.Type.Error);
            }
        }

        IEnumerator removeTemporaryCoOwning(Player player, LifeArea area)
        {
            yield return new WaitForSeconds(300f);
            var newArea = Nova.a.areas.First(x => x.areaId == area.areaId);
            foreach (var coOwner in newArea.permissions.coOwners.Where(coOwner =>
                         coOwner.characterId == player.character.Id))
            {
                newArea.DeleteCoOwner(coOwner);
            }
        }

        private async void MooveAlarmPosition(Player player)
        {
            var alarm = await LeManipulateurDeLaDonnee.GetAlarm((int)player.setup.areaId);

            if (alarm != null)
            {
                var transform = player.setup.transform;
                var position = transform.position;
                alarm.posX = position.x;
                alarm.posY = position.y;
                alarm.posZ = position.z;

                LeManipulateurDeLaDonnee.UpdateAlarm(alarm);

                RegisterCheckpoint(player,
                    new Vector3(player.setup.transform.position.x, position.y,
                        position.z));

                ReWriteCheckPoints();

                checkpoints.Clear();
                player.Notify(alarmSystemName, "Alarme déplacé !", NotificationManager.Type.Info);
            }
            else
            {
                player.Notify(alarmSystemName, "Aucune alarme disponible à déplacer.",
                    NotificationManager.Type.Warning);
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

        private async void SendSms(Characters proprio, string target)
        {
            await LifeDB.SendSMS(proprio.Id, "70", proprio.PhoneNumber, Nova.UnixTimeNow(),
                $"Votre alarme a été activé, votre fournisseur a été averti pour le terrain de {target}.");

            var contacts = await LifeDB.FetchContacts(proprio.Id);
            var contactPub = contacts.contacts.Where(contact => contact.number == "70").ToList();
            if (contactPub.Count == 0)
            {
                await LifeDB.CreateContact(proprio.Id, "70", alarmSystemName);
            }
            
            foreach (var player in Nova.server.GetAllInGamePlayers().Where(player => player.character.Id == proprio.Id))
            {
                player.setup.TargetUpdateSMS();
                player.Notify(alarmSystemName, "Vous avez reçu une nouvelle alerte par SMS !",
                    NotificationManager.Type.Info);
            }
        }
    }

    #region class Data

    public class OwnershipData
    {
        [JsonProperty("owner")] public OwnerData Owner { get; set; }

        [JsonProperty("coOwners")] public CoOwnerData[] CoOwners { get; set; }
    }

    public class OwnerData
    {
        [JsonProperty("groupId")] public int GroupId { get; set; }

        [JsonProperty("characterId")] public int CharacterId { get; set; }
    }

    public class CoOwnerData
    {
        [JsonProperty("groupId")] public int GroupId { get; set; }

        [JsonProperty("characterId")] public int CharacterId { get; set; }
    }

    internal class Config
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
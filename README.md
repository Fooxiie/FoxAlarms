# FoxAlarms (Public name : VeryFox)
 A new job for your Nova Life Amboise server, setup a society for this job, install alarms, get notification if someone enter in your house.

# For more information go to this Discord :
[Lien Discord](https://discord.gg/aztFmNxEqp)

# Installation
Copy all the files and folder in the plugins folder of your server, set the variable in the config.json file in FoxAlarms folder and it's done

 # Features
 - Install alarm in an area (house) for an amount (can be configurable)
 - Define password for the alarm
 - Give a name of the alarm
 - Log incident and test in discord
 - Generate intervention for incident for the society
 - SMS the owner if incident appear (work for people offline)
 - Moove the alarm in the house
 - Uninstall the alarm
 - Have temporary access to the house for an intervention (Can be logged in discord)
 - Skin for clothes
 - Skin for car

# Configuration
- AlarmPrice => Price of an Alarm installation (without the alarm 3D model)
- SecuritySociety => Array of the society id witch can install and manage the VeryFox Alarm
- messageNotifIntervention => The message on discord for an incident in the alarm
- logDiscordSecret => WebHook adress of the discord channel for the log of intervention
- logDiscordAdress => WebHook adress for the public log of incident (can be the same as Secret)
- accessAlarmAuth => Give access for the VeryFox agent to access to the alarm settings without the code

## How to configure the discord webhook

1. Create a text channel on you discord server (mine for the example is named "demo-logs")
2. Clic on the edit button option the channel
3. In the left menu select "Integration"
4. Now you have a "Create a Webhook" button like on this screen click it
![image](https://github.com/Fooxiie/FoxAlarms/assets/13649585/3174fdcf-40f7-4bb5-8b66-3dcb436515ab)
5. Open the new webhook, choose an avatar and name it like you want
6. The important part is to copy the URL and paste it in the configuration file.
7. That's it ! Don't forget to test your configuration with the test feature in the game.

# Keys
Key P => To Open de interact menu for the agent
All the other command is in menu on the alarm checkpoint

# Detailed features

## Install the alarm
First to know. You need money of the alarm in the society bank <br> In the second thing, you can install an alarm in any area of the game.<br>
Press P and the menu open. You have the option to install the alarm for the price the server choose.
Instantly whent the alarm is finished to setup you can use all the feature.


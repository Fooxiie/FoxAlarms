using FoxAlarms.DataBase;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FoxAlarms
{
    public class LeManipulateurDeLaDonnee
    {
        public static SQLiteAsyncConnection db;
        public static string PathDb;

        public async static Task<bool> Init(string pluginPath)
        {
            PathDb = pluginPath;
            LeManipulateurDeLaDonnee.db = new SQLiteAsyncConnection(pluginPath);
            await LeManipulateurDeLaDonnee.db.CreateTableAsync<AlarmModel>(CreateFlags.None);
            Debug.Log("[FoxAlarms] Database init");
            return true;
        }

        public async static void registerAlarm(uint areaid, float posX, float posY, float posZ, string name)
        {
            AlarmModel alarmModel = new AlarmModel()
            {
                areaId = (int)areaid,
                posX = posX,
                posY = posY,
                posZ = posZ,
                password = "0000",
                status = 0,
                name = name
            };

            await LeManipulateurDeLaDonnee.db.InsertAsync(alarmModel);
        }

        public async static Task<AlarmModel> GetAlarm(int areaId)
        {
            AsyncTableQuery<AlarmModel> asyncTableQuery = LeManipulateurDeLaDonnee.db.Table<AlarmModel>();
            AlarmModel alarm = await asyncTableQuery.Where((AlarmModel a) => a.areaId == areaId).FirstOrDefaultAsync();

            return alarm;
        }

        public static AlarmModel GetAlarmSynchroneTaMere(int areaId)
        {
            SQLiteConnection dobie = new SQLiteConnection(PathDb);
            var TableQuery = dobie.Table<AlarmModel>();
            return TableQuery.Where((AlarmModel a) => a.areaId == areaId).First();
        }

        public async static Task<List<AlarmModel>> GetAlarms()
        {
            AsyncTableQuery<AlarmModel> asyncTableQuery = LeManipulateurDeLaDonnee.db.Table<AlarmModel>();
            List<AlarmModel> alarms= await asyncTableQuery.ToListAsync();

            return alarms;
        }

        public async static void UpdateAlarm(AlarmModel alarm)
        {
            await LeManipulateurDeLaDonnee.db.UpdateAsync(alarm, typeof(AlarmModel));
        }

        public async static void DeleteAlarm(AlarmModel alarm)
        {
            await LeManipulateurDeLaDonnee.db.DeleteAsync(alarm);
        }

        //public async static Task<Armury> getArmury(int characterid)
        //{
        //    AsyncTableQuery<Armury> asyncTableQuery = PoliceSQLUtil.db.Table<Armury>();
        //    Armury armury = await asyncTableQuery.Where((Armury a) => a.characterid == characterid).FirstOrDefaultAsync();

        //    return armury;
        //}

        //public async static Task<bool> removeArmury(int characterid)
        //{
        //    AsyncTableQuery<Armury> asyncTableQuery = PoliceSQLUtil.db.Table<Armury>();
        //    await asyncTableQuery.DeleteAsync((Armury a) => a.characterid == characterid);

        //    return true;
        //}

        //public async static Task<Wanted> getWanted(string name)
        //{
        //    AsyncTableQuery<Wanted> asyncTableQuery = PoliceSQLUtil.db.Table<Wanted>();
        //    Wanted wanted = await asyncTableQuery.Where((Wanted w) => w.name == name).FirstOrDefaultAsync();

        //    return wanted;
        //}

        //public async static Task<List<Wanted>> getAllWanted()
        //{
        //    AsyncTableQuery<Wanted> asyncTableQuery = PoliceSQLUtil.db.Table<Wanted>();
        //    List<Wanted> wanted = await asyncTableQuery.ToListAsync();

        //    return wanted;
        //}

        //public async static Task<bool> addWanted(string name, string reason)
        //{
        //    Wanted wanted = new Wanted()
        //    {
        //        name = name,
        //        reason = reason
        //    };

        //    await PoliceSQLUtil.db.InsertAsync(wanted);

        //    return true;
        //}

        //public async static Task<bool> removeWanted(string name)
        //{
        //    AsyncTableQuery<Wanted> asyncTableQuery = PoliceSQLUtil.db.Table<Wanted>();
        //    await asyncTableQuery.DeleteAsync((Wanted w) => w.name == name);

        //    return true;
        //}
    }
}

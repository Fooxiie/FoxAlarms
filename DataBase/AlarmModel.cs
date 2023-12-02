using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxAlarms.DataBase
{
    public class AlarmModel
    {
        [AutoIncrement]
        [PrimaryKey]
        public int id
        {
            get;
            set;
        }

        public int areaId
        {
            get;
            set;
        }

        public float posX
        {
            get;
            set;
        }

        public float posY
        {
            get;
            set;
        }

        public float posZ
        {
            get;
            set;
        }

        public int status
        {
            get;
            set;
        }

        public string password
        {
            get;
            set;
        }

        public string name
        {
            get;
            set;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmServiceBusConsoleApp
{
    class AlarmMessage
    {
        //public string ioTHubDeviceID { get; set; }
        //public string messageID { get; set; }
        //public string alarmType { get; set; }
        //public string name { get; set; }
        //public string localTime { get; set; }
        //public string createdAt { get; set; }

        public string deviceId { get; set; }
        public string msgId { get; set; }
        public string name { get; set; }
        public string open { get; set; }
        public string age { get; set; }
        public string gender { get; set; }
        public string emotion { get; set; }
        public string time { get; set; }
        public float angerScore{ get; set; }
        public float happyScore { get; set; }
        public float neutralScore { get; set; }
        public float contemptScore { get; set; }
        public float disgustScore { get; set; }
        public float fearScore { get; set; }
        public float sadScore { get; set; }
        public float surpriseScore { get; set; }
    }
}

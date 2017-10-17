using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmServiceBusConsoleApp
{
    class C2DCommand
    {
        public const string COMMAND_OPEN_DOOR_WARNING = "OPEN_DOOR_WARNING";
        public const string COMMAND_CLOSE_DOOR_WARNING = "CLOSE_DOOR_WARNING";

        public string command { get; set; }
        public string value { get; set; }
        public string time { get; set; }        
    }

    class C2DCommand2
    {

        public string Name { get; set; }
        public Boolean Lock { get; set; }
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
    }

}

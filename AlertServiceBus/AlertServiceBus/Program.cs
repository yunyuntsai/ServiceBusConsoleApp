using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure.Devices;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.IO;
using System.Data.Entity;

namespace AlarmServiceBusConsoleApp
{
    class Program
    {
        /* Service Bus */
        private const string QueueName = "cloud2device";// It's hard-coded for this workshop

        /* IoT Hub */
        private static ServiceClient _serviceClient;
        private const string DEVICEID = "Monitor";
        //private const string DEVICEID_WINDOWS_TURBINE = "WinTurbine";// It's hard-coded for this workshop
        //private const string DEVICEID_LINUX_TURBINE = "LinuxTurbine";// It's hard-coded for this workshop

        private const string STORAGEACCOUNT_PROTOCOL = "https";
        private const string CONTAINER_NAME = "ignorelog";

        private static List<string> ignore_messages = new List<string> { };
        private static int open_ignore_count = 0;
        private static int close_ignore_count = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("Console App for Alarm Service Bus...");

            /* Load the settings from App.config */
            string serviceBusConnectionString = ConfigurationManager.AppSettings["ServiceBus.ConnectionString"];
            Console.WriteLine("serviceBusConnectionString={0}\n", serviceBusConnectionString);
            string iotHubConnectionString = ConfigurationManager.AppSettings["IoTHub.ConnectionString"];
            Console.WriteLine("iotHubConnectionString={0}\n", iotHubConnectionString);

            // Retrieve a Queue Client
            QueueClient queueClient = QueueClient.CreateFromConnectionString(serviceBusConnectionString, QueueName);

            // Retrieve a Service Client of IoT Hub
            _serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);

            /*using (var db = new HumanContext())
            {
                // Create and save a new Blog 

                var entity = new Dbmsg
                {
                    name = "None",
                    deviceId = "Monitor",
                    msgId = "Message id 1",
                    open = "1",
                    age = "23",
                    gender = "female",
                    emotion = "Anger",
                    angerScore = 0.9f,
                    happyScore = 0.0f,
                    neutralScore = 0.0f,
                    contemptScore = 0.0f,
                    disgustScore = 0.0f,
                    fearScore = 0.0f,
                    sadScore = 0.0f,
                    surpriseScore = 0.0f,
                    CreatedAt = DateTime.UtcNow.AddHours(8)
                };
                db.Data.Add(entity);
                db.SaveChanges();

            }*/

            //WriteLogToBlob();
            queueClient.OnMessage(message =>
            {
                Console.WriteLine("\n*******************************************************");
                //string msg = message.GetBody<String>();
                try
                {
                    //AlarmMessage alarmMessage = JsonConvert.DeserializeObject<AlarmMessage>(msg);
                    //ProcessAlarmMessage(alarmMessage);
                    //Console.WriteLine(msg);
                    Stream stream = message.GetBody<Stream>();
                    StreamReader reader = new StreamReader(stream, Encoding.ASCII);
                    string s = reader.ReadToEnd();
                    //Console.WriteLine(String.Format("Message body: {0}", s));
                    AlarmMessage alarmMessage = JsonConvert.DeserializeObject<AlarmMessage>(s);
                    ProcessAlarmMessage(alarmMessage);
                    message.Complete();

                }
                catch (Exception ex)
                {
                    Console.WriteLine("****  Exception=" + ex.Message);
                }


            });

            WriteLogToBlob();

            Console.ReadLine();
        }

        private static void ProcessAlarmMessage(AlarmMessage alarmMessage)
        {
            /*switch (alarmMessage.alarmType)
            {
                case "OpenDoor":
                    ActionOpenDoor(alarmMessage);
                    break;
                case "CloseDoor":
                    ActionRepair(alarmMessage);
                    break;
                default:
                    Console.WriteLine("AlarmType is Not accpeted!");
                    break;
            }*/
            switch (alarmMessage.open)
            {
                case "1":
                    ActionOpenDoor(alarmMessage);
                    break;
                case "0":
                    ActionRepair(alarmMessage);
                    break;
                default:
                    Console.WriteLine("AlarmType is Not accpeted!");
                    break;
            }
        }

        private static void ActionOpenDoor(AlarmMessage alarmMessage)
        {
            //if (alarmMessage.ioTHubDeviceID.Equals(DEVICEID))
            if(alarmMessage.deviceId.Equals(DEVICEID))
                ActionOpenDoorWindows(alarmMessage);
        }

        private static void ActionOpenDoorWindows(AlarmMessage alarmMessage)
        {
            //DateTime date1 = Convert.ToDateTime(alarmMessage.createdAt);
            IFormatProvider culture = new System.Globalization.CultureInfo("en-US", true);
            //DateTime date1 = DateTime.Parse(alarmMessage.createdAt, culture, System.Globalization.DateTimeStyles.AssumeLocal);
            DateTime date1 = DateTime.Parse(alarmMessage.time, culture, System.Globalization.DateTimeStyles.AssumeLocal);
            DateTime date2 = DateTime.UtcNow;
            TimeSpan diff = date2.Subtract(date1.AddHours(-16));
            if (diff.TotalSeconds > 20)
            {
                //WriteHighlightedMessage("Message Expired(Open door), " + "Diff=" + diff.TotalSeconds.ToString(), ConsoleColor.Blue);
                ignore_messages.Add(GetDeviceIdHint(alarmMessage.deviceId) +
                    " OpenDoor! Certificate=" + alarmMessage.name +
                    ", MessageID=" + alarmMessage.msgId +
                    ", Diff=" + diff.TotalSeconds.ToString());
                open_ignore_count += 1;
                return;
            }
            WriteHighlightedMessage(
                    GetDeviceIdHint(alarmMessage.deviceId) +
                    " OpenDoor! Certificate=" + alarmMessage.name +
                    ", MessageID=" + alarmMessage.msgId +
                    ", Diff=" + diff.TotalSeconds.ToString(),
                    ConsoleColor.Yellow);

            /*C2DCommand c2dCommand = new C2DCommand();
            c2dCommand.command = C2DCommand.COMMAND_OPEN_DOOR_WARNING;
            c2dCommand.value = alarmMessage.msgId;
            c2dCommand.time = alarmMessage.time;

            SendCloudToDeviceCommand(
                _serviceClient,
                alarmMessage.deviceId,
                c2dCommand).Wait();*/
            C2DCommand2 c2dCommand = new C2DCommand2();
            c2dCommand.Name = alarmMessage.name;
            c2dCommand.Lock = true;
            color c = new color(alarmMessage);
            c2dCommand.R = c.R;
            c2dCommand.G = c.G;
            c2dCommand.B = c.B;
            SendCloudToDeviceCommand(
                _serviceClient,
                "Devices",
                c2dCommand).Wait();


            using (var db = new HumanContext())
            {
                // Create and save a new Blog 

                var entity = new Dbmsg { name = alarmMessage.name,
                                         deviceId = alarmMessage.deviceId,
                                         msgId = alarmMessage.msgId,
                                         open = alarmMessage.open,
                                         age = alarmMessage.age,
                                         gender = alarmMessage.gender,
                                         emotion = alarmMessage.emotion,
                                         angerScore = alarmMessage.angerScore,
                                         happyScore = alarmMessage.happyScore,
                                         neutralScore = alarmMessage.neutralScore,
                                         contemptScore = alarmMessage.contemptScore,
                                         disgustScore = alarmMessage.disgustScore,
                                         fearScore = alarmMessage.fearScore,
                                         sadScore = alarmMessage.sadScore,
                                         surpriseScore = alarmMessage.surpriseScore,
                                         CreatedAt = DateTime.UtcNow.AddHours(8)
                                         //time = alarmMessage.time
                                        };
                db.Data.Add(entity);
                db.SaveChanges();

            }
        }

        private static void ActionRepair(AlarmMessage alarmMessage)
        {
            if (alarmMessage.deviceId.Equals(DEVICEID))
                ActionCloseDoorWindows(alarmMessage);
        }

        private static void ActionCloseDoorWindows(AlarmMessage alarmMessage)
        {
            //DateTime date1 = Convert.ToDateTime(alarmMessage.createdAt);
            IFormatProvider culture = new System.Globalization.CultureInfo("en-US", true);
            DateTime date1 = DateTime.Parse(alarmMessage.time, culture, System.Globalization.DateTimeStyles.AssumeLocal);
            DateTime date2 = DateTime.UtcNow;
            TimeSpan diff = date2.Subtract(date1.AddHours(-16));
            if(diff.TotalSeconds > 20)
            {
                //WriteHighlightedMessage("Message Expired(Close door), " + "Diff=" + diff.TotalSeconds.ToString(), ConsoleColor.Blue);
                ignore_messages.Add(GetDeviceIdHint(alarmMessage.deviceId) +
                    " CloseDoor! " +
                    ", MessageID=" + alarmMessage.msgId +
                    ", Diff=" + diff.TotalSeconds.ToString());
                close_ignore_count += 1;
                return;
            }
            WriteHighlightedMessage(
                    GetDeviceIdHint(alarmMessage.deviceId) +
                    " CloseDoor! " +
                    ", MessageID=" + alarmMessage.msgId +
                    ", Diff=" + diff.TotalSeconds.ToString(),
                    ConsoleColor.Red);

            /*C2DCommand c2dCommand = new C2DCommand();
            c2dCommand.command = C2DCommand.COMMAND_CLOSE_DOOR_WARNING;
            c2dCommand.value = alarmMessage.msgId;
            c2dCommand.time = alarmMessage.time;

            SendCloudToDeviceCommand(
                _serviceClient,
                alarmMessage.deviceId,
                c2dCommand).Wait();*/
            C2DCommand2 c2dCommand = new C2DCommand2();
            c2dCommand.Name = alarmMessage.name;
            c2dCommand.Lock = false;
            color c = new color(alarmMessage);
            c2dCommand.R = c.R;
            c2dCommand.G = c.G;
            c2dCommand.B = c.B;
            SendCloudToDeviceCommand(
                _serviceClient,
                "Devices",
                c2dCommand).Wait();

            using (var db = new HumanContext())
            {
                // Create and save a new Blog 

                var entity = new Dbmsg
                {
                    name = alarmMessage.name,
                    deviceId = alarmMessage.deviceId,
                    msgId = alarmMessage.msgId,
                    open = alarmMessage.open,
                    age = alarmMessage.age,
                    gender = alarmMessage.gender,
                    emotion = alarmMessage.emotion,
                    angerScore = alarmMessage.angerScore,
                    happyScore = alarmMessage.happyScore,
                    neutralScore = alarmMessage.neutralScore,
                    contemptScore = alarmMessage.contemptScore,
                    disgustScore = alarmMessage.disgustScore,
                    fearScore = alarmMessage.fearScore,
                    sadScore = alarmMessage.sadScore,
                    surpriseScore = alarmMessage.surpriseScore,
                    CreatedAt = DateTime.UtcNow.AddHours(8)
                };
                db.Data.Add(entity);
                db.SaveChanges();

            }
        }

        private async static Task SendCloudToDeviceCommand(ServiceClient serviceClient, String deviceId, C2DCommand command)
        {
            var commandMessage = new Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(command)));
            await serviceClient.SendAsync(deviceId, commandMessage);
        }

        private async static Task SendCloudToDeviceCommand(ServiceClient serviceClient, String deviceId, C2DCommand2 command)
        {
            var commandMessage = new Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(command)));
            await serviceClient.SendAsync(deviceId, commandMessage);
        }

        private static void WriteHighlightedMessage(string message, System.ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static string GetDeviceIdHint(string ioTHubDeviceID)
        {
            return "[" + ioTHubDeviceID +" ("+ DateTime.UtcNow.ToString("MM-ddTHH:mm:ss") + ")"+ "]";
        }

        private static void CreateAndUploadBlob(CloudBlobContainer container, string blobName)
        {
            //Console.WriteLine("container={0}, blobName={1}\n", container.Name, blobName);
            // Retrieve reference to a blob named
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

            // Create the device rules for the content of blob
            String blobContent = String.Join("\n",ignore_messages);
            //Console.ForegroundColor = ConsoleColor.Blue;
            //Console.WriteLine("blobContent={0}\n", blobContent);
            //Console.ResetColor();

            blobContent += "\nopen = " + open_ignore_count.ToString() + ", close = " + close_ignore_count.ToString() + "\n";
            byte[] content = ASCIIEncoding.ASCII.GetBytes(blobContent);
            blockBlob.UploadFromByteArrayAsync(content, 0, content.Count()).Wait();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("upload successful for {0} ignore messages.\n", ignore_messages.Count);
            Console.ResetColor();

            open_ignore_count = 0;
            close_ignore_count = 0;
            ignore_messages.Clear();

        }

        private static async void WriteLogToBlob()
        {
            /* Load the settings from App.config */
            string storageAccountName = ConfigurationManager.AppSettings["StorageAccountName"];
            string storageAccountKey = ConfigurationManager.AppSettings["StorageAccountKey"];
            string connectionString = CombineConnectionString(storageAccountName, storageAccountKey);
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference(CONTAINER_NAME);
            // Create the container if it doesn't already exist.
            container.CreateIfNotExists();

            while (true)
            {
                if (ignore_messages.Count > 0)
                {
                    CreateAndUploadBlob(container, GetBlobFileName());
                }
                Task.Delay(20000).Wait();
            }

        }

        private static string CombineConnectionString(string storageAccountName, string storageAccountKey)
        {
            return "DefaultEndpointsProtocol=" + STORAGEACCOUNT_PROTOCOL + ";" +
                "AccountName=" + storageAccountName + ";" +
                "AccountKey=" + storageAccountKey;
        }

        private static DateTimeFormatInfo _formatInfo;
        private static string GetBlobFileName()
        {
            // note: InvariantCulture is read-only, so use en-US and hardcode all relevant aspects
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            _formatInfo = culture.DateTimeFormat;
            _formatInfo.ShortDatePattern = @"yyyy-MM-dd";
            _formatInfo.ShortTimePattern = @"HH-mm-ss";

            //DateTime saveDate = DateTime.UtcNow.AddMinutes(blobSaveMinutesInTheFuture);
            DateTime saveDate = DateTime.UtcNow.AddSeconds(10);// for workshop
            string dateString = saveDate.ToString("d", _formatInfo);
            string timeString = saveDate.ToString("t", _formatInfo);
            string blobName = string.Format(@"{0}\{1}", dateString, timeString);

            return blobName;
        }
    }

    public partial class HumanContext : DbContext
    {
        public HumanContext()
            : base("name=Conn")
        {
        }

        public virtual DbSet<Dbmsg> Data { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }
    }

    public partial class Dbmsg
    {
        public int id { get; set; }
        public string deviceId { get; set; }
        public string msgId { get; set; }
        public string name { get; set; }
        public string open { get; set; }
        public string age { get; set; }
        public string gender { get; set; }
        public string emotion { get; set; }
        public float angerScore { get; set; }
        public float happyScore{ get; set; }
        public float neutralScore{ get; set; }
        public float contemptScore{ get; set; }
        public float disgustScore{ get; set; }
        public float fearScore{ get; set; }
        public float sadScore{ get; set; }
        public float surpriseScore{ get; set; }
        public DateTime CreatedAt { get; set; }
        //public string time { get; set; }
    }

    class color
    {
        public color(AlarmMessage alarmMessage)
        {
            string emotion = alarmMessage.emotion;
            switch (emotion)
            {
                case "Anger":
                    R = 255;
                    G = 0;
                    B = 0;
                    break;
                case "Contempt":
                    R = 0;
                    G = 255;
                    B = 255;
                    break;
                case "Disgust":
                    R = 255;
                    G = 0;
                    B = 238;
                    break;
                case "Fear":
                    R = 224;
                    G = 0;
                    B = 112;
                    break;
                case "Happiness":
                    R = 225;
                    G = 225;
                    B = 0;
                    break;
                case "Neutral":
                    R = 255;
                    G = 255;
                    B = 255;
                    break;
                case "Sadness":
                    R = 0;
                    G = 0;
                    B = 230;
                    break;
                case "Surprise":
                    R = 0;
                    G = 230;
                    B = 23;
                    break;
                default:
                    Console.WriteLine("Emotion Type is Not accpeted!");
                    break;
            }

        }

        public int R;
        public int G;
        public int B;

    }
}

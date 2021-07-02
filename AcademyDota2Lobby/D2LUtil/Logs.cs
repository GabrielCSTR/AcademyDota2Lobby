using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace D2LUtil
{
    public class Logs
    {
        static string Server = "none";

        // The default background colour to use when outputting text to the console.
        public static ConsoleColor DefaultBG = ConsoleColor.Black;
        // The default foreground colour to use when outputting text to the console.
        public static ConsoleColor DefaultFG = ConsoleColor.DarkGreen;
        // The colour to use when displaying an error message in the console.
        static ConsoleColor error = ConsoleColor.Red;
        // The colour to use when displaying a warning message in the console.
        static ConsoleColor warning = ConsoleColor.Yellow;
        // The colour to use when displaying a notice message in the console.
        static ConsoleColor notice = ConsoleColor.Green;
        // The colour to use when displaying a info message in the console.
        static ConsoleColor info = ConsoleColor.Blue;
        // The colour to use when displaying a userconnect message in the console.
        static ConsoleColor userconnect = ConsoleColor.DarkYellow;
        // The colour to use when displaying a userdisconnect message in the console.
        static ConsoleColor userdisconnect = ConsoleColor.Red;
        // The colour to use when displaying a userdisconnect message in the console.
        static ConsoleColor userbanned = ConsoleColor.Red;


        static Queue<LogMessage> messages = new Queue<LogMessage>();
        static bool stopped = false;

        static void Listen()
        {
            while (!stopped)
            {
                if (messages.Count > 0)
                    lock (messages)
                        Message(messages.Dequeue());
                else
                    Thread.Sleep(150);
            }
        }

        static void Message(LogMessage m)
        {
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logMsg = "";
            string message = m.Message;
            ConsoleColor colour = m.Colour;
            string tag = m.Tag;

            bool bright = !((int)colour < 8);
            Console.BackgroundColor = !bright ? (ConsoleColor)((int)colour | 8) : (ConsoleColor)((int)colour ^ 8);
            Console.ForegroundColor = !bright ? ConsoleColor.Black : ConsoleColor.White;
            if (tag != "")
            {
                Console.Write(" ##{0}## ", tag);
                logMsg = String.Format("##{0}## ", tag);
            }
            Console.BackgroundColor = DefaultBG;
            Console.ForegroundColor = colour;
            if (tag != "")
            {
                Console.Write(" {0}\n", message);
                logMsg = String.Format("[{0}] {1}{2}\n", time, logMsg, message);
            }
            else
            {
                Console.Write("{0}\n", message);
                logMsg = String.Format("[{0}] {1}\n", time, message);
            }
            Console.ForegroundColor = DefaultFG;
            writeLogToFile(Server, logMsg);
        }

        public static void Received(string type, int opcode, int size)
        { Message(String.Format("Received packet: CSC_{0} (opcode: {1}, size: {2})", type, opcode, size), DefaultFG); }

        public static void Sent(string type, int opcode, int size)
        { Message(String.Format("Sent packet: SSC_{0} (opcode: {1}, size: {2})", type, opcode, size), DefaultFG); }

        public static void Error(string message)
        { Message(message, error, "ERROR"); }
        public static void Error(string format, params object[] arg)
        { Error(String.Format(format, arg)); }

        public static void Notice(string message)
        { Message(message, notice, "NOTICE"); }
        public static void Notice(string format, params object[] arg)
        { Notice(String.Format(format, arg)); }

        public static void Warning(string message)
        { Message(message, warning, "WARNING"); }
        public static void Warning(string format, params object[] arg)
        { Warning(String.Format(format, arg)); }

        public static void Info(string message)
        { Message(message, info, "INFORMATION"); }
        public static void Info(string format, params object[] arg)
        { Info(String.Format(format, arg)); }

        public static void UserConnect(string message)
        { Message(message, userconnect, "USERCONNECT"); }
        public static void UserConnect(string format, params object[] arg)
        { UserConnect(String.Format(format, arg)); }

        public static void UserDisconnect(string message)
        { Message(message, userdisconnect, "USERDISCONNECT"); }
        public static void UserDisconnect(string format, params object[] arg)
        { UserDisconnect(String.Format(format, arg)); }

        public static void UserBanned(string message)
        { Message(message, userbanned, "USERBANNED"); }
        public static void UserBanned(string format, params object[] arg)
        { UserBanned(String.Format(format, arg)); }


        public static void Message(string message, ConsoleColor colour, string tag = "")
        {
            lock (messages)
                messages.Enqueue(new LogMessage(message, colour, tag));
        }

        public static void Start(string server)
        {
            Server = server;
            var t = new Thread(new ThreadStart(Listen));
            t.Start();
        }

        public static void Stop()
        { stopped = true; }


        static void writeLogToFile(string server, string args)
        {
            string address = "logs/" + server + ".log";
            StreamWriter logFile = null;

            if (!Directory.Exists("logs"))
            {
                Directory.CreateDirectory("logs");
            }
            if (!File.Exists(address))
            {
                logFile = new StreamWriter(address);
            }
            else
                logFile = File.AppendText(address);

            logFile.WriteLine(args);
            logFile.Close();
        }
    }

    public struct LogMessage
    {
        public string Message;
        public ConsoleColor Colour;
        public string Tag;

        public LogMessage(string message, ConsoleColor colour, string tag = "")
        {
            Message = message;
            Colour = colour;
            Tag = tag;
        }
    }
}

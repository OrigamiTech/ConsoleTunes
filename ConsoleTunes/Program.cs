using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace ConsoleTunes
{
    class Program
    {
        const char
            MENU_ITEM_POINTER = (char)26,
            CHAR_STRING = ' ';
        const ConsoleColor
            CONSOLE_COLOR = ConsoleColor.Yellow,
            CONSOLE_BACK_COLOR = ConsoleColor.Black;
        static List<Event>
            Events;
        static double
            BaseTempo = 120;
        static double[]
            StringFreqs = new double[] { 329.63, 246.94, 196.00, 146.83, 110.00, 82.41 };
        const string
            FILENAME_EXTENSION = ".ct",
            EXIT_ITEM = "\b[EXIT]",
            TUNE_DIR = "Tunes/";
        static bool COMPRESSED_FILE = false;

        const double
            DURATION_SWING_QUAVER_1 = 2d / 3d,
            DURATION_SWING_QUAVER_2 = 1d / 3d,
            DURATION_DEMISEMIQUAVER = 1d / 8d,
            DURATION_SEMIQUAVER = 1d / 4d,
            DURATION_QUAVER = 1d / 2d,
            DURATION_CROTCHET = 1,
            DURATION_MINIM = 2,
            DURATION_SEMIBREVE = 4,
            DURATION_TRIPLET_CROTCHET = 2d / 3d,
            DURATION_TRIPLET_QUAVER = 1d / 3d,
            DURATION_TRIPLET_SEMIQUAVER = 1d / 6d,
            DURATION_TRIPLET_DEMISEMIQUAVER = 1d / 12d,
            SEMITONE_INTERVAL = 1.05946309d;    // Will be used to generate a lookup table in future.

        static void Main(string[] args)
        {
            Console.CursorVisible = false;
            Console.BufferWidth = Console.WindowWidth = 80;
            Console.BufferHeight = Console.WindowHeight = 25;
            SetColors();
            Events = new List<Event>();
            Menu();
        }

        #region Backend Code
        static void Menu()
        {
            Console.Title = "ConsoleTunes";
            Console.Clear();
            SetColors();
            Console.WriteLine(@"        ╔═══╦═══╗                ╔═══╗     ╔═══════╗                    ");
            Console.WriteLine(@"        ║ ╔═╩═╗ ╚════════════════╝ │ ╚═════╝ ══╦══ ╚═══════════════════╗");
            Console.WriteLine(@"        ║ ║    ┌───┐┌───┐┌───┐┌───┐│    ┌───┐  ║  ┐   ┌┌───┐┌───┐┌───┐ ║");
            Console.WriteLine(@"        ║ ║    │   ││   │└───┐│   ││    │───┘  ║  │   ││   ││───┘└───┐ ║");
            Console.WriteLine(@"        ║ ╚═══╝└───┘└   └└───┘└───┘└───┘└───┘  ║  └───┘└   └└───┘└───┘ ║");
            Console.WriteLine(@"        ╚══════════════════════════════════════╩═══════════════════════╝");
            Console.WriteLine(@"                             Choose a file to load:");
            Console.WriteLine("");
            if (!Directory.Exists(TUNE_DIR))
                Directory.CreateDirectory(TUNE_DIR);
            List<string> files = Directory.GetFiles(TUNE_DIR, "*" + FILENAME_EXTENSION).ToList();
            if (files.Count != 0)
            {
                for (int i = 0; i < files.Count; i++)
                {
                    files[i] = files[i].Substring(TUNE_DIR.Length, files[i].Length - TUNE_DIR.Length - FILENAME_EXTENSION.Length);
                    SetColors(i != 0);
                    Console.WriteLine((i == 0 ? MENU_ITEM_POINTER.ToString() : " ") + " " + files[i]);
                }
                files.Add(EXIT_ITEM);
                Console.WriteLine("  " + EXIT_ITEM);
                Console.CursorLeft = 0;
                Console.CursorTop -= files.Count;
                int CurrentMenuItem = 0;
                bool UserHasChosen = false;
                while (!UserHasChosen)
                {
                    ConsoleKeyInfo cki = Console.ReadKey(true);
                    if ((cki.Key == ConsoleKey.UpArrow && CurrentMenuItem > 0) || (cki.Key == ConsoleKey.DownArrow && CurrentMenuItem < files.Count - 1) || cki.Key == ConsoleKey.Home || cki.Key == ConsoleKey.End)
                    {
                        Console.CursorLeft = 0;
                        Console.Write("  " + files[CurrentMenuItem]);
                        switch (cki.Key)
                        {
                            case ConsoleKey.UpArrow:
                                CurrentMenuItem--;
                                Console.CursorTop--;
                                break;
                            case ConsoleKey.DownArrow:
                                CurrentMenuItem++;
                                Console.CursorTop++;
                                break;
                            case ConsoleKey.Home:
                                Console.CursorTop -= CurrentMenuItem;
                                CurrentMenuItem = 0;
                                break;
                            case ConsoleKey.End:
                                Console.CursorTop += files.Count - 1 - CurrentMenuItem;
                                CurrentMenuItem = files.Count - 1;
                                break;
                        }
                        //CurrentMenuItem += (cki.Key == ConsoleKey.UpArrow ? -1 : 1);
                        //Console.CursorTop += (cki.Key == ConsoleKey.UpArrow ? -1 : 1);
                        Console.CursorLeft = 0;
                        SetColors(false);
                        Console.Write(MENU_ITEM_POINTER.ToString() + " " + files[CurrentMenuItem]);
                        SetColors();
                        Console.CursorLeft = 0;
                    }
                    UserHasChosen = (cki.Key == ConsoleKey.Enter);
                }
                Console.CursorTop += (files.Count - CurrentMenuItem) + 1;
                if (CurrentMenuItem != files.Count - 1)
                    LoadFile(TUNE_DIR + files[CurrentMenuItem] + FILENAME_EXTENSION);
            }
            else
            {
                Console.WriteLine("No tunes detected. Press any key to exit.");
                Console.ReadKey();
            }
        }

        static void LoadFile(string filePath)
        {
            Console.Clear();
            Events.Clear();
            BaseTempo = 120;
            StreamReader sr = new StreamReader(filePath);
            FileInfo FI = new FileInfo(filePath);
            string currLine = "";
            int stringLine = 0;
            bool successfulLoad = true;
            string FileData = sr.ReadToEnd();
            sr.Close();
            COMPRESSED_FILE = FileData[0] == '$';
            if (!COMPRESSED_FILE)
            {
                string[] lines = FileData.Split('\n');
                for (stringLine = 1; stringLine <= lines.Length; stringLine++)
                {
                    currLine = lines[stringLine - 1];
                    if (!AddEvent(currLine, stringLine))
                    {
                        successfulLoad = false;
                        break;
                    }
                }
            }
            else
                successfulLoad = ReadCompressedFile(filePath);
            if (successfulLoad)
                Play();
            else
                Console.ReadLine();
            Menu();
        }

        static void Play()
        {
            for (int y = 1; y <= 6; y++)
            {
                Console.CursorLeft = 0;
                Console.CursorTop = y;
                Console.Write("".PadLeft(Console.WindowWidth, CHAR_STRING));
            }
            foreach (Event Ev in Events)
            {
                switch (Ev.Type)
                {
                    case EventType.TabNote:
                        TabNote currNote = (TabNote)Ev.Argument;
                        RenderEvent(Ev);
                        Console.Beep((int)currNote.Frequency, (int)((currNote.Duration * 60000d) / BaseTempo));
                        break;
                    case EventType.Rest:
                        System.Threading.Thread.Sleep((int)(((double)Ev.Argument * 60000d) / BaseTempo));
                        break;
                    case EventType.TempoChange:
                        BaseTempo = (double)Ev.Argument;
                        break;
                    case EventType.ColorChange:
                        Console.ForegroundColor = (ConsoleColor)(int)Ev.Argument;
                        break;
                    case EventType.Message:
                        Console.Title = (string)Ev.Argument;
                        break;
                }
            }
        }

        static void RenderEvent(Event Ev)
        {
            switch (Ev.Type)
            {
                case EventType.TabNote:
                    TabNote currNote = (TabNote)Ev.Argument;
                    if (Console.CursorLeft > 0)
                        Console.CursorLeft--;
                    Console.Write(CHAR_STRING);
                    if (currNote.Fret == 0 && currNote.Strings == 0)
                        Console.SetCursorPosition((int)((Math.Log(currNote.Frequency, Math.E) * 100) % Console.WindowWidth), (int)(currNote.Duration % Console.WindowHeight));
                    else
                        Console.SetCursorPosition(currNote.Fret, currNote.Strings);
                    Console.Write('O');
                    break;
                case EventType.Rest:
                    Console.WriteLine(0d + "," + (double)Ev.Argument);
                    break;
            }
        }

        static bool AddEvent(string s, int stringLine)
        {
            s = s.ToLower().Trim();
            try
            {
                if (Regex.Match(s, @"^m\(.+\);$").Success)
                    Events.Add(new Event(EventType.Message, GetMessage(s)));
                else
                {
                    s = s.Replace(" ", "");
                    if (Regex.Match(s, @"^tn\(\d+,\d+,.+\);$").Success)
                        Events.Add(new Event(EventType.TabNote, GetTabNote(s)));
                    /*
                    else if (Regex.Match(s, @"^sn\([a-g](s|b)?\d,.+\);$").Success)
                        Events.Add(new Event(EventType.TabNote, GetScaleNote(s)));
                     */
                    else if (Regex.Match(s, @"^fn\(\d+(\.\d+)?,.+\);$").Success)
                        Events.Add(new Event(EventType.TabNote, GetFreqNote(s)));
                    else if (Regex.Match(s, @"^r\(.+\);$").Success)
                        Events.Add(new Event(EventType.Rest, GetRest(s)));
                    else if (Regex.Match(s, @"^t\(\d+\);$").Success)
                        Events.Add(new Event(EventType.TempoChange, GetTempo(s)));
                    else if (Regex.Match(s, @"^c\(\d+\);$").Success)
                        Events.Add(new Event(EventType.ColorChange, GetColor(s)));
                    else if (s.Length > 0 && !s.StartsWith("//"))
                        throw new Exception();
                }
                return true;
            }
            catch
            { Console.WriteLine("Encountered an error on line " + stringLine + " while loading.\nPress enter to return to the menu."); }
            return false;
        }

        static void SetColors()
        { SetColors(true); }
        static void SetColors(bool useDefaults)
        {
            Console.ForegroundColor = (useDefaults ? CONSOLE_COLOR : CONSOLE_BACK_COLOR);
            Console.BackgroundColor = (useDefaults ? CONSOLE_BACK_COLOR : CONSOLE_COLOR);
        }

        static bool ReadCompressedFile(string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            MemoryStream ms = new MemoryStream(bytes);
            ms.Position = 0;
            if ((char)ms.ReadByte() != '$')
                return false;
            else
            {
                while (ms.Position < ms.Length)
                {
                    EventType eventType = (EventType)(byte)ms.ReadByte();
                    switch (eventType)
                    {
                        case EventType.ColorChange:
                            Events.Add(new Event(eventType, ms.ReadByte()));
                            break;
                        case EventType.FreqNote:
                            byte[]
                                fnFrequencyBytes = new byte[8],
                                fnDurationBytes = new byte[8];
                            for (int i = 0; i < 8; i++)
                                fnFrequencyBytes[i] = (byte)ms.ReadByte();
                            for (int i = 0; i < 8; i++)
                                fnDurationBytes[i] = (byte)ms.ReadByte();
                            double
                                fnFrequency = BitConverter.ToDouble(fnFrequencyBytes, 0),
                                fnDuration = BitConverter.ToDouble(fnDurationBytes, 0);
                            Events.Add(new Event(eventType, new FreqNote(fnFrequency, fnDuration)));
                            break;
                        case EventType.TabNote:
                            byte tnData = (byte)ms.ReadByte();
                            byte[] tnDurationBytes = new byte[8];
                            for (int i = 0; i < 8; i++)
                                tnDurationBytes[i] = (byte)ms.ReadByte();
                            double tnDuration = BitConverter.ToDouble(tnDurationBytes, 0);
                            Events.Add(new Event(eventType, new TabNote((tnData >> 3) & 0x1F, tnData & 0x07, tnDuration)));
                            break;
                        case EventType.Rest:
                            byte[] restDurationBytes = new byte[8];
                            for (int i = 0; i < 8; i++)
                                restDurationBytes[i] = (byte)ms.ReadByte();
                            double restDuration = BitConverter.ToDouble(restDurationBytes, 0);
                            Events.Add(new Event(eventType, restDuration));
                            break;
                        case EventType.TempoChange:
                            byte[] tempoBytes = new byte[8];
                            for (int i = 0; i < 8; i++)
                                tempoBytes[i] = (byte)ms.ReadByte();
                            double tempo = BitConverter.ToDouble(tempoBytes, 0);
                            Events.Add(new Event(eventType, tempo));
                            break;
                        case EventType.Message:
                            byte messageLength = (byte)ms.ReadByte();
                            string message = "";
                            for (int i = 0; i < messageLength; i++)
                                message += (char)(byte)ms.ReadByte();
                            Events.Add(new Event(eventType, message));
                            break;
                        default:
                            return false;
                    }
                }
            }
            return true;
        }
        static string[] GetParams(string call)
        {
            call = call.Substring(call.IndexOf("(") + 1, call.IndexOf(")") - call.IndexOf("(") - 1);
            return call.Split(',');
        }
        #endregion

        #region Classes

        private class Note
        {
            private double
                _Duration = DURATION_CROTCHET,
                _Frequency = 440;
            private int
                _Strings,
                _Fret;
            public double Duration
            {
                get { return _Duration; }
                set { _Duration = value; }
            }
            public double Frequency
            {
                get { return _Frequency; }
                set { _Frequency = value; }
            }
            public int Strings { get { return _Strings; } }
            public int Fret { get { return _Fret; } }
            public Note(double duration, double frequency, int fret, int strings)
            {
                this._Duration = duration;
                this._Frequency = frequency;
                this._Fret = fret;
                this._Strings = strings;
            }
        }
        private class TabNote
        {
            private double
                _Duration = DURATION_CROTCHET,
                _Frequency = 440;
            private int
                _Strings,
                _Fret;
            public double Frequency { get { return _Frequency; } }
            public double Duration { get { return _Duration; } }
            public int Strings { get { return _Strings; } set { _Strings = value; Recalc(); } }
            public int Fret { get { return _Fret; } set { _Fret = value; Recalc(); } }
            public TabNote(int fret, int strings, double duration)
            {
                this._Duration = duration;
                this._Fret = fret;
                this._Strings = strings;
                Recalc();
            }
            private void Recalc()
            {
                _Frequency = GetFrequency(_Fret, _Strings);
            }
        }
        // Will implement this soon. 
        // This will allow specifying notes as SN(Cs5,crotchet);, for example.
        /*
        private class ScaleNote
        {
            private double
                _Duration = DURATION_CROTCHET,
                _Frequency = 440;
            public ScaleNote(double duration)
            {
                this._Duration = duration;
            }
        }
         */
        private class FreqNote
        {
            private double
                _Duration = DURATION_CROTCHET,
                _Frequency = 440;
            public double Duration
            {
                get { return _Duration; }
                set { _Duration = value; }
            }
            public double Frequency
            {
                get { return _Frequency; }
                set { _Frequency = value; }
            }
            public FreqNote(double frequency, double duration)
            {
                this._Frequency = frequency;
                this._Duration = duration;
            }
        }

        private class Event
        {
            public EventType Type;
            public object Argument;
            public Event(EventType e, object a)
            {
                Type = e;
                Argument = a;
            }
        }
        enum EventType : byte
        {
            FreqNote = 0x00,
            Rest = 0x01,
            TempoChange = 0x02,
            ColorChange = 0x03,
            Message = 0x04,
            ScaleNote = 0x05,
            TabNote = 0x06
        }

        #endregion

        #region Musical Code

        static Note GetNote(string s)
        {
            string[] noteParams = GetParams(s);
            int fret = int.Parse(noteParams[0]);
            int strings = int.Parse(noteParams[1]);
            string duration = noteParams[2];
            return new Note(GetDuration(duration), GetFrequency(fret, strings), fret, strings);
        }
        static TabNote GetTabNote(string s)
        {
            string[] noteParams = GetParams(s);
            int fret = int.Parse(noteParams[0]);
            int strings = int.Parse(noteParams[1]);
            string duration = noteParams[2];
            return new TabNote(fret, strings, GetDuration(duration));
        }
        /*
        static ScaleNote GetScaleNote(string s)
        {
         
        }
         */
        static FreqNote GetFreqNote(string s)
        {
            string[] noteParams = GetParams(s);
            double frequency = double.Parse(noteParams[0]);
            string duration = noteParams[1];
            return new FreqNote(frequency, GetDuration(duration));
        }

        static double GetRest(string s)
        { return GetDuration(s.Substring(s.IndexOf("(") + 1, s.IndexOf(")") - s.IndexOf("(") - 1)); }

        static double GetFrequency(int fret, int strings)
        {
            double frequency = StringFreqs[strings - 1];
            while (fret >= 12)
            {
                frequency *= 2;
                fret -= 12;
            }
            frequency *= Math.Pow(2.0, (double)fret / 12d);
            return frequency;
        }

        static double GetTempo(string s)
        { return Convert.ToDouble(s.Substring(s.IndexOf("(") + 1, s.IndexOf(")") - s.IndexOf("(") - 1)); }

        static double GetDuration(string duration)
        {
            switch (duration.ToLower().Trim())
            {
                case "squaver1":
                    return DURATION_SWING_QUAVER_1;
                case "squaver2":
                    return DURATION_SWING_QUAVER_2;
                case "demisemiquaver":
                    return DURATION_DEMISEMIQUAVER;
                case "semiquaver":
                    return DURATION_SEMIQUAVER;
                case "quaver":
                    return DURATION_QUAVER;
                case "crotchet":
                    return DURATION_CROTCHET;
                case "minim":
                    return DURATION_MINIM;
                case "semibreve":
                    return DURATION_SEMIBREVE;
                case "trip_crotchet":
                    return DURATION_TRIPLET_CROTCHET;
                case "trip_quaver":
                    return DURATION_TRIPLET_QUAVER;
                case "trip_semiquaver":
                    return DURATION_TRIPLET_SEMIQUAVER;
                case "trip_demisemiquaver":
                    return DURATION_TRIPLET_DEMISEMIQUAVER;
                default:
                    return Convert.ToDouble(duration.Trim());
            }
        }

        static int GetColor(string color)
        { return int.Parse(color.Substring(color.IndexOf("(") + 1, color.IndexOf(")") - color.IndexOf("(") - 1)); }
        static string GetMessage(string message)
        { return message.Substring(message.IndexOf("(") + 1, message.IndexOf(")") - message.IndexOf("(") - 1); }
        #endregion
    }
}
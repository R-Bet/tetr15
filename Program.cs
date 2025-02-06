using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Collections;

namespace tetr15
{
    internal partial class Program
    {
        static void Main(string[] args)
        {
            PrintMenu();
            Console.CursorVisible = false;

            while (true)
                switch (Console.ReadKey(true).KeyChar)
                {
                    case '1':
                        PlayGame();
                        break;
                    case '2':
                        ShowTopScores();
                        break;
                    case '3':
                        ShowControls();
                        break;
                    case '4':
                        Settings();
                        break;
                    case '5':
                        Exit();
                        break;
                }
        }

        public static void Settings()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.SetWindowSize(30, 10);

            if (!File.Exists("Settings.config"))
                MakeSettings();

            List<(string setting, bool current)> Settings = GetSettings();

            PrintSettings(Settings);

            HandleSettingsInput(Settings);

            SetSettings(Settings);

            PrintMenu();

        }


        private static void HandleSettingsInput(List<(string setting, bool current)> Settings)
        {
            ConsoleKeyInfo input = Console.ReadKey(true);
            while (input.Key != ConsoleKey.Escape)
            {
                Console.SetCursorPosition(0, 0);
                if (char.IsDigit(input.KeyChar))
                {
                    if (int.Parse(input.KeyChar + "") > 0 && int.Parse(input.KeyChar + "") <= Settings.Count)
                    {
                        (string setting, bool current) CurrentSet = Settings[int.Parse(input.KeyChar + "") - 1];
                        Settings[int.Parse(input.KeyChar + "") - 1] = (CurrentSet.setting, !CurrentSet.current);
                    }
                }

                PrintSettings(Settings);
                input = Console.ReadKey(true);
            }
        }

        private static void PrintSettings(List<(string setting, bool current)> Settings)
        {
            int LineIndex = 1;
            foreach ((string setting, bool current) Line in Settings)
            {
                Console.WriteLine($"({LineIndex}) {Line.setting} - {Line.current}");
                LineIndex++;
            }
        }

        public static void MakeSettings()
        {
            string[] SettingLines = new string[]
            {
        "show_ghost_piece=1",
        "animate_line_clear=1"
            };

            File.WriteAllLines("Settings.config", SettingLines);
            Thread.Sleep(10);
        }

        public static List<(string setting, bool current)> GetSettings()
        {
            string[] SettingLines = File.ReadAllLines("Settings.config");

            List<(string setting, bool current)> Settings = new List<(string setting, bool current)>();

            for (int i = 0; i < SettingLines.Length; i++)
            {
                string SettingLine = SettingLines[i];
                string[] SettingSplit = SettingLine.Split('=');

                if (SettingSplit.Length != 2)
                    throw new Exception("Options files is in the wrong format! Please correct or delete the settings file.");
                Settings.Add((SettingSplit[0], int.Parse(SettingSplit[1]) == 0 ? false : true));
            }

            return Settings;
        }

        public static void SetSettings(List<(string setting, bool current)> Settings)
        {
            string[] SettingLines = new string[Settings.Count];
            for (int i = 0; i < Settings.Count; i++)
            {
                SettingLines[i] = Settings[i].setting + "=" + (Settings[i].current ? 1 : 0);
            }

            File.WriteAllLines("Settings.config", SettingLines);
        }


        public static void Exit()
        {
            Environment.Exit(0);
        }

        public static void ShowControls()
        {
            Console.Clear();
            Console.SetWindowSize(50, 18);
            StringBuilder sb = new StringBuilder();
            Console.ForegroundColor = ConsoleColor.Green;
            sb.Append(
            "╔══════════════════════════════════════════════╗\n" +
            "║                                              ║\n" +
            "║ Move piece - right, left and down arrow keys ║\n" +
            "║ or the W, S and D keys respectively          ║\n" +
            "║                                              ║\n" +
            "║ Rotate -                                     ║\n" +
            "║   Clockwise - the up arrow or W key          ║\n" +
            "║   Counterclockwise - Z key                   ║\n" +
            "║                                              ║\n" +
            "║ Space - hard drop                            ║\n" +
            "║                                              ║\n" +
            "║ Hold - C key                                 ║\n" +
            "║                                              ║\n" +
            "║ Esc - quit to menu                           ║\n" +
            "║                                              ║\n" +
            "║ R - restart                                  ║\n" +
            "║                                              ║\n" +
            "╚══════════════════════════════════════════════╝\n");
            Console.WriteLine(sb);
            Console.SetCursorPosition(0, 0);
            Console.ReadKey(true);
            PrintMenu();
        }

        public static string GetDoubleCompletedToNthDigit(double input, int digit)
        {
            string str = input.ToString();

            if (str.Length > digit) throw new Exception($"Error! Number is longer than {digit} digits.");

            return new string('0', digit - str.Length) + str;
        }

        public static void PlayGame()
        {
            tetr15 t = new tetr15(GetSettings());
            int StartingLevel = SelectNumber(20);
            double Score = t.StartGame(StartingLevel);
            if (Score == -1)
            {
                while (Score == -1)
                {
                    Thread.Sleep(10);
                    t = new tetr15(GetSettings());
                    Score = t.StartGame(StartingLevel);
                }
            }
            if (Score == -2)
            {
                Thread.Sleep(10);
                PrintMenu();
                return;
            }

            SwallowingWait(300);

            ShowScoreScreen(Score);
            Console.ForegroundColor = ConsoleColor.Green;
            string Name = Console.ReadLine();
            SaveScore(Score, Name);

            Console.Clear();
            PrintMenu();
        }

        public static void PrintMenu()
        {
            Console.Clear();
            Console.SetWindowSize(25, 10);
            WriteLineGreen("╔════════════════════╗");
            WriteGreen("║ ");
            WriteRainbow("Welcome to Tetr15!");
            WriteLineGreen(" ║");
            WriteLineGreen("║                    ║");
            WriteGreen("║ (1)");
            Console.Write(" Play           ");
            WriteLineGreen("║");
            WriteGreen("║ (2)");
            Console.Write(" Top Scores     ");
            WriteLineGreen("║");
            WriteGreen("║ (3)");
            Console.Write(" Show Controls  ");
            WriteLineGreen("║");
            WriteGreen("║ (4)");
            Console.Write(" Settings       ");
            WriteLineGreen("║");
            WriteGreen("║ (5)");
            Console.Write(" Exit Game      ");
            WriteLineGreen("║                    ║");
            WriteLineGreen("╚════════════════════╝");
        }

        public static void SaveScore(double Score, string Name)
        {
            File.AppendAllLines("Scores.txt", [Name + ":" + Score]);
        }

        public static int SelectNumber(int End)
        {
            Console.SetWindowSize(30, 7);
            Console.Clear();

            WriteLineGreen("Select starting level (0-" + End + ")");

            WriteGreen("╔");
            for (int i = 0; i < End + 1; i++)
            {
                WriteGreen("═");
            }

            WriteLineGreen("╗");

            WriteGreen("║■");
            Console.Write(new string('■', End));

            WriteLineGreen("║");

            WriteGreen("╚");
            for (int i = 0; i < End + 1; i++)
            {
                WriteGreen("═");
            }

            WriteLineGreen("╝");

            WriteLineGreen("Choose using arrow keys");
            WriteLineGreen("Press enter to continue");

            int Select = 0;
            ConsoleKey Key = Console.ReadKey(true).Key;
            while (Key != ConsoleKey.Enter)
            {
                Console.SetCursorPosition(1, 2);

                if (Key == ConsoleKey.LeftArrow || Key == ConsoleKey.A)
                {
                    if (Select > 0)
                        Select--;
                }

                if (Key == ConsoleKey.RightArrow || Key == ConsoleKey.D)
                {
                    if (Select < End)
                        Select++;
                }

                for (int i = 0; i <= End; i++)
                {
                    if (i == Select)
                        WriteGreen("■");
                    else
                        Console.Write("■");
                }

                Key = Console.ReadKey(true).Key;
            }

            return Select;
        }

        public static void ShowScoreScreen(double Score)
        {
            Console.Clear();
            WriteLineGreen("╔══════════════════╗");
            WriteGreen("║    ");
            WriteRainbow("Game over!   ");
            WriteLineGreen(" ║");
            WriteLineGreen("║                  ║");
            WriteGreen("║ Score:");
            WriteRainbow($"{GetDoubleCompletedToNthDigit(Score, 10)} ");
            WriteLineGreen("║");
            WriteLineGreen("║                  ║");
            WriteLineGreen("║ Enter name:      ║");
            WriteLineGreen("║                  ║");
            WriteLineGreen("╚══════════════════╝");
        }

        public static void ShowTopScores()
        {
            if (!File.Exists("Scores.txt"))
            {
                Console.Clear();
                Console.WriteLine("There are no scores to show!");
                Console.ReadKey(true);
                PrintMenu();
                return;
            }
            Console.Clear();

            string[] ScoreTXT = File.ReadAllLines("Scores.txt");
            if (ScoreTXT.Length <= 0)
            {
                Console.Clear();
                Console.WriteLine("There are no scores to show!");
                Console.ReadKey(true);
                PrintMenu();
                return;
            }

            List<ScoreWithName> Scores = new List<ScoreWithName>();
            for (int i = 0; i < ScoreTXT.Length; i++)
            {
                double Score = double.Parse(ScoreTXT[i].Split(":")[1]);
                string Name = ScoreTXT[i].Split(":")[0];
                Scores.Add((Score, Name));
            }

            Scores.Sort();

            Console.SetWindowSize(30, 12);

            WriteLineGreen("Top scores: ");
            int ShowScoresCount = 10;
            if (Scores.Count < 10)
                ShowScoresCount = Scores.Count;

            for (int i = 0; i < ShowScoresCount; i++)
            {
                WriteLineGreen(Scores[i].Name.Trim() + ": " + Scores[i].Score);
            }


            SwallowingWait(200);

            Console.Clear();
            PrintMenu();
        }

        public static void SwallowingWait(int ms)
        {
            Stopwatch Swallow = new Stopwatch();
            Swallow.Start();
            while (Swallow.ElapsedMilliseconds < ms)
            {
                Console.ReadKey();
            }
        }

        public static void WriteGreen(string str)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(str);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void WriteLineGreen(string str)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(str);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void WriteRainbow(string str)
        {
            ConsoleColor[] Rainbow = {
                ConsoleColor.DarkRed,
                ConsoleColor.Red,
                ConsoleColor.DarkYellow,
                ConsoleColor.Yellow,
                ConsoleColor.Green,
                ConsoleColor.DarkGreen,
                ConsoleColor.Cyan,
                ConsoleColor.DarkCyan,
                ConsoleColor.Blue,
                ConsoleColor.DarkBlue,
                ConsoleColor.Magenta,
                ConsoleColor.DarkMagenta,
            };

            int RainbowIndex = 0;
            int Step = 1;
            for (int i = 0; i < str.Length; i++)
            {
                Console.ForegroundColor = Rainbow[RainbowIndex];
                Console.Write(str[i]);

                if (RainbowIndex + Step == Rainbow.Length)
                    Step = -1;
                if (RainbowIndex + Step == -1)
                    Step = 1;
                RainbowIndex += Step;
            }
            Console.BackgroundColor = ConsoleColor.Black;
        }
    }
}

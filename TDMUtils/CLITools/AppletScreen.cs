using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDMUtils.CLITools
{
    public abstract class Applet
    {
        /// <summary>
        /// Whether the app is enabled. Disabled app will not be printed
        /// </summary>
        public bool IsEnabled = true;
        /// <summary>
        /// The amount of values that should be displayed at once in the applet
        /// </summary>
        public int valueSize = 0;
        /// <summary>
        /// The total length of the applet when printed to the screen
        /// </summary>
        public int TotalSize => valueSize + 2; //Size of the values plus header and separator
        /// <summary>
        /// The applets starting position in the console window.
        /// </summary>
        public int startIndex = 0;
        /// <summary>
        /// Marks that the app data needs to be redrawn
        /// </summary>
        public int NeedsUpdate = 0;
        /// <summary>
        /// The current value page being displayed
        /// </summary>
        public int currentPage = 0;
        /// <summary>
        /// The total amount of pages values will be split into.
        /// </summary>
        public int maxPage = 0;
        /// <summary>
        /// Clears the line before writing a new line. Used when special formatting might mess up spacing.
        /// </summary>
        public bool AggressiveLineClearing = false;
        /// <summary>
        /// The display name of the app
        /// </summary>
        /// <returns></returns>
        public abstract string Title();
        /// <summary>
        /// Are the values displayed by the app a static unchanging size?
        /// </summary>
        public abstract bool StaticSize();
        /// <summary>
        /// Should the values of the app be displayed backwards? (ex.. chat logs)
        /// </summary>
        public abstract bool StartAtEnd();
        /// <summary>
        /// The values that will be displayed by the app
        /// </summary>
        public abstract object[] Values();
    }
    public class AppletScreen
    {
        int MenuIndex = 0;
        Applet[] applets;
        Applet SelectedApplet;
        public Dictionary<Type, Func<object, string>> Formatters = new()
        {
            [typeof(string)] = s => ((string)s).PadRight(Console.WindowWidth),
            [typeof(ColoredString)] = s => ((ColoredString)s).PadRight(Console.WindowWidth).Build(),
            [typeof(DateTime)] = s => ((DateTime)s).ToString("MM/dd/yyyy").PadRight(Console.WindowWidth),
        };
        string MenuBar => $"[Esc] Menu [R] Refresh [↕] Cycle Selected App ({SelectedApplet.Title()}) [↔] Cycle App Page [Space] Toggle App";
        public AppletScreen(Applet[] Apps)
        {
            applets = Apps;
            SelectedApplet = applets.First();
        }

        /// <summary>
        /// Flags apps of the given type to be updated. Use the base class <Applet> to flagg all.
        /// </summary>
        /// <typeparam name="T">The type of app to update</typeparam>
        /// <param name="Delay">They amount of cycles that should pass once the update is flagged before it happens</param>
        public void FlagForUpdate<T>(int Delay = 1)
        {
            foreach (var i in applets)
                if (i is T && i.NeedsUpdate == 0)
                    i.NeedsUpdate = Delay;
        }
        /// <summary>
        /// Runs the App Screen
        /// </summary>
        public void Show()
        {
            using var cts = new CancellationTokenSource();
            var lastTick = Environment.TickCount64;
            var refreshMs = 500;
            FormatWindow();
            Console.CursorVisible = false;
            while (!cts.IsCancellationRequested)
            {
                Console.SetCursorPosition(0, 0);
                while (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    Console.CursorVisible = false;
                    switch (key)
                    {
                        case ConsoleKey.Escape: return;
                        case ConsoleKey.R: FormatWindow(); break;
                        case ConsoleKey.UpArrow: SelectedApplet = GetNextApplet(applets, SelectedApplet, true); FormatWindow(); break;
                        case ConsoleKey.DownArrow: SelectedApplet = GetNextApplet(applets, SelectedApplet); FormatWindow(); break;
                        case ConsoleKey.Spacebar: SelectedApplet.IsEnabled = !SelectedApplet.IsEnabled; FormatWindow(); break;
                        case ConsoleKey.RightArrow: SelectedApplet.currentPage++; PrintApp(SelectedApplet); break;
                        case ConsoleKey.LeftArrow: SelectedApplet.currentPage--; PrintApp(SelectedApplet); break;
                    }
                }

                if (Environment.TickCount64 - lastTick >= refreshMs)
                {
                    UpdateApps();
                    lastTick = Environment.TickCount64;
                }

                Thread.Sleep(10);
            }
            Console.CursorVisible = true;
        }

        private void UpdateApps()
        {
            foreach (var i in applets.Where(x => x.IsEnabled && x.NeedsUpdate > 0))
            {
                i.NeedsUpdate--;
                if (i.NeedsUpdate == 0)
                    PrintApp(i);
            }
        }

        private void FormatWindow()
        {
            Console.Clear();
            CalculateAppProperties();
            foreach (var i in applets.Where(x => x.IsEnabled))
                PrintApp(i, true);
            Console.SetCursorPosition(0, MenuIndex);
            Console.WriteLine(MenuBar);
        }

        private void PrintApp(Applet app, bool full = false)
        {
            object[][] pages = app.StartAtEnd() ? [.. app.Values().Reverse().Chunk(app.valueSize)] : [.. app.Values().Chunk(app.valueSize)];
            app.maxPage = Math.Max(pages.Length - 1, 0);
            app.currentPage = Math.Clamp(app.currentPage, 0, app.maxPage);

            var row = app.startIndex;
            var page = pages.Length > 0 ? (app.StartAtEnd() ? pages[app.currentPage].Reverse().ToArray() : pages[app.currentPage]) : [];

            Console.SetCursorPosition(0, row);
            if (full || pages.Length > 1)
            {
                string Title = "";
                if (SelectedApplet == app) Title += "> ";
                Title += app.Title();
                if (pages.Length > 1) Title += $" {app.currentPage + 1}/{app.maxPage + 1}";
                Console.Write(Title.PadRight(Console.WindowWidth));
            }
            row++;
            for (int i = 0; i < app.valueSize; i++)
            {
                Console.SetCursorPosition(0, row++);
                if (app.AggressiveLineClearing)
                    Console.Write(new string(' ', Console.WindowWidth));
                object printObject = (i < page.Length ? page[i] : string.Empty);
                if (Formatters.TryGetValue(printObject.GetType(), out var formatter))
                    Console.Write(formatter(printObject));
                else
                    Console.Write((printObject.ToString()??printObject.GetType().ToString()).PadRight(Console.WindowWidth));
            }
            if (full)
            {
                Console.SetCursorPosition(0, row);
                Console.Write(new string('=', Console.WindowWidth));
            }
        }

        private static T GetNextApplet<T>(IList<T> list, T current, bool reverse = false)
        {
            int i = list.IndexOf(current);
            if (i == -1)
                return list[0];

            int next = reverse ? (i - 1 + list.Count) % list.Count : (i + 1) % list.Count;

            return list[next];
        }

        public static T[] TakePortion<T>(T[] source, int count, bool fromEnd) => fromEnd ? [.. source.TakeLast(count)] : [.. source.Take(count)];

        private void CalculateAppProperties()
        {
            var EnabledApps = applets.Where(x => x.IsEnabled);

            var AvailableConsoleSpace = Console.WindowHeight - 2; //Minus one for buffer and one for menu
            var StaticApps = EnabledApps.Where(x => x.StaticSize());
            var DynamicApps = EnabledApps.Where(x => !x.StaticSize());

            foreach (var app in StaticApps)
                app.valueSize = app.Values().Length;

            int StaticCount = StaticApps.Select(x => x.TotalSize).Sum();
            int DynamicAvailableCount = AvailableConsoleSpace - StaticCount;
            int PerDynamic = DynamicApps.Count() > 0 ? DynamicAvailableCount / DynamicApps.Count() : 0;

            foreach (var app in DynamicApps)
                app.valueSize = PerDynamic - 2; //This value tracks items and does not account for the separator and Title

            int Ind = 0;
            foreach (var app in EnabledApps)
            {
                app.startIndex = Ind;
                Ind = Ind + app.TotalSize;
                app.currentPage = 0;
            }
            MenuIndex = Ind;
        }
    }
}

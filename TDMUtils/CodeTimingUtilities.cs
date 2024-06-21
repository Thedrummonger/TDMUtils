using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDMUtils
{
    internal class CodeTimingUtilities
    {

        /// <summary>
        /// Tracks code execution Timing
        /// </summary>
        /// <param name="stopwatch">The source Stopwatch Object</param>
        /// <param name="CodeTimed">Description of the code being timed</param>
        /// <param name="Action">The action to perform 0 = Start the stopwatch, 1 = Print Time and Restart, 2 = Print Time and stop timer</param>
        public enum StopwatchAction
        {
            start,
            stop,
            reset
        }
        public static void TimeCodeExecution(Stopwatch stopwatch, string CodeTimed = "", StopwatchAction Action = 0)
        {
            if (Action == StopwatchAction.start)
            {
                stopwatch.Start();
            }
            else
            {
                Debug.WriteLine($"{CodeTimed} took {stopwatch.ElapsedMilliseconds} m/s");
                stopwatch.Stop();
                stopwatch.Reset();
                if (Action == StopwatchAction.reset) { stopwatch.Start(); }
            }
        }
    }
}

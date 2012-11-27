using System.Runtime.InteropServices;

namespace Algorithms
{
    public static class HighResolutionTime
    {
        #region Win32APIs

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long perfcount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long freq);

        #endregion

        #region Variables Declaration

        private static long mStartCounter;
        private static readonly long mFrequency;

        #endregion

        #region Constuctors

        static HighResolutionTime()
        {
            QueryPerformanceFrequency(out mFrequency);
        }

        #endregion

        #region Methods

        public static double GetTime()
        {
            long endCounter;
            QueryPerformanceCounter(out endCounter);
            long elapsed = endCounter - mStartCounter;
            return (double) elapsed/mFrequency;
        }

        public static void Start()
        {
            QueryPerformanceCounter(out mStartCounter);
        }

        #endregion
    }
}
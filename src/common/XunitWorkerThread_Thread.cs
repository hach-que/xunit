using System;
using System.Threading;

namespace Xunit.Sdk
{
    internal class XunitWorkerThread
    {
        readonly Thread thread;

        public XunitWorkerThread(Action threadProc)
        {
            thread = new Thread(() => threadProc());
            thread.Start();
        }

        public void Join()
        {
            thread.Join();
        }

#if PLATFORM_LINUX || PLATFORM_MACOS
        public void Abort()
        {
            thread.Abort();
        }
#endif

        public static void QueueUserWorkItem(Action backgroundTask)
        {
            ThreadPool.QueueUserWorkItem(_ => backgroundTask());
        }
    }
}
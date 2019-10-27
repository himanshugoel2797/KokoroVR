using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Common
{
    public static class BackgroundTaskManager
    {
        private static Queue<Action> BackgroundTasks;   //execute these tasks during waits and finally, before swapbuffers
        private static HashSet<Action> DeregisterTasks;    //tasks to deregister

        static BackgroundTaskManager()
        {
            BackgroundTasks = new Queue<Action>();
            DeregisterTasks = new HashSet<Action>();
        }

        public static void RegisterBackgroundTask(Action a)
        {
            BackgroundTasks.Enqueue(a);
        }

        public static void DeregisterBackgroundTask(Action a)
        {
            DeregisterTasks.Add(a);
        }

        public static bool ExecuteBackgroundTasksUntil(Func<bool> a)
        {
            while (!a())
            {
                if (BackgroundTasks.Count == 0)
                    return a();

                ExecuteBackgroundTask();
            }

            return true;
        }

        public static void ExecuteBackgroundTask()
        {
            if (BackgroundTasks.Count == 0)
                return;

            Action a = BackgroundTasks.Dequeue();
            if (DeregisterTasks.Contains(a))
            {
                DeregisterTasks.Remove(a);
                return;
            }
            a();
        }
    }
}

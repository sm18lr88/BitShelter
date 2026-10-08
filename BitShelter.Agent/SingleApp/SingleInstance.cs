using System.Threading;

namespace BitShelter.Agent.SingleApp
{
  public static class SingleInstance
  {
    private const string MutexName = @"Local\BitShelter.Agent";

    private static Mutex mutex;

    public static bool Start()
    {
      mutex = new Mutex(true, MutexName, out bool onlyInstance);

      if (!onlyInstance)
      {
        mutex.Dispose();
        mutex = null;
      }

      return onlyInstance;
    }

    public static void Stop()
    {
      if (mutex == null)
        return;

      mutex.ReleaseMutex();
      mutex.Dispose();
      mutex = null;
    }
  }
}

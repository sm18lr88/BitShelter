using System;
using System.ServiceProcess;

namespace BitShelter.Service.Jobs
{
  // Restarts the Volume Shadow Copy service. A snapshot rule can ask for this before a retry,
  // because a VSS service in a bad state is a common cause of repeated snapshot failures.
  internal static class VssServiceControl
  {
    public const string ServiceName = "VSS";

    public static void Restart(TimeSpan timeout)
    {
      using (var controller = new ServiceController(ServiceName))
      {
        if (controller.Status != ServiceControllerStatus.Stopped)
        {
          if (controller.Status != ServiceControllerStatus.StopPending)
            controller.Stop();

          controller.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
        }

        controller.Start();
        controller.WaitForStatus(ServiceControllerStatus.Running, timeout);
      }
    }
  }
}

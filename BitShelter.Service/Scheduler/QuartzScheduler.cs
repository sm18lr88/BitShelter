using Quartz;
using System.Threading.Tasks;

namespace BitShelter.Service.Scheduler
{
  // https://www.quartz-scheduler.net/documentation/quartz-4.x/tutorial/standalone-scheduler.html
  class QuartzScheduler
  {
    public IScheduler Scheduler { get; private set; }

    protected static QuartzScheduler _instance = null;
    public static QuartzScheduler Instance
    {
      get
      {
        return _instance ?? (_instance = new QuartzScheduler());
      }
    }

    protected QuartzScheduler() { }

    public async Task Start()
    {
      // In-memory job store: rules are persisted by ConfigMgr and triggers are rebuilt from them at startup.
      Scheduler = await QuartzSchedulerBuilder
        .Create(q => q
          .ConfigureScheduler(o => o.InstanceName = Const.AppName)
          .UseInMemoryStore())
        .BuildScheduler();

      await Scheduler.Start();
    }

    public async Task Shutdown()
    {
      await Scheduler.Shutdown();
    }
  }
}

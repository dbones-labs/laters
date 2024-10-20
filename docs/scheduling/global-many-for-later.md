---
outline: deep
---

# Global Many For Later - CRON

> [!NOTE]
> You must provide the ID for a ManyForLater

Global CRON jobs are setup at the application scope (at startup)

They are seen in the global (cross cutting) scope. (if you require to setup a cron for a particular item, then check out ManyForLater).

## GlobalSchedules

By implementing the `GlobalSchedules` interface, we can define global cron jobs, at appplication startup.

- 1️⃣ - this is the set of Global Cron Jobs, if we remove one, it will delete it.

```csharp
public class GlobalSchedules : GlobalSchedules
{
    public void Configure(IScheduleCron scheduleCron)
    {
        var midnight = "0 0 * * *";
        // 1️⃣
        scheduleCron.ManyForLater("joiners", new HelloNewPeople(),  midnight); 
        scheduleCron.ManyForLater("leavers", new ByeToTheLeavers(),  midnight);
    }
}
```

once a CRON job has been created, it will initiate the first job (which will be delayed execution, to the fisrt occourance of the CRON schedule).

### The Payload

Similar to ForLater, the payload `new HelloNewPeople()` in the example, is important as this is passed into the `Handler`, to prodive it all the information it rquires to execute.

```csharp
app.MapHandler<HelloNewPeople>(async (JobContext<HelloNewPeople> ctx) =>
{
   //the HelloNewPeople object is passed into the handler, via the JobContext.
});
```

## Configure

In order to register the `GlobalSchedules` class from the example above

- 1️⃣ - Apply the `ConfigureLaters` located on the `HostBuilder`
- 2️⃣ - `AddSetupSchedule` against any `GlobalSchedules` you would like to register. (you can register many).

```csharp
//1️⃣
builder.WebHost.ConfigureLaters((context, setup) =>
{
    //2️⃣
   setup.AddSetupSchedule<GlobalSchedules>();

    //....
});
```



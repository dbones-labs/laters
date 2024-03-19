---
outline: deep
---

# Many For Later - CRON

This allows you to enqueue work which happens multiple times (such as every wednesday), defined use the Cron Syntax

there are a few commands we can do, here are some to give you a feel of the API.

## ManyForLater - Configure Dynamically

In any part of your code, within a transaction, you can setup a CRON job

```csharp
var midnight = "0 0 * * *";
schedule.ManyForLater("greetings", new Hello { Name = "dave" },  midnight)
```

once a CRON job has been created, it will initiate the first job (which will be delayed execution, to the fisrt occourance of the CRON schedule).

### The Payload

Similar to ForLater, the payload `new Hello { Name = "dave" }` in the example, is important as this is passed into the `Handler`, to prodive it all the information it rquires to execute.

```csharp
app.MapHandler<Hello>(async (JobContext<Hello> ctx) =>
{
   //the hello object is passed into the handler, via the JobContext.
});
```

## ForgetAboutIt - Removing the CRON

> [!NOTE]
> There is an option to keep or delete orphined tasks, as a CRON will always have 1 task (the next) waiting to be processed.

This will allow you to delete any CRON job you have setup

```csharp
schedule.ForgetAboutAllOfIt<Hello>("greetings")
```
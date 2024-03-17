---
outline: deep
---

# ForLater - Fire-Forget and delayed.

> [!NOTE]
> `Laters` checks the datastore every 3 seconds, this can be over-ridden.

This mechanism allows you to queue work (a single instance) to be processed now on another process/thread, or delay it to `Laters` (to a DateTime).

There are a few commands we can do, here are some to give you a feel of the API.

## ForLater - ASAP.

> [!NOTE]
> the job id is returned, incase you need to reference it

This will queue work to happen as soon as possible.

```csharp
schedule.ForLater(new Hello { Name = "dave" } )
```

### The Payload.

The payload `new Hello { Name = "dave" }` in the example, is important as this is passed into the `Handler, to produce all the information it requires to execute.

```csharp
app.MapHandler<Hello>(async (JobContext<Hello> ctx) =>
{
   //the hello object is passed into the handler, via the JobContext.
});
```

## ForLater - Delayed.

Same as the example before, however, we can supply a `DateTime` of when we want to process the one message.

```csharp
var whenToProcess = new DateTime(2032, 01, 02, 12,12, 12);
schedule.ForLater(new Hello { Name = "dave" },  whenToProcess)
```

## ForgetAboutIt - Removing a Job.

using the `JobId` which is returned from `Laters` you can delete jobs, as follows:

```csharp
schedule.ForgetAboutIt<Hello>(jobId);
```
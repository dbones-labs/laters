---
outline: deep
---

# Rate Limiting

This is a powerful mechanism which throttles jobs, for more information please read the [Performace](./performance.md) for more information on how this works

In this page we show how we can setup windows and then queue jobs against a window

## Config

The windows are setup in the config (`appsettings.json`)

Add the windows, here we add `green` and `blue`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Laters" : {
    "Windows": {
      "green": {
        "Max": 500,
        "SizeInSeconds": 360
      },
      "blue": {
        "Max": 200,
        "SizeInSeconds": 60
      }
    }
  }
}
```

### override global

> [!NOTE]
> All jobs will be rate limited through the global window, so remember to set this high enough for your total throughput.

`global` is always present, with defaults, however you can override it

```json
{
  //other config
  "Laters" : {
    "Windows": {
      "gloabl": {
        "Max": 20000,
        "SizeInSeconds": 5
      }
    }
  }
}
```

## Code

Each job that is queued will be processed in a window, and you can supply the name using the options.

### Lookup

Setup constaints for each window name

Although not required this will help you with magic strings 

```csharp
public static class Windows {
    public const string Blue = "blue";
    public const string Gree = "green";
}
```

### Enqueue into a window

To queue the job and throttle with a window, you will need to provide it via the `options` and say which `WindowName`

Here is how we could setup a job to be rate-limited in the `blue` window:

```csharp
var options = new OnceOptions();
options.Delivery.WindowName = Windows.Blue;

var sayHello = new SayHello { Name = "bob" };

schedule.ForLater(sayHello, options);
```
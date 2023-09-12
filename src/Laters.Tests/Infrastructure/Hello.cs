namespace Laters.Tests.Infrastructure;


public class TestMonitor
{
    public IDictionary<object, int> CallTicks { get; set; } = new Dictionary<object, int>();
    public List<Type> CallOrder { get; set; } = new();
    public IDictionary<object, int> CreationTicks { get; set; } = new Dictionary<object, int>();
    public IDictionary<object, int> DisposeTicks { get; set; } = new Dictionary<object, int>();

    public int NumberOfCallTicksFor<T>()
    {
        var count = CallTicks.Where(item => item.Key.GetType() == typeof(T)).Sum(item => item.Value);
        return count;
    }

    public int NumberOfCtorTicksFor<T>()
    {
        var count = CreationTicks.Where(item => item.Key.GetType() == typeof(T)).Sum(item => item.Value);
        return count;
    }

    public int NumberOfDisposalTicksFor<T>()
    {
        var count = DisposeTicks.Where(item => item.Key.GetType() == typeof(T)).Sum(item => item.Value);
        return count;
    }

    public void AddCallTick(object instance)
    {
        if (!CallTicks.ContainsKey(instance))
            CallTicks.Add(instance, 0);

        CallTicks[instance]++;

        CallOrder.Add(instance.GetType());
    }

    public void AddCtorTick(object instance)
    {
        if (!CreationTicks.ContainsKey(instance))
            CreationTicks.Add(instance, 0);

        CreationTicks[instance]++;
    }

    public void AddDisposalTick(object instance)
    {
        if (!DisposeTicks.ContainsKey(instance))
            DisposeTicks.Add(instance, 0);

        DisposeTicks[instance]++;
    }

}
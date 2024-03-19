namespace Laters.Configuration;

using System.Reflection;
using Data;
using Laters.ClientProcessing.Middleware;

public class Setup
{
    List<Type> _jobHandlerTypes = new();
    List<Type> _setupScheduleTypes = new();
    StorageSetup _storageSetup;

    /// <summary>
    /// update the ioc config
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <exception cref="MissingStorageConfigurationException"></exception>
    internal void Apply(IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        
        if (_storageSetup == null) throw new MissingStorageConfigurationException();
        _storageSetup.Apply(serviceCollection);

        var jobHandlerType = typeof(IJobHandler<>);
        foreach (var type in _jobHandlerTypes)
        {
            var handlerInterfaceType = jobHandlerType.MakeGenericType(GetImplementedType(type, jobHandlerType));
            serviceCollection.AddScoped(handlerInterfaceType,type);
        }

        foreach (var setupScheduleType in _setupScheduleTypes)
        {
            serviceCollection.AddTransient(typeof(ISetupSchedule), setupScheduleType);
        }
    }


    public void AddJobHandler<T>()
    {
        AddJobHandler(typeof(T));
    }
    
    public void AddJobHandler(Type handlerType)
    {
        var jobHandlerType = typeof(IJobHandler<>);
        var handlesJobType = GetImplementedType(handlerType, jobHandlerType);

        if (handlesJobType is null)
        {
            throw new NotSupportedException($"{handlerType} does not implement {jobHandlerType.Name}");
        }
        
        _jobHandlerTypes.Add(handlerType);
    }
    
    /// <summary>
    /// scan an assembly for &lt;see cref="IJobHandler{T}"/&gt; and wire them up, ready for use
    /// </summary>
    /// <typeparam name="T">a type which is within the target assembly</typeparam>
    public void ScanForJobHandlers<T>()
    {
        ScanForJobHandlers(typeof(T).Assembly);
    }

    /// <summary>
    /// scan an assembly for <see cref="IJobHandler{T}"/> and wire them up, ready for use
    /// </summary>
    /// <param name="fromHere">the target assembly</param>
    public void ScanForJobHandlers(Assembly? fromHere = null)
    {
        var jobHandlerType = typeof(IJobHandler<>);

        //default to the running project
        fromHere ??= Assembly.GetCallingAssembly(); 
        
        var types = fromHere.GetTypes()
            .Where(x => x.IsClass && !x.IsAbstract)
            .Where(ShouldIncludeType)
            .Select(x => new
            {
                JobType = GetImplementedType(x, jobHandlerType),
                HandlerType = x
            })
            .Where(x => x.JobType is not null);

        _jobHandlerTypes = types
            .Select(x => x.HandlerType)
            .Union(_jobHandlerTypes)
            .Distinct()
            .ToList();
    }
    
    
    public void AddSetupSchedule<T>() where T: ISetupSchedule
    {
        AddSetupSchedule(typeof(T));
    }
    
    public void AddSetupSchedule(Type setupScheduleType)
    {
        var isSetupScheduleType = setupScheduleType.IsAssignableTo(typeof(ISetupSchedule));

        if (!isSetupScheduleType)
        {
            throw new NotSupportedException($"{setupScheduleType} does not implement {typeof(ISetupSchedule).Name}");
        }
        
        _setupScheduleTypes.Add(setupScheduleType);
    }
    
    /// <summary>
    /// scan an assembly for <see cref="ISetupSchedule"/>
    /// </summary>
    /// <param name="fromHere">the target assembly</param>
    public void ScanForCronSetups(Assembly? fromHere = null)
    {
        var setupScheduleType = typeof(ISetupSchedule);

        //default to the running project
        fromHere ??= Assembly.GetCallingAssembly();

        _setupScheduleTypes = fromHere.GetTypes()
                .Where(x => x.IsClass && !x.IsAbstract)
                .Where(x => x.IsAssignableTo(setupScheduleType))
                .Where(ShouldIncludeType)
                .Distinct()
                .ToList();
    }
    
    /// <summary>
    /// this is the laters configuration, which you can configure directly or via this class
    /// </summary>
    public LatersConfiguration Configuration { get; set; }
    
    /// <summary>
    /// this is the raw configuration section, please leave this alone
    /// </summary>
    public IConfigurationSection ConfigurationSection { get; set; }
    
    /// <summary>
    /// this is where we can setup the middleware processing of any job
    /// </summary>
    public ClientActions ClientActions { get; set; }
    
    /// <summary>
    /// mainly used the find if the type implements <see cref="jobHandlerType"/> and what the type it is against.
    /// </summary>
    /// <param name="svcType">the type we are inspecting to see if it implements the desired type</param>
    /// <param name="jobHandlerType">the type we are looking for</param>
    /// <returns>the generic type it implements</returns>
    static Type? GetImplementedType(Type svcType, Type jobHandlerType)
    {
        if (svcType.IsInterface && svcType.IsGenericType &&
            svcType.GetGenericTypeDefinition() == jobHandlerType)
        {
            return svcType.GetGenericArguments().First();
        }

        return svcType.GetInterfaces()
            .Select(x => GetImplementedType(x, jobHandlerType))
            .FirstOrDefault(x => x != null);
    }

    private bool ShouldIncludeType(Type type)
    {
        return !type.GetCustomAttributes(typeof(IgnoreAttribute), false).Any();
    }

    public void UseStorage<T>(Action<T>? storage = null) where T : StorageSetup, new()
    {
        var storageSetup = new T();
        storage?.Invoke(storageSetup);
        _storageSetup = storageSetup;
    }
}
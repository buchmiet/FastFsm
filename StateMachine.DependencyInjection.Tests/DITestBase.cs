using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Xunit;

namespace StateMachine.Tests.DI;

/// <summary>
/// Base class for all DI-related tests
/// </summary>
public abstract class DITestBase : IDisposable
{
    protected IServiceCollection Services { get; }
    protected ServiceProvider? Provider { get; private set; }

    protected DITestBase()
    {
        Services = new ServiceCollection();

        // Add common services that might be needed
        Services.AddLogging();
    }

    /// <summary>
    /// Build the service provider. Call this after all services are registered.
    /// </summary>
    protected void BuildProvider()
    {
        Provider = Services.BuildServiceProvider();
    }

    /// <summary>
    /// Get a required service from the container
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        if (Provider == null)
            throw new InvalidOperationException("Call BuildProvider() first");

        return Provider.GetRequiredService<T>();
    }

    /// <summary>
    /// Get an optional service from the container
    /// </summary>
    protected T? GetOptionalService<T>() where T : class
    {
        return Provider?.GetService<T>();
    }

    /// <summary>
    /// Create a scope for testing scoped services
    /// </summary>
    protected IServiceScope CreateScope()
    {
        if (Provider == null)
            throw new InvalidOperationException("Call BuildProvider() first");

        return Provider.CreateScope();
    }

    /// <summary>
    /// Verify that a service is registered
    /// </summary>
    protected void AssertServiceRegistered<T>() where T:class
    {
        BuildProvider();
        var service = GetOptionalService<T>();
        Assert.NotNull(service);
    }

    /// <summary>
    /// Verify that a service is NOT registered
    /// </summary>
    protected void AssertServiceNotRegistered<T>() where T : class
    {
        BuildProvider();
        var service = GetOptionalService<T>();
        Assert.Null(service);
    }

    /// <summary>
    /// Helper to test service lifetime
    /// </summary>
    protected void AssertServiceLifetime<T>(ServiceLifetime expectedLifetime) where T : notnull
    {
        var descriptor = Services.FirstOrDefault(d => d.ServiceType == typeof(T));
        Assert.NotNull(descriptor);
        Assert.Equal(expectedLifetime, descriptor.Lifetime);
    }

    public virtual void Dispose()
    {
        Provider?.Dispose();
    }
}
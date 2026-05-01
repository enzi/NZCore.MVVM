// <copyright project="Assembly-CSharp" file="DIContainer.cs">
// Copyright © 2025 Thomas Enzenebner. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NZCore.Inject
{
    /// <summary>
    /// Implementation of a dependency injection container.
    /// </summary>
    public class ServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, IServiceDescriptor> _serviceDescriptors = new();
        private readonly Dictionary<Type, object> _singletonInstances = new();
        private readonly Dictionary<Type, object> _scopedInstances = new();
        private readonly IServiceProvider _rootProvider;
        private bool _disposed;

        /// <summary>
        /// Creates a new root container.
        /// </summary>
        public ServiceProvider()
        {
            _rootProvider = this;
        }

        /// <summary>
        /// Creates a new scoped container.
        /// </summary>
        private ServiceProvider(IServiceProvider rootProvider)
        {
            this._rootProvider = rootProvider;
        }

        /// <summary>
        /// Registers a service with the container.
        /// </summary>
        public void Register(IServiceDescriptor descriptor)
        {
            _serviceDescriptors[descriptor.ServiceType] = descriptor;
        }

        /// <summary>
        /// Registers a service type with a specific implementation type and lifetime.
        /// </summary>
        public void Register<TService, TImplementation>(ServiceLifetime lifetime) where TImplementation : class, TService
        {
            Register(new ServiceDescriptor(typeof(TService), typeof(TImplementation), lifetime));
        }

        /// <summary>
        /// Registers a service type with a factory and lifetime.
        /// </summary>
        public void Register<TService>(ServiceLifetime lifetime, Func<IServiceProvider, object> factory) where TService : class
        {
            Register(new ServiceDescriptor(typeof(TService), null, lifetime, factory));
        }

        /// <summary>
        /// Registers a singleton instance.
        /// </summary>
        public void RegisterSingleton<TService>(TService instance) where TService : class
        {
            Register(new ServiceDescriptor(typeof(TService), typeof(TService), ServiceLifetime.Singleton, _ => instance));
            _singletonInstances[typeof(TService)] = instance;
            // Keep root cache in sync so re-registration on a scoped container doesn't return a stale instance
            if (_rootProvider != this)
            {
                ((ServiceProvider)_rootProvider)._singletonInstances[typeof(TService)] = instance;
            }
        }

        /// <summary>
        /// Resolves a service from the container.
        /// </summary>
        public T Resolve<T>() where T : class => (T)Resolve(typeof(T));

        /// <summary>
        /// Resolves a service from the container.
        /// </summary>
        public object Resolve(Type serviceType)
        {
            if (!_serviceDescriptors.TryGetValue(serviceType, out var descriptor))
            {
                throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered.");
            }

            return GetInstance(descriptor);
        }

        /// <summary>
        /// Creates a new scope.
        /// </summary>
        public IServiceProvider CreateScope() => new ServiceProvider(_rootProvider);

        /// <summary>
        /// Gets an instance of a service based on its descriptor.
        /// </summary>
        private object GetInstance(IServiceDescriptor descriptor)
        {
            return descriptor.Lifetime switch
            {
                ServiceLifetime.Singleton => GetSingletonInstance(descriptor),
                ServiceLifetime.Scoped => GetScopedInstance(descriptor),
                ServiceLifetime.Transient => CreateInstance(descriptor),
                _ => throw new InvalidOperationException($"Unsupported lifetime: {descriptor.Lifetime}")
            };
        }

        /// <summary>
        /// Gets or creates a singleton instance.
        /// </summary>
        private object GetSingletonInstance(IServiceDescriptor descriptor)
        {
            var rootContainer = (ServiceProvider)_rootProvider;
            if (rootContainer._singletonInstances.TryGetValue(descriptor.ServiceType, out var instance))
            {
                return instance;
            }

            instance = CreateInstance(descriptor);
            rootContainer._singletonInstances[descriptor.ServiceType] = instance;
            return instance;
        }

        /// <summary>
        /// Gets or creates a scoped instance.
        /// </summary>
        private object GetScopedInstance(IServiceDescriptor descriptor)
        {
            if (_scopedInstances.TryGetValue(descriptor.ServiceType, out var instance))
            {
                return instance;
            }

            instance = CreateInstance(descriptor);
            _scopedInstances[descriptor.ServiceType] = instance;
            return instance;
        }

        public T CreateInstance<T>() => (T)CreateInstance(typeof(T));

        public object CreateInstance(Type implementationType) => CreateInstance(implementationType, null);

        public object CreateInstance(IServiceDescriptor descriptor) => CreateInstance(descriptor.ImplementationType, descriptor.Factory);

        /// <summary>
        /// Creates a new instance of a service.
        /// </summary>
        private object CreateInstance(Type implementationType, Func<IServiceProvider, object> factory)
        {
            if (factory != null)
            {
                return factory(this);
            }

            if (implementationType == null)
            {
                throw new InvalidOperationException($"No implementation type specified!");
            }

            // Check if there's a parameterless constructor
            var ctor = implementationType.GetConstructor(Type.EmptyTypes);
            if (ctor != null)
            {
                var instance = Activator.CreateInstance(implementationType);
                Inject(implementationType, instance);
                return instance;
            }
            else
            {
                // Otherwise, resolve constructor parameters
                var constructors = implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                if (constructors.Length == 0)
                {
                    throw new InvalidOperationException($"No public constructor found for {implementationType.Name}.");
                }

                // Use the constructor with the most parameters
                var constructor = constructors[0];
                foreach (var c in constructors)
                {
                    if (c.GetParameters().Length > constructor.GetParameters().Length)
                    {
                        constructor = c;
                    }
                }

                var parameters = constructor.GetParameters();
                var parameterInstances = new object[parameters.Length];

                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameterType = parameters[i].ParameterType;

                    try
                    {
                        parameterInstances[i] = Resolve(parameterType);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to resolve parameter '{parameters[i].Name}' of type '{parameterType.Name}' for constructor of" +
                            $" '{implementationType.Name}'. Make sure the dependency is registered in the Service Provider.", ex);
                    }
                }

                var instance = Activator.CreateInstance(implementationType, parameterInstances);
                
                Inject(implementationType, instance);

                return instance;
            }
        }

        public void Inject<T>(T instance) => Inject(typeof(T), instance);

        public void Inject(Type implementationType, object instance)
        {
            var props = implementationType
                        .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(m => m.GetCustomAttribute<InjectAttribute>() != null);

            foreach (var property in props)
            {
                property.SetValue(instance, Resolve(property.PropertyType));
            }

            var fields = implementationType
                         .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                         .Where(m => m.GetCustomAttribute<InjectAttribute>() != null);

            foreach (var field in fields)
            {
                field.SetValue(instance, Resolve(field.FieldType));
            }
        }

        /// <summary>
        /// Disposes the container and all scoped instances.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // Dispose scoped instances that implement IDisposable
            foreach (var instance in _scopedInstances.Values)
            {
                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            // Only dispose singleton instances if this is the root container
            if (this == _rootProvider)
            {
                var rootContainer = (ServiceProvider)_rootProvider;
                foreach (var instance in rootContainer._singletonInstances.Values)
                {
                    if (instance is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }

            _scopedInstances.Clear();
        }
    }
}
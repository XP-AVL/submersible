using System;
using System.Collections.Generic;

namespace Submersible.Runtime.Core
{
    /// <summary>
    /// A service which can be registered with the service locator.
    /// </summary>
    public abstract class Service
    {
    }

    /// <summary>
    /// Represents a service that is automatically registered and unregistered
    /// with the service locator upon creation and disposal.
    /// </summary>
    public abstract class AutoRegisteredService<T> : Service, IDisposable where T : AutoRegisteredService<T>
    {
        protected AutoRegisteredService()
        {
            ServiceLocator.RegisterService((T) this);
        }
        
        public virtual void Dispose()
        {
            ServiceLocator.UnregisterService((T) this);
        }
    }
    
    /// <summary>
    /// Provides a mechanism for retrieving common services without
    /// having to use singletons or other problematic patterns.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, Service> Services = new();
        
        /// <summary>
        /// Register an instance of a service.
        /// </summary>
        /// <param name="service">The service instance</param>
        /// <typeparam name="T">The type of the service.</typeparam>
        public static void RegisterService<T>(T service) where T : Service
        {
            Services[typeof(T)] = service;
        }

        /// <summary>
        /// Unregister an instance of a service.
        /// </summary>
        /// <param name="service">The service instance to unregister.</param>
        /// <typeparam name="T">The type of the service.</typeparam>
        public static void UnregisterService<T>(T service) where T : Service
        {
            if (Services.TryGetValue(typeof(T), out var obj) && obj == service)
            {
                Services.Remove(typeof(T));
            }
        }

        /// <summary>
        /// Unregister all registered services.
        /// </summary>
        public static void UnregisterAllServices()
        {
            Services.Clear();
        }

        /// <summary>
        /// Try to get a service instance.
        /// </summary>
        /// <param name="service">The service instance, if it has been registered.</param>
        /// <typeparam name="T">The type of the service.</typeparam>
        /// <returns></returns>
        public static bool TryGetService<T>(out T service) where T : Service
        {
            if (Services.TryGetValue(typeof(T), out var obj))
            {
                service = (T) obj;
                return true;
            }
            
            service = null;
            return false;
        }
    }
}

using System;
using System.Collections.Generic;

namespace Submersible.Runtime.Core
{
    public interface IService
    {
    }
    
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, IService> Services = new();
        
        public static void RegisterService<T>(T service) where T : IService
        {
            Services[typeof(T)] = service;
        }
        
        public static bool TryGetService<T>(out T service) where T : IService
        {
            if (Services.TryGetValue(typeof(T), out var obj))
            {
                service = (T) obj;
                return true;
            }
            
            service = default;
            return false;
        }
    }
}

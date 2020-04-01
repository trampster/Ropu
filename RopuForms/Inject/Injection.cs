using System;
using System.Collections.Generic;
using Client.NoAudio;
using Ropu.Client;
using Ropu.Client.JitterBuffer;
using Ropu.Client.Opus;
using Ropu.Shared;
using Ropu.Shared.Groups;
using Ropu.Shared.LoadBalancing;
using Ropu.Shared.OpenSsl;
using Ropu.Shared.Web;
using Ropu.Shared.WebModels;
using RopuForms.Services;
using RopuForms.ViewModels;
using RopuForms.Views;
using Singularity;

namespace RopuForms.Inject
{
    public class Injection
    {
        static Injection _container = new Injection();

        public static void RegisterTypes(Action<Injection> builder)
        {
            builder(_container);
        }

        public static T Resolve<T>() where T : class
        {
            return _container.Get<T>();
        }

        Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();
        Dictionary<Type, object> _singletons = new Dictionary<Type, object>();

        public Injection Register<T>(Func<Injection, T> create)
        {
            _factories.Add(typeof(T), () => (object)create(this));
            return this;
        }

        public Injection RegisterSingleton<T>(Func<Injection, T> create)
        {
            _factories.Add(typeof(T), () => (object)create(this));
            _singletons.Add(typeof(T), null);
            return this;
        }

        public T Get<T>()
        {
            if(_singletons.TryGetValue(typeof(T), out var singleton))
            {
                if(singleton != null)
                {
                    return (T)singleton;
                }
                if(!_factories.TryGetValue(typeof(T), out var singletonFactory))
                {
                    throw new InvalidOperationException($"No factory found for singleton type {typeof(T)}");
                }
                var created = singletonFactory();
                _singletons[typeof(T)] = (T)created;
                return (T)created;
            }
            if (!_factories.TryGetValue(typeof(T), out var factory))
            {
                throw new InvalidOperationException($"No factory found for type {typeof(T)}");
            }
            return (T)factory();
        }
    }
}

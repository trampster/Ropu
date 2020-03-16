using System;
using System.Collections.Generic;
using Ropu.Client;
using Ropu.Shared.Web;
using RopuForms.Services;
using RopuForms.ViewModels;
using RopuForms.Views;
using Singularity;

namespace RopuForms.Inject
{
    public class Injection
    {
        static Injection _container;

        public static T Resolve<T>() where T : class
        {
            if (_container == null)
            {
                _container = new Injection()
                    .RegisterSingleton<IClientSettings>(i => new FormsClientSettings())
                    .RegisterSingleton(i => new CredentialsProvider())
                    .RegisterSingleton(i => new RopuWebClient("https://192.168.1.7:5001/", i.Get<CredentialsProvider>()))
                    .RegisterSingleton<INavigationService>(i => new Navigator())
                    .RegisterSingleton(i => new LoginViewModel(i.Get<IClientSettings>(), i.Get<INavigationService>(), i.Get<RopuWebClient>(), i.Get<CredentialsProvider>()))
                    .RegisterSingleton(i => new LoginPage(i.Get<LoginViewModel>()))
                    .RegisterSingleton(i => new MainViewModel(i.Get<IClientSettings>(), i.Get<INavigationService>()))
                    .RegisterSingleton(i => new MainPage(i.Get<MainViewModel>()))
                    .RegisterSingleton(i => new PttViewModel());
            }
            return _container.Get<T>();
        }

        Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();
        Dictionary<Type, object> _singletons = new Dictionary<Type, object>();

        Injection Register<T>(Func<Injection, T> create)
        {
            _factories.Add(typeof(T), () => (object)create);
            return this;
        }

        Injection RegisterSingleton<T>(Func<Injection, T> create)
        {
            _factories.Add(typeof(T), () => (object)create(this));
            _singletons.Add(typeof(T), null);
            return this;
        }

        T Get<T>()
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

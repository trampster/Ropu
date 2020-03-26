using System;
using System.Collections.Generic;
using Client.NoAudio;
using Ropu.Client;
using Ropu.Client.JitterBuffer;
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
        static Injection _container;

        public static T Resolve<T>() where T : class
        {
            const ushort ControlPortStarting = 5061;

            if (_container == null)
            {
                _container = new Injection()
                    .RegisterSingleton<IClientSettings>(i => new FormsClientSettings())
                    .RegisterSingleton(i => new CredentialsProvider())
                    .RegisterSingleton(i => new RopuWebClient("https://192.168.1.7:5001/", i.Get<CredentialsProvider>()))
                    .RegisterSingleton<INavigationService>(i => new Navigator())
                    .RegisterSingleton<ICredentialsStore>(i => new CredentialsStore())
                    .RegisterSingleton(i => new LoginViewModel(i.Get<IClientSettings>(), i.Get<INavigationService>(), i.Get<RopuWebClient>(), i.Get<CredentialsProvider>(), i.Get<ImageService>(), i.Get<ICredentialsStore>()))
                    .RegisterSingleton(i => new LoginPage(i.Get<LoginViewModel>()))
                    .RegisterSingleton(i => new MainViewModel(i.Get<IClientSettings>(), i.Get<INavigationService>()))
                    .RegisterSingleton(i => new MainPage(i.Get<MainViewModel>()))
                    .RegisterSingleton(i => new ImageClient(i.Get<RopuWebClient>()))
                    .RegisterSingleton<IGroupsClient>(i => new GroupsClient(i.Get<RopuWebClient>(), i.Get<ImageClient>()))
                    .RegisterSingleton<Func<byte[], IAesGcm>>(i => key => new AesGcmOpenSsl(key))
                    .RegisterSingleton<Func<EncryptionKey, CachedEncryptionKey>>(i => encryptionKey => new CachedEncryptionKey(encryptionKey, i.Get<Func<byte[], IAesGcm>>()))
                    .RegisterSingleton(i => new KeysClient(i.Get<RopuWebClient>(), false, i.Get<Func<EncryptionKey, CachedEncryptionKey>>()))
                    .RegisterSingleton(i => new PacketEncryption(i.Get<KeysClient>()))
                    .RegisterSingleton(i => new ProtocolSwitch(ControlPortStarting, i.Get<IPortFinder>(), i.Get<PacketEncryption>(), i.Get<KeysClient>(), i.Get<IClientSettings>()))
                    .RegisterSingleton(i => new ServingNodeClient(i.Get<ProtocolSwitch>()))
                    .RegisterSingleton<IJitterBuffer>(i => new AdaptiveJitterBuffer(2, 50))
                    .RegisterSingleton<IAudioSource>(i => new NoAudioSource())
                    .RegisterSingleton<IAudioCodec>(i => new NoAudioCodec())
                    .RegisterSingleton<IAudioPlayer>(i => new NoAudioPlayer())
                    .RegisterSingleton<IPortFinder>(i => new MobilePortFinder())
                    .RegisterSingleton<IMediaClient>(i => new MediaClient(
                        i.Get<ProtocolSwitch>(), i.Get<IAudioSource>(), i.Get<IAudioPlayer>(), i.Get<IAudioCodec>(), i.Get<IJitterBuffer>(), i.Get<IClientSettings>()))
                    .RegisterSingleton(i => new LoadBalancerProtocol(i.Get<IPortFinder>(), 5079, i.Get<PacketEncryption>(), i.Get<KeysClient>()))
                    .RegisterSingleton<IBeepPlayer>(i => new NoBeepPlayer())
                    .RegisterSingleton(i => new RopuClient(
                        i.Get<ProtocolSwitch>(), i.Get<ServingNodeClient>(), i.Get<IMediaClient>(), i.Get<LoadBalancerProtocol>(), 
                        i.Get<IClientSettings>(), i.Get<IBeepPlayer>(), i.Get<RopuWebClient>(), i.Get<KeysClient>()))
                    .Register<IUsersClient>(i => new UsersClient(i.Get<RopuWebClient>()))
                    .RegisterSingleton(i => new PttViewModel(i.Get<RopuClient>(), i.Get<IClientSettings>(), i.Get<IGroupsClient>(), i.Get<IUsersClient>(), i.Get<ImageClient>()))
                    .RegisterSingleton(i => new ImageService())
                    .Register(i => new PttPage());
            }
            return _container.Get<T>();
        }

        Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();
        Dictionary<Type, object> _singletons = new Dictionary<Type, object>();

        Injection Register<T>(Func<Injection, T> create)
        {
            _factories.Add(typeof(T), () => (object)create(this));
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

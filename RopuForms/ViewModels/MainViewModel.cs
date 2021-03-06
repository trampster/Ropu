﻿using System.Threading.Tasks;
using Ropu.Client;
using Ropu.Gui.Shared.ViewModels;
using RopuForms.Services;

namespace RopuForms.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        readonly IClientSettings _clientSettings;
        readonly INavigationService _navigator;

        public MainViewModel(IClientSettings clientSettings, INavigationService navigator)
        {
            _clientSettings = clientSettings;
            _navigator = navigator;
        }

        public override async Task Initialize()
        {
            if (_clientSettings.UserId == null)
            {
                await _navigator.ShowModal<LoginViewModel>();
            }
        }
    }
}

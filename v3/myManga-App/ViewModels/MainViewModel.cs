﻿using System;
using System.ComponentModel;
using System.Net;
using System.Windows;
using myManga_App.IO.Network;
using Core.Other.Singleton;

namespace myManga_App.ViewModels
{
    public sealed class MainViewModel : DependencyObject, IDisposable
    {
        #region Content
        private HomeViewModel homeViewModel;
        public HomeViewModel HomeViewModel
        {
            get
            {
                return homeViewModel ?? (homeViewModel = new HomeViewModel());
            }
        }
        #endregion

        private App App = App.Current as App;

        public MainViewModel()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
                App.SiteExtensions.LoadDLL(App.PLUGIN_DIRECTORY, "*.mymanga.dll");
            ServicePointManager.DefaultConnectionLimit = Singleton<SmartMangaDownloader>.Instance.Concurrency + Singleton<SmartChapterDownloader>.Instance.Concurrency;
        }

        public void Dispose() { App.SiteExtensions.Unload(); }
    }
}

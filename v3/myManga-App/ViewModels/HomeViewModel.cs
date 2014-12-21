﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Amib.Threading;
using Core.IO;
using Core.MVVM;
using Core.Other.Singleton;
using myManga_App.IO;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using myManga_App.Properties;
using Core.IO.Storage.Manager.BaseInterfaceClasses;
using myManga_App.IO.ViewModel;
using myManga_App.Objects;
using myManga_App.IO.Network;
using myManga_App.Objects.MVVM;
using myManga_App.Objects.UserInterface;

namespace myManga_App.ViewModels
{
    public sealed class HomeViewModel : BaseViewModel
    {
        #region MangaList
        private static readonly DependencyProperty MangaArchiveCollectionProperty = DependencyProperty.RegisterAttached(
            "MangaArchiveCollection",
            typeof(ObservableCollection<MangaArchiveInformationObject>),
            typeof(HomeViewModel),
            new PropertyMetadata(new ObservableCollection<MangaArchiveInformationObject>()));
        public ObservableCollection<MangaArchiveInformationObject> MangaArchiveCollection
        {
            get { return (ObservableCollection<MangaArchiveInformationObject>)GetValue(MangaArchiveCollectionProperty); }
            set { SetValue(MangaArchiveCollectionProperty, value); }
        }

        private static readonly DependencyProperty SelectedMangaArchiveProperty = DependencyProperty.RegisterAttached(
            "SelectedMangaArchive",
            typeof(MangaArchiveInformationObject),
            typeof(HomeViewModel));
        public MangaArchiveInformationObject SelectedMangaArchive
        {
            get { return (MangaArchiveInformationObject)GetValue(SelectedMangaArchiveProperty); }
            set { SetValue(SelectedMangaArchiveProperty, value); }
        }
        #endregion

        private static readonly DependencyProperty SearchFilterProperty = DependencyProperty.RegisterAttached(
            "SearchFilter",
            typeof(String),
            typeof(HomeViewModel),
            new PropertyMetadata(OnSearchFilterChanged));
        public String SearchFilter
        {
            get { return (String)GetValue(SearchFilterProperty); }
            set { SetValue(SearchFilterProperty, value); }
        }

        private static void OnSearchFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HomeViewModel _this = (d as HomeViewModel);
            _this.MangaListView.Refresh();
            _this.MangaListView.MoveCurrentToFirst();
        }

        private ICollectionView MangaListView;

        private DelegateCommand _ClearSearchCommand;
        public ICommand ClearSearchCommand
        { get { return _ClearSearchCommand ?? (_ClearSearchCommand = new DelegateCommand(ClearSearch, CanClearSearch)); } }
        private void ClearSearch()
        { SearchFilter = String.Empty; }
        private Boolean CanClearSearch()
        { return !String.IsNullOrWhiteSpace(SearchFilter); }

        #region SearchSites
        private DelegateCommand _SearchSiteCommand;
        public ICommand SearchSiteCommand
        { get { return _SearchSiteCommand ?? (_SearchSiteCommand = new DelegateCommand(SearchSites, CanSearchSite)); } }

        private Boolean CanSearchSite()
        { return !String.IsNullOrWhiteSpace(SearchFilter) && (SearchFilter.Trim().Length >= 3); }

        private void SearchSites()
        { Messenger.Default.Send(SearchFilter.Trim(), "SearchRequest"); }
        #endregion

        #region DownloadChapter
        private DelegateCommand<ChapterObject> _DownloadChapterCommand;
        public ICommand DownloadChapterCommand
        { get { return _DownloadChapterCommand ?? (_DownloadChapterCommand = new DelegateCommand<ChapterObject>(DownloadChapter)); } }

        private void DownloadChapter(ChapterObject ChapterObj)
        { DownloadManager.Default.Download(SelectedMangaArchive.MangaObject, ChapterObj); }
        #endregion

        #region ReadChapter
        private DelegateCommand<ChapterObject> _ReadChapterCommand;
        public ICommand ReadChapterCommand
        { get { return _ReadChapterCommand ?? (_ReadChapterCommand = new DelegateCommand<ChapterObject>(ReadChapter)); } }

        private void ReadChapter(ChapterObject ChapterObj)
        {
            String bookmark_chapter_path = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, this.SelectedMangaArchive.MangaObject.MangaFileName());
            MangaObject SelectedMangaObject = this.SelectedMangaArchive.MangaObject;
            if (ChapterObj.IsLocal(bookmark_chapter_path, App.CHAPTER_ARCHIVE_EXTENSION))
                Messenger.Default.Send(new ReadChapterRequestObject(this.SelectedMangaArchive.MangaObject, ChapterObj), "ReadChapterRequest");
            else
                DownloadManager.Default.Download(SelectedMangaObject, ChapterObj);
        }

        private DelegateCommand _ResumeReadingCommand;
        public ICommand ResumeReadingCommand
        { get { return _ResumeReadingCommand ?? (_ResumeReadingCommand = new DelegateCommand(ResumeReading, CanResumeReading)); } }

        private void ResumeReading()
        {
            String bookmark_chapter_path = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, this.SelectedMangaArchive.MangaObject.MangaFileName());
            MangaObject SelectedMangaObject = this.SelectedMangaArchive.MangaObject;
            ChapterObject ResumeChapterObject = (this.SelectedMangaArchive.BookmarkObject != null) ?
                SelectedMangaObject.ChapterObjectOfBookmarkObject(this.SelectedMangaArchive.BookmarkObject) : 
                SelectedMangaObject.Chapters.FirstOrDefault();
            BookmarkObject SelectedBookmarkObject = this.SelectedMangaArchive.BookmarkObject ?? new myMangaSiteExtension.Objects.BookmarkObject()
            {
                Volume = ResumeChapterObject.Volume,
                Chapter = ResumeChapterObject.Chapter,
                SubChapter = ResumeChapterObject.SubChapter,
                Page = 1,
            };
            if (ResumeChapterObject.IsLocal(bookmark_chapter_path, App.CHAPTER_ARCHIVE_EXTENSION))
                Messenger.Default.Send(new ReadChapterRequestObject(SelectedMangaObject, ResumeChapterObject), "ReadChapterRequest");
            else
            {
                Singleton<ZipStorage>.Instance.Write(
                    Path.Combine(App.MANGA_ARCHIVE_DIRECTORY, SelectedMangaObject.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION)),
                    typeof(BookmarkObject).Name,
                    SelectedBookmarkObject.Serialize(SaveType: App.UserConfig.SaveType)
                );
                DownloadManager.Default.Download(SelectedMangaObject, ResumeChapterObject);
            }
        }
        private Boolean CanResumeReading()
        { return !MangaArchiveInformationObject.Equals(this.SelectedMangaArchive, null) && !this.SelectedMangaArchive.Empty(); }
        #endregion

        #region RefreshManga
        private DelegateCommand _RefreshMangaCommand;
        public ICommand RefreshMangaCommand
        { get { return _RefreshMangaCommand ?? (_RefreshMangaCommand = new DelegateCommand(RefreshManga, CanRefreshManga)); } }

        private Boolean CanRefreshManga()
        { return !MangaArchiveInformationObject.Equals(this.SelectedMangaArchive, null) && !this.SelectedMangaArchive.Empty(); }

        private void RefreshManga()
        { DownloadManager.Default.Download(SelectedMangaArchive.MangaObject); }
        #endregion

        #region RefreshMangaList
        private DelegateCommand _RefreshMangaListCommand;
        public ICommand RefreshMangaListCommand
        { get { return _RefreshMangaListCommand ?? (_RefreshMangaListCommand = new DelegateCommand(RefreshMangaList, CanRefreshMangaList)); } }

        private Boolean CanRefreshMangaList()
        { return MangaArchiveCollection.Count > 0; }

        private void RefreshMangaList()
        { foreach (MangaArchiveInformationObject manga_archive in MangaArchiveCollection) DownloadManager.Default.Download(manga_archive.MangaObject); }
        #endregion

        private Boolean _IsLoading;
        public Boolean IsLoading
        {
            get { return _IsLoading; }
            set { SetProperty(ref this._IsLoading, value); }
        }

        public HomeViewModel()
            : base(SupportsViewTypeChange:true)
        {
            if (!IsInDesignMode)
            {
                ConfigureSearchFilter();
                foreach (String MangaArchiveFilePath in Directory.GetFiles(App.MANGA_ARCHIVE_DIRECTORY, App.MANGA_ARCHIVE_FILTER, SearchOption.AllDirectories))
                {
                    MangaArchiveInformationObject manga_archive = LoadMangaArchiveInformationObject(MangaArchiveFilePath);
                    if (!manga_archive.Empty()) MangaArchiveCollection.Add(manga_archive);
                }
                this.SelectedMangaArchive = this.MangaArchiveCollection.FirstOrDefault();
                MangaListView.MoveCurrentToFirst();

                Messenger.Default.RegisterRecipient<FileSystemEventArgs>(this, MangaObjectArchiveWatcher_Event, "MangaObjectArchiveWatcher");
            }
#if DEBUG
#endif
        }

        private void MangaObjectArchiveWatcher_Event(FileSystemEventArgs e)
        {
            MangaArchiveInformationObject current_manga_archive = MangaArchiveCollection.FirstOrDefault(
                o => o.MangaObject.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION) == e.Name);
            Boolean ViewingSelectedMangaObject = this.SelectedMangaArchive != null && this.SelectedMangaArchive.Equals(current_manga_archive);
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Changed:
                case WatcherChangeTypes.Created:
                    MangaArchiveInformationObject new_manga_archive = LoadMangaArchiveInformationObject(e.FullPath);
                    if (!new_manga_archive.Empty())
                    {
                        if (current_manga_archive.Empty())
                            MangaArchiveCollection.Add(new_manga_archive);
                        else
                            current_manga_archive.Merge(new_manga_archive);
                        if (ViewingSelectedMangaObject) this.SelectedMangaArchive = new_manga_archive;
                    }
                    break;

                case WatcherChangeTypes.Deleted:
                    MangaArchiveCollection.Remove(current_manga_archive);
                    break;

                default:
                    break;
            }
        }

        private MangaArchiveInformationObject LoadMangaArchiveInformationObject(String ArchivePath)
        {
            Stream archive_file;
            MangaObject manga_object = null;
            BookmarkObject bookmark_object = null;
            if (Singleton<ZipStorage>.Instance.TryRead(ArchivePath, out archive_file, typeof(MangaObject).Name))
            {
                try
                {
                    if (archive_file.CanRead && archive_file.Length > 0)
                    { manga_object = archive_file.Deserialize<MangaObject>(SaveType: App.UserConfig.SaveType); }
                }
                catch { }
                archive_file.Close();
            }
            if (Singleton<ZipStorage>.Instance.TryRead(ArchivePath, out archive_file, typeof(BookmarkObject).Name))
            {
                try
                {
                    if (archive_file.CanRead && archive_file.Length > 0)
                    { bookmark_object = archive_file.Deserialize<BookmarkObject>(SaveType: App.UserConfig.SaveType); }
                }
                catch { }
                archive_file.Close();
            }
            return new MangaArchiveInformationObject(manga_object, bookmark_object);
        }

        private void ConfigureSearchFilter()
        {
            MangaListView = CollectionViewSource.GetDefaultView(MangaArchiveCollection);
            MangaListView.Filter = mangaArchive =>
            {
                // Show all items if search is empty
                if (String.IsNullOrWhiteSpace(SearchFilter)) return true;
                return (mangaArchive as MangaArchiveInformationObject).MangaObject.IsNameMatch(SearchFilter);
            };
            if (MangaListView.CanSort)
                MangaListView.SortDescriptions.Add(new SortDescription("MangaObject.Name", ListSortDirection.Ascending));
        }
    }
}

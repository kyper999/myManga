﻿using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace myManga_App.Objects.Cache
{
    public sealed class ChapterCacheObject : DependencyObject
    {
        #region Constructors
        private readonly App App = App.Current as App;
        private String initialArchiveFileName;
        public String ArchiveFileName
        {
            get
            {
                if (!Equals(ChapterObject, null))
                    return ChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION);
                return initialArchiveFileName;
            }
            set { initialArchiveFileName = value; }
        }

        public ChapterCacheObject(ChapterObject ChapterObject, Boolean CreateProgressReporter = true) : base()
        {
            DownloadProgressReporter = new Progress<Int32>(ProgressValue =>
            {
                DownloadProgressActive = (0 < ProgressValue && ProgressValue < 100);
                DownloadProgress = ProgressValue;
            });

            this.ChapterObject = ChapterObject;
        }

        public override string ToString()
        {
            if (!Equals(ChapterObject, null))
                return String.Format("[ChapterCacheObject]{0} - {1}.{2}.{3}", ChapterObject.Name, ChapterObject.Volume, ChapterObject.Chapter, ChapterObject.SubChapter);
            return String.Format("{0}", base.ToString());
        }

        public void ForceDataRefresh()
        {
            BindingOperations.GetBindingExpressionBase(this, ChapterObjectProperty).UpdateTarget();
            BindingOperations.GetBindingExpressionBase(this, IsLocalProperty).UpdateTarget();
            BindingOperations.GetBindingExpressionBase(this, IsResumeChapterProperty).UpdateTarget();
        }
        #endregion

        #region Chapter
        private static readonly DependencyPropertyKey ChapterObjectPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "ChapterObject",
            typeof(ChapterObject),
            typeof(ChapterCacheObject),
            null);
        private static readonly DependencyProperty ChapterObjectProperty = ChapterObjectPropertyKey.DependencyProperty;

        public ChapterObject ChapterObject
        {
            get { return (ChapterObject)GetValue(ChapterObjectProperty); }
            internal set { SetValue(ChapterObjectPropertyKey, value); }
        }
        #endregion

        #region Status

        #region IsLocal
        private static readonly DependencyPropertyKey IsLocalPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "IsLocal",
            typeof(Boolean),
            typeof(ChapterCacheObject),
            null);
        private static readonly DependencyProperty IsLocalProperty = IsLocalPropertyKey.DependencyProperty;

        public Boolean IsLocal
        {
            get { return (Boolean)GetValue(IsLocalProperty); }
            internal set { SetValue(IsLocalPropertyKey, value); }
        }
        #endregion

        #region IsResumeChapter
        private static readonly DependencyPropertyKey IsResumeChapterPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "IsResumeChapter",
            typeof(Boolean),
            typeof(ChapterCacheObject),
            null);
        private static readonly DependencyProperty IsResumeChapterProperty = IsResumeChapterPropertyKey.DependencyProperty;

        public Boolean IsResumeChapter
        {
            get { return (Boolean)GetValue(IsResumeChapterProperty); }
            internal set { SetValue(IsResumeChapterPropertyKey, value); }
        }
        #endregion

        #region Progress
        public IProgress<Int32> DownloadProgressReporter
        { get; private set; }

        private static readonly DependencyPropertyKey DownloadProgressPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "DownloadProgress",
            typeof(Int32),
            typeof(ChapterCacheObject),
            new PropertyMetadata(0));
        private static readonly DependencyProperty DownloadProgressProperty = DownloadProgressPropertyKey.DependencyProperty;

        public Int32 DownloadProgress
        {
            get { return (Int32)GetValue(DownloadProgressProperty); }
            private set { SetValue(DownloadProgressPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey DownloadProgressActivePropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "DownloadProgressActive",
            typeof(Boolean),
            typeof(ChapterCacheObject),
            new PropertyMetadata(false));
        private static readonly DependencyProperty DownloadProgressActiveProperty = DownloadProgressActivePropertyKey.DependencyProperty;

        public Boolean DownloadProgressActive
        {
            get { return (Boolean)GetValue(DownloadProgressActiveProperty); }
            private set { SetValue(DownloadProgressActivePropertyKey, value); }
        }
        #endregion

        #endregion
    }
}

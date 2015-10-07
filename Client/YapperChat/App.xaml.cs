using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using GalaSoft.MvvmLight.Messaging;
using YapperChat.EventMessages;
using YapperChat.Models;
using YapperChat.Database;
using YapperChat.Sync;
using YapperChat.ServiceProxy;
using System.ComponentModel;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using Windows.Devices.Geolocation;
using YapperChat.ViewModels;
using Microsoft.Phone.Data.Linq;

namespace YapperChat
{
    public partial class App : Application
    {
        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        // Geolocator instance
        public Geolocator watcher;

        public string PerfTrackerString;

        public Stopwatch PerfTrackerStopWatch = new Stopwatch();

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions. 
            UnhandledException += Application_UnhandledException;

            // Standard Silverlight initialization
            InitializeComponent();

            this.MergeCustomColors();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Disable the application idle detection by setting the UserIdleDetectionMode property of the
                // application's PhoneApplicationService object to Disabled.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

            // Create the data base here if it does not exist already
            using (YapperDataContext locdb = new YapperDataContext())
            {
                if (locdb.DatabaseExists() == false)
                {
                    locdb.CreateDatabase();

                    // Set the database version
                    DatabaseSchemaUpdater dbUpdater = locdb.CreateDatabaseSchemaUpdater();
                    dbUpdater.DatabaseSchemaVersion = YapperDataContext.DbVersion;
                    dbUpdater.Execute();
                }
                else
                {
                    // Check whether a database update is needed.
                    DatabaseSchemaUpdater dbUpdater = locdb.CreateDatabaseSchemaUpdater();

                    if (dbUpdater.DatabaseSchemaVersion < YapperDataContext.DbVersion)
                    {
                        int oldVersion = dbUpdater.DatabaseSchemaVersion;
                        for (int i = oldVersion + 1; i <= YapperDataContext.DbVersion; i++)
                        {
                            if (YapperDataContext.NewColumnsInVersion.ContainsKey(i))
                            {
                                Action<DatabaseSchemaUpdater> updater = YapperDataContext.NewColumnsInVersion[i];
                                updater(dbUpdater);
                            }
                        }

                        // Add the new database version.
                        dbUpdater.DatabaseSchemaVersion = YapperDataContext.DbVersion;

                        // Perform the database update in a single transaction.
                        dbUpdater.Execute();

                        // Post schema upgrade task
                        for (int i = oldVersion + 1; i <= YapperDataContext.DbVersion; i++)
                        {
                            if (YapperDataContext.PostDatabaseUpgradeOperation.ContainsKey(i))
                            {
                                Action updater = YapperDataContext.PostDatabaseUpgradeOperation[i];
                                updater();
                            }
                        }
                    }
                }
            }

            PhoneApplicationService.Current.Activated += Application_Activated;

            watcher = new Geolocator();
            watcher.MovementThreshold = 100;
            watcher.DesiredAccuracy = PositionAccuracy.High;
            watcher.DesiredAccuracyInMeters = 50;
        }

        public Geoposition Currentlocation
        {
            get;
            set;
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            using (YapperDataContext locdb = new YapperDataContext())
            {
                if (locdb.DatabaseExists() == false)
                {
                    locdb.CreateDatabase();
                }
            }

            if (UserSettingsModel.Instance.IsAuthenticated())
            {
                DataSync.Instance.Sync();

                // kick off a background task to sync the conversation list from server
                // to phone and update the observable collection
                BackgroundWorker exceptionWorker = new BackgroundWorker();
                exceptionWorker.DoWork += this.UploadExceptions;
                exceptionWorker.RunWorkerAsync();

                BackgroundWorker unsentWorker = new BackgroundWorker();
                unsentWorker.DoWork += this.UploadUnsentMessages;
                unsentWorker.RunWorkerAsync();
            }
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            if (UserSettingsModel.Instance.IsAuthenticated())
            {
                DataSync.Instance.Sync(true);
            }
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            // Ensure that required application state is persisted here.
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
        }

        private void UploadExceptions(object o, DoWorkEventArgs e)
        {
            AutoResetEvent completedEvent = (AutoResetEvent)e.Argument;

            try
            {
                YapperServiceProxy.Instance.UploadExceptions();
            }
            catch (Exception)
            {
            }
        }

        private void UploadUnsentMessages(object o, DoWorkEventArgs e)
        {
            IEnumerable<MessageModel> unsentMessages = DataSync.Instance.GetUnsentMessages();
            foreach (MessageModel m in unsentMessages)
            {
                try
                {
                    if (!m.IsTaskMessage.Value && !m.IsPollResponseMessage)
                    {
                        YapperServiceProxy.Instance.SendNewMessage(m.EncryptMessage(), DataSync.Instance.NewMessageCreated);
                    }

                    if (m.IsPollResponseMessage)
                    {
                        bool sendMessage = false;
                        if (m.PollMessageId == Guid.Empty && m.PollClientMessageId.HasValue && m.PollClientMessageId.Value != Guid.Empty)
                        {
                            MessageModel message = DataSync.Instance.GetMessageFromClientId(m.PollClientMessageId.Value);
                            if (message.MessageId != Guid.Empty)
                            {
                                m.PollMessageId = message.MessageId;
                                sendMessage = true;
                            }
                        }
                        else if (m.PollMessageId != Guid.Empty)
                        {
                            sendMessage = true;
                        }

                        if (sendMessage)
                        {
                            YapperServiceProxy.Instance.SendNewMessage(m.EncryptMessage(), DataSync.Instance.NewMessageCreated);
                        }
                    }
                }
                catch (Exception)
                {
                    DataSync.Instance.DeleteMessage(m);
                }
            }
        }

        private void MergeCustomColors()
        {
            var dictionaries = new ResourceDictionary();
            string source = String.Format("/YapperChat;component/CustomTheme/ThemeResources.xaml");
            var themeStyles = new ResourceDictionary { Source = new Uri(source, UriKind.Relative) };
            dictionaries.MergedDictionaries.Add(themeStyles);

            ResourceDictionary appResources = App.Current.Resources;
            foreach (DictionaryEntry entry in dictionaries.MergedDictionaries[0])
            {
                SolidColorBrush colorBrush = entry.Value as SolidColorBrush;
                SolidColorBrush existingBrush = appResources[entry.Key] as SolidColorBrush;
                if (existingBrush != null && colorBrush != null)
                {
                    existingBrush.Color = colorBrush.Color;
                    continue;
                }

                if (!appResources.Contains(entry.Key))
                {
                    appResources.Add(entry.Key, entry.Value);
                }
            }
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            UserSettingsModel.Instance.SaveException(e.ExceptionObject.ToString());

            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion
    }
}
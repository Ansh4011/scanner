﻿using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using static Globals;
using static Utilities;

namespace Scanner
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private UISettings uISettings;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            // quickly load theme
            if (ApplicationData.Current.LocalSettings.Values["settingAppTheme"] != null)
            {
                switch ((int)ApplicationData.Current.LocalSettings.Values["settingAppTheme"])
                {
                    case 0:
                        break;
                    case 1:
                        this.RequestedTheme = ApplicationTheme.Light;
                        break;
                    case 2:
                        this.RequestedTheme = ApplicationTheme.Dark;
                        break;
                }
            }

            _ = InitializeSerilogAsync();

            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }


        /// <summary>
        /// Invoked when the application is launched normally by the end user. Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            // Hide default title bar.
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            applicationViewTitlebar = ApplicationView.GetForCurrentView().TitleBar;

            applicationViewTitlebar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
            applicationViewTitlebar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;

            uISettings = new UISettings();
            uISettings.ColorValuesChanged += UpdateTheme;

            // Load settings from local app data
            LoadSettings();

            // Update theme once to ensure that the titlebar buttons are correct
            UpdateTheme(null, null);

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Set minimum width and height
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(500, 500));

                // Ensure the current window is active
                Window.Current.Activate();
            }
        }


        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            // Save application state and stop any background activity
            deferral.Complete();
        }



        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            // app service launched
            base.OnBackgroundActivated(args);

            IBackgroundTaskInstance taskInstance = args.TaskInstance;
            AppServiceTriggerDetails appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            appServiceDeferral = taskInstance.GetDeferral();
            taskInstance.Canceled += AppService_Canceled;

            appServiceConnection = appService.AppServiceConnection;
            appServiceConnection.RequestReceived += AppServiceConnection_RequestReceived;
            appServiceConnection.ServiceClosed += AppServiceConnection_Closed;
        }

        private void AppService_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            appServiceConnection = null;
            appServiceDeferral.Complete();
        }

        private void AppServiceConnection_Closed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            appServiceConnection = null;
            appServiceDeferral.Complete();
        }

        private void AppServiceConnection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            // win32 component finished
            object result;
            args.Request.Message.TryGetValue("RESULT", out result);
            if ((string)result == "SUCCESS") taskCompletionSource.TrySetResult(true);
            else taskCompletionSource.TrySetResult(false);
        }
    }
}

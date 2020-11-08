using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using FilePath = System.IO.Path;

namespace Display.Controls
{
    /// <summary>
    /// Interaction logic for WindowHost.xaml
    /// </summary>
    public partial class WindowHost : UserControl, IDisposable
    {
        #region Member Variables
        private bool _isDisposed;
        private bool _isCreated;

        /// <summary>
        /// Handle to the application Window
        /// </summary>
        private IntPtr _windowHandle;

        /// <summary>
        /// The child process
        /// </summary>
        private Process _childProcess;
        #endregion

        #region Lifecycle
        public WindowHost()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
            Unloaded += OnUnloaded;
        }

        ~WindowHost()
        {
            Dispose();
        }
        #endregion

        #region Dependency Properties
        public string ExePath
        {
            get { return (string)GetValue(ExePathProperty); }
            set { SetValue(ExePathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ExePath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExePathProperty =
            DependencyProperty.Register("ExePath", typeof(string), typeof(WindowHost));



        public string Arguments
        {
            get { return (string)GetValue(ArgumentsProperty); }
            set { SetValue(ArgumentsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Arguments.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ArgumentsProperty =
            DependencyProperty.Register("Arguments", typeof(string), typeof(WindowHost));

        #endregion

        #region Properties
        public IntPtr WindowHandle { get { return _windowHandle; } }
        #endregion

        #region Methods
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_isCreated)
                return;

            if (string.IsNullOrEmpty(ExePath))
                return;

            // Mark that control is created
            _isCreated = true;

            // Initialize handle value to invalid
            _windowHandle = IntPtr.Zero;

            try
            {
                var procInfo = new ProcessStartInfo(ExePath)
                {
                    WorkingDirectory = FilePath.GetDirectoryName(ExePath),
                    Arguments = Arguments
                };

                // Start the process
                _childProcess = Process.Start(procInfo);

                // No process was started, something went wrong so bail out
                if (_childProcess == null)
                {
                    Log.TraceErr("WindowHost: Child process did not start");
                    return;
                }

                // Wait for process to be created and enter idle condition
                if (!_childProcess.WaitForInputIdle())
                {
                    Log.TraceErr("WindowHost: Input idle state was not reached");
                    return;
                }

                // Hack - I think if the app has a splash screen the main window handle changes so we need to wait for the app to become
                // fully loaded before we attempt to grab the window handle and push it into the control.
                System.Threading.Thread.Sleep(4000);
                
                // Get the main handle
                _windowHandle = _childProcess.MainWindowHandle;
            }
            catch (Exception ex)
            {
                Log.TraceErr(ex.Message);
            }

            if (_windowHandle == IntPtr.Zero)
            {
                Log.TraceErr("WindowHost: Couldn't find child window handle");
                return;
            }

            // Put it into this form
            var helper = new WindowInteropHelper(Window.GetWindow(WindowHostContainer));
            if (NativeMethods.SetParent(_windowHandle, helper.Handle) == 0)
            {
                Log.TraceErr("WindowHost: SetParent failed. LastError: {0}", Marshal.GetLastWin32Error());
                return;
            }

            // Remove border and whatnot
            if (NativeMethods.SetWindowLongA(_windowHandle, NativeMethods.GWL_STYLE, NativeMethods.WS_VISIBLE) == 0)
            {
                Log.TraceErr("WindowHost: SetWindowLongA failed. LastError: {0}", Marshal.GetLastWin32Error());
                return;
            }

            // Move the window to overlay it on this window
            if (!NativeMethods.MoveWindow(_windowHandle, 0, 0, (int) ActualWidth, (int) ActualHeight, true))
            {
                Log.TraceErr("WindowHost: MoveWindow failed. LastError: {0}", Marshal.GetLastWin32Error());
                return;
            }
        }

        protected void OnSizeChanged(object s, SizeChangedEventArgs e)
        {
            // Force redraw of control when size changes
            InvalidateVisual();

            // Update display of the executable
            if (_windowHandle != IntPtr.Zero)
            {
                NativeMethods.MoveWindow(_windowHandle, 0, 0, (int)ActualWidth, (int)ActualHeight, true);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }
        #endregion

        #region Implements IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (_isCreated && _windowHandle != IntPtr.Zero && !_childProcess.HasExited)
                    {
                        // Stop the application
                        _childProcess.Kill();

                        // Clear internal handle
                        _windowHandle = IntPtr.Zero;
                    }
                }

                _isDisposed = true;
            }
        }
        #endregion
    }
}

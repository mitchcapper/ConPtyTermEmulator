using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Console;

namespace ConPtyTermEmulatorLib {
    /// <summary>
    /// Utility functions around the new Pseudo Console APIs.
    /// </summary>
    internal sealed class PseudoConsole : IDisposable {
        private bool disposed;
		public bool IsDisposed => disposed;
        public HANDLE Handle { get; }

        private PseudoConsole(IntPtr handle) {
            this.Handle = new (handle);
        }
        public void Resize(int width, int height) {
            PseudoConsoleApi.ResizePseudoConsole(Handle, new COORD { X = (short)width, Y = (short)height });
        }
        const uint PSEUDOCONSOLE_RESIZE_QUIRK = 2u;
        const uint PSEUDOCONSOLE_WIN32_INPUT_MODE = 4u;
        internal static PseudoConsole Create(SafeFileHandle inputReadSide, SafeFileHandle outputWriteSide, int width, int height) {
            var flags = PSEUDOCONSOLE_WIN32_INPUT_MODE | PSEUDOCONSOLE_RESIZE_QUIRK;//causes powershell to isnta crash
            flags = 0;
            var createResult = PseudoConsoleApi.CreatePseudoConsole(
                new COORD { X = (short)width, Y = (short)height },
                inputReadSide, outputWriteSide,
               flags , out IntPtr hPC);
            if (createResult != 0) {
                throw new Win32Exception(createResult, "Could not create pseudo console.");
            }
            return new PseudoConsole(hPC);
        }
        private class NoReleaseSafeHandle : System.Runtime.InteropServices.SafeHandle
        {
        public NoReleaseSafeHandle(IntPtr value)
            : base(IntPtr.Zero, true)
        {
            this.SetHandle(value);
        }

        public override bool IsInvalid => throw new NotImplementedException();

        protected override bool ReleaseHandle()
        {
            return true;
        }
    }
        unsafe public CONSOLE_MODE GetInputMode() {//does not work or pseudo
            //LPDWORD mode;

            //new NoReleaseSafeHandle( Handle.Value)
            CONSOLE_MODE mode=default;
            if (!PInvoke.GetConsoleMode(Handle, &mode))
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            return mode;
        }
        //
        public void SetInputMode(CONSOLE_MODE mode) {//does not work for pseudo
            if (!PInvoke.SetConsoleMode(Handle, mode))
                throw new Exception($"Unable to set console mode to: {mode}");
        }
        private void Dispose(bool disposing) {
            if (!disposed) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                }

                PseudoConsoleApi.ClosePseudoConsole(Handle);
                // TODO: set large fields to null
                disposed = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~PseudoConsole() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }
}

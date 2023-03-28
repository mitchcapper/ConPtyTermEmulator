
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Console;

namespace ConPtyTermEmulatorLib {
	/// <summary>
	/// Utility functions around the new Pseudo Console APIs.
	/// </summary>
	public class PseudoConsole : IDisposable {
		private bool disposed;
		public bool IsDisposed => disposed;
		internal ConPtyClosePseudoConsoleSafeHandle Handle { get; }

		private PseudoConsole(ConPtyClosePseudoConsoleSafeHandle handle) {
			this.Handle = handle;
		}
		public void Resize(int width, int height) {
			PseudoConsoleApi.ResizePseudoConsole(Handle.DangerousGetHandle(), new COORD { X = (short)width, Y = (short)height });
		}
		internal class ConPtyClosePseudoConsoleSafeHandle : ClosePseudoConsoleSafeHandle {
			public ConPtyClosePseudoConsoleSafeHandle(IntPtr preexistingHandle, bool ownsHandle = true) : base(preexistingHandle, ownsHandle) {
			}
			protected override bool ReleaseHandle() {
				PseudoConsoleApi.ClosePseudoConsole(handle);
				return true;
			}
		}
		public static PseudoConsole Create(SafeFileHandle inputReadSide, SafeFileHandle outputWriteSide, int width, int height) {
			var createResult = PseudoConsoleApi.CreatePseudoConsole(
				new COORD { X = (short)width, Y = (short)height },
				inputReadSide, outputWriteSide,
			   0, out IntPtr hPC);
			if (createResult != 0) {
				throw new Win32Exception(createResult, "Could not create pseudo console.");
			}
			return new PseudoConsole(new ConPtyClosePseudoConsoleSafeHandle(hPC));
		}

		private void Dispose(bool disposing) {
			if (!disposed) {
				if (disposing) {
					Handle.Dispose();
				}
				
				// TODO: set large fields to null
				disposed = true;
			}
		}

		public void Dispose() {
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

	}
}

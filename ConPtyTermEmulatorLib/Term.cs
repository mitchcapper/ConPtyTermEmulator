using Microsoft.Terminal.Wpf;
using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.IO;
using Windows.Win32;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;

namespace ConPtyTermEmulatorLib {
	/// <summary>
	/// Class for managing communication with the underlying console, and communicating with its pseudoconsole.
	/// </summary>
	public class Term : ITerminalConnection {
		private static bool IsDesignMode = System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject());
		private SafeFileHandle _consoleInputPipeWriteHandle;
		private StreamWriter _consoleInputWriter;
		public StringBuilder ConsoleOutputLog { get; private set; }
		private static Regex NewlineReduce = new (@"\n\s*?\n\s*?[\s]+", RegexOptions.Singleline);
		public static Regex colorStrip = new(@"((\x1B\[\??[0-9;]*[a-zA-Z])|\uFEFF|\u200B|\x1B\]0;|[\a\b])", RegexOptions.Compiled | RegexOptions.ExplicitCapture); //also strips BOM, bells, backspaces etc
		public string GetConsoleText(bool stripVTCodes=true) => NewlineReduce.Replace( (stripVTCodes ? StripColors(ConsoleOutputLog.ToString()) : ConsoleOutputLog.ToString()).Replace("\r",""),"\n\n").Trim();

		public static string StripColors(String str) {
			return colorStrip.Replace(str, "");
		}

		/// <summary>
		/// A stream of VT-100-enabled output from the console.
		/// </summary>
		public FileStream ConsoleOutStream { get; private set; }

		/// <summary>
		/// Fired once the console has been hooked up and is ready to receive input.
		/// </summary>
		public event EventHandler TermReady;
		public event EventHandler<TerminalOutputEventArgs> TerminalOutput;//how we send data to the UI terminal


		/// <summary>
		/// Start the pseudoconsole and run the process as shown in 
		/// https://docs.microsoft.com/en-us/windows/console/creating-a-pseudoconsole-session#creating-the-pseudoconsole
		/// </summary>
		/// <param name="command">the command to run, e.g. cmd.exe</param>
		/// <param name="consoleHeight">The height (in characters) to start the pseudoconsole with. Defaults to 80.</param>
		/// <param name="consoleWidth">The width (in characters) to start the pseudoconsole with. Defaults to 30.</param>
		public void Start(string command, int consoleWidth = 80, int consoleHeight = 30, bool logOutput=false) {
			if (IsDesignMode) {
				TermReady?.Invoke(this, EventArgs.Empty);
			}
			if (logOutput)
				ConsoleOutputLog = new();
			using (var inputPipe = new PseudoConsolePipe())
			using (var outputPipe = new PseudoConsolePipe())
			using (var pseudoConsole = PseudoConsole.Create(inputPipe.ReadSide, outputPipe.WriteSide, consoleWidth, consoleHeight))
			using (var process = ProcessFactory.Start(command, PInvoke.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE, pseudoConsole.Handle)) {
				TheConsole = pseudoConsole;
				// copy all pseudoconsole output to a FileStream and expose it to the rest of the app
				ConsoleOutStream = new FileStream(outputPipe.ReadSide, FileAccess.Read);
				TermReady?.Invoke(this, EventArgs.Empty);
				// Store input pipe handle, and a writer for later reuse
				_consoleInputPipeWriteHandle = inputPipe.WriteSide;
				_consoleInputWriter = new StreamWriter(new FileStream(_consoleInputPipeWriteHandle, FileAccess.Write)) {
					AutoFlush = true
				};

				// free resources in case the console is ungracefully closed (e.g. by the 'x' in the window titlebar)
				OnClose(() => DisposeResources(process, pseudoConsole, outputPipe, inputPipe, _consoleInputWriter));

				WaitForExit(process).WaitOne(Timeout.Infinite);
				TerminalOutput?.Invoke(this, new TerminalOutputEventArgs("Session Terminated"));
				TheConsole.Dispose();
			}
		}
		PseudoConsole TheConsole;
		/// <summary>
		/// Sends the given string to the anonymous pipe that writes to the active pseudoconsole.
		/// </summary>
		/// <param name="input">A string of characters to write to the console. Supports VT-100 codes.</param>
		public void WriteToTerm(string input) {
			if (IsDesignMode)
				return;
			if (TheConsole.IsDisposed)
				return;
			if (_consoleInputWriter == null) {
				throw new InvalidOperationException("There is no writer attached to a pseudoconsole. Have you called Start on this instance yet?");
			}
			Debug.WriteLine("writing to terminal: " + input);
			_consoleInputWriter.Write(input);
		}

		/// <summary>
		/// Get an AutoResetEvent that signals when the process exits
		/// </summary>
		private static AutoResetEvent WaitForExit(Process process) =>
			new AutoResetEvent(false) {
				SafeWaitHandle = new SafeWaitHandle(process.ProcessInfo.hProcess, ownsHandle: false)
			};

		/// <summary>
		/// Set a callback for when the terminal is closed (e.g. via the "X" window decoration button).
		/// Intended for resource cleanup logic.
		/// </summary>
		private static void OnClose(Action handler) {

			PInvoke.SetConsoleCtrlHandler(eventType => {
				if (eventType == PInvoke.CTRL_CLOSE_EVENT) {
					handler();
				}
				return false;
			}, true);
		}

		private void DisposeResources(params IDisposable[] disposables) {
			foreach (var disposable in disposables) {
				disposable.Dispose();
			}
		}

		public void Start() {
			if (IsDesignMode) {
				TerminalOutput?.Invoke(this,new ("MyShell DesignMode:> Your command window content here\r\n"));
				return;
			}
			Task.Run(() => ReadOutputLoop());
		}
		const int READ_BUFFER_SIZE = 1;
		public void WinDirectInputMode(bool enable) {

			var decSet = enable ? "h" : "l";
			var str = $"\x1b[?9001{decSet}";
			TerminalOutput?.Invoke(this, new TerminalOutputEventArgs(str));
			//var cur = TheConsole.GetInputMode();
			//if (enable)
			//    cur |= CONSOLE_MODE.ENABLE_VIRTUAL_TERMINAL_INPUT;
			//else
			//    cur &= ~CONSOLE_MODE.ENABLE_VIRTUAL_TERMINAL_INPUT;
			//TheConsole.SetInputMode(cur);
			WriteToTerm(str);
		}
		private void ReadOutputLoop() {
			using (StreamReader reader = new StreamReader(ConsoleOutStream)) {
				// Read the console's output 1 character at a time
				int bytesRead;
				var buf = new char[READ_BUFFER_SIZE];
				while ((bytesRead = reader.Read(buf, 0, READ_BUFFER_SIZE)) != 0) {
					var str = new string(buf, 0, bytesRead);
					TerminalOutput?.Invoke(this, new TerminalOutputEventArgs(str));
					if (ConsoleOutputLog != null)
						ConsoleOutputLog.Append(str);


				}
			}
		}

		void ITerminalConnection.WriteInput(string data) => WriteToTerm(data);

		public void Resize(uint rows, uint columns) {
			TheConsole?.Resize((int)columns, (int)rows);
		}

		void ITerminalConnection.Close() {
			TheConsole?.Dispose();
		}
	}
}

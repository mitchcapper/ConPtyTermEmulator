using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Terminal.Wpf;

namespace ConPtyTermEmulatorLib {
	public class BasicTerminalControl : UserControl {
		public BasicTerminalControl() {
			InitializeComponent();
			SetKBCaptureOptions();
		}
		[Flags]
		[System.ComponentModel.TypeConverter(typeof(System.ComponentModel.EnumConverter))]
		public enum INPUT_CAPTURE { None = 1 << 0, TabKey = 1 << 1, DirectionKeys = 1 << 2 };



		private static void InputCaptureChanged(DependencyObject target, DependencyPropertyChangedEventArgs e) {
			var cntrl = target as BasicTerminalControl;
			cntrl.SetKBCaptureOptions();
		}
		private void SetKBCaptureOptions() {
			KeyboardNavigation.SetTabNavigation(this, InputCapture.HasFlag(INPUT_CAPTURE.TabKey) ? KeyboardNavigationMode.Contained : KeyboardNavigationMode.Continue);
			KeyboardNavigation.SetDirectionalNavigation(this, InputCapture.HasFlag(INPUT_CAPTURE.DirectionKeys) ? KeyboardNavigationMode.Contained : KeyboardNavigationMode.Continue);
		}
		/// <summary>
		/// Helper property for setting KeyboardNavigation.Set*Navigation commands to prevent arrow keys or tabs from causing us to leave the control (aka pass through to conpty)
		/// </summary>
		public INPUT_CAPTURE InputCapture {
			get => (INPUT_CAPTURE)GetValue(InputCaptureProperty);
			set => SetValue(InputCaptureProperty, value);
		}
		public TerminalTheme? Theme { set; get; }


		public TerminalControl Terminal {
			get => (TerminalControl)GetValue(TerminalPropertyKey.DependencyProperty);
			set => SetValue(TerminalPropertyKey, value);
		}

		private static void OnTermChanged(DependencyObject target, DependencyPropertyChangedEventArgs e) {
			var cntrl = (target as BasicTerminalControl);
			var newTerm = e.NewValue as Term;
			if (newTerm != null) {
				if (cntrl.Terminal.IsLoaded)
					cntrl.Terminal_Loaded(cntrl.Terminal, null);

				if (newTerm.TermProcIsStarted)
					cntrl.Term_TermReady(newTerm, null);
				else
					newTerm.TermReady += cntrl.Term_TermReady;
			}
		}
		/// <summary>
		/// Update the Term if you want to set to an existing
		/// </summary>
		public Term ConPTYTerm {
			get => (Term)GetValue(ConPTYTermProperty);
			set => SetValue(ConPTYTermProperty, value);
		}


		public Term DisconnectConPTYTerm() {
			if (Terminal != null)
				Terminal.Connection = null;
			if (ConPTYTerm != null)
				ConPTYTerm.TermReady -= Term_TermReady;
			var ret = ConPTYTerm;
			ConPTYTerm = null;
			return ret;
		}

		public string StartupCommandLine {
			get => (string)GetValue(StartupCommandLineProperty);
			set => SetValue(StartupCommandLineProperty, value);
		}

		public bool LogConPTYOutput {
			get => (bool)GetValue(LogConPTYOutputProperty);
			set => SetValue(LogConPTYOutputProperty, value);
		}
		/// <summary>
		/// Sets if the GUI Terminal control communicates to ConPTY using extended key events (handles certain control sequences better)
		/// https://github.com/microsoft/terminal/blob/main/doc/specs/%234999%20-%20Improved%20keyboard%20handling%20in%20Conpty.md
		/// </summary>
		public bool Win32InputMode {
			get => (bool)GetValue(Win32InputModeProperty);
			set => SetValue(Win32InputModeProperty, value);
		}



		public FontFamily FontFamilyWhenSettingTheme {
			get => (FontFamily)GetValue(FontFamilyWhenSettingThemeProperty);
			set => SetValue(FontFamilyWhenSettingThemeProperty, value);
		}

		public int FontSizeWhenSettingTheme {
			get => (int)GetValue(FontSizeWhenSettingThemeProperty);
			set => SetValue(FontSizeWhenSettingThemeProperty, value);
		}
		private void InitializeComponent() {
			Terminal = new();
			ConPTYTerm = new();
			Focusable = true;
			Terminal.Focusable = true;
			Terminal.AutoResize = true;
			Terminal.Loaded += Terminal_Loaded;
			var grid = new Grid() { };
			grid.Children.Add(Terminal);
			this.Content = grid;
			this.GotFocus += (_, _) => Terminal.Focus();
		}


		private void Term_TermReady(object sender, EventArgs e) {
			this.Dispatcher.Invoke(() => {
				Terminal.Connection = ConPTYTerm;
				ConPTYTerm.Win32DirectInputMode(Win32InputMode);
				ConPTYTerm.Resize(Terminal.Columns, Terminal.Rows);//fix the size being partially off on first load
			});
		}
		private void StartTerm(int column_width, int row_height) {
			if (ConPTYTerm == null)
				return;

			if (ConPTYTerm.TermProcIsStarted) {
				ConPTYTerm.Resize(column_width, row_height);
				Term_TermReady(ConPTYTerm, null);
				return;
			}
			ConPTYTerm.TermReady += Term_TermReady;
			this.Dispatcher.Invoke(() => {
				var cmd = StartupCommandLine;//thread safety for dp
				var term = ConPTYTerm;
				var logOutput = LogConPTYOutput;
				Task.Run(() => term.Start(cmd, column_width, row_height, logOutput));
			});
		}
		private void Terminal_Loaded(object sender, RoutedEventArgs e) {
			StartTerm(Terminal.Columns, Terminal.Rows);
			if (Theme != null)
				Terminal.SetTheme(Theme.Value, FontFamilyWhenSettingTheme.Source, (short)FontSizeWhenSettingTheme);
			Terminal.Focus();
		}

		#region Depdendency Properties
		public static readonly DependencyProperty InputCaptureProperty = DependencyProperty.Register(nameof(InputCapture), typeof(INPUT_CAPTURE), typeof(BasicTerminalControl), new
		PropertyMetadata(INPUT_CAPTURE.TabKey | INPUT_CAPTURE.DirectionKeys, InputCaptureChanged));
		public static readonly DependencyProperty ThemeProperty = DependencyProperty.Register(nameof(Theme), typeof(TerminalTheme?), typeof(BasicTerminalControl), new FrameworkPropertyMetadata(null, CoerceTheme));
		private static object CoerceTheme(DependencyObject target, object value) {//prevent users from thinking they can read the theme
			(target as BasicTerminalControl).Theme = value as TerminalTheme?;
			return default;
		}
		protected static readonly DependencyPropertyKey TerminalPropertyKey = DependencyProperty.RegisterReadOnly(nameof(Terminal), typeof(TerminalControl), typeof(BasicTerminalControl), new PropertyMetadata());

		public static readonly DependencyProperty TerminalProperty = TerminalPropertyKey.DependencyProperty;
		public static readonly DependencyProperty ConPTYTermProperty = DependencyProperty.Register(nameof(ConPTYTerm), typeof(Term), typeof(BasicTerminalControl), new(OnTermChanged));
		public static readonly DependencyProperty StartupCommandLineProperty = DependencyProperty.Register(nameof(StartupCommandLine), typeof(string), typeof(BasicTerminalControl), new PropertyMetadata("powershell.exe"));

		public static readonly DependencyProperty LogConPTYOutputProperty = DependencyProperty.Register(nameof(LogConPTYOutput), typeof(bool), typeof(BasicTerminalControl), new PropertyMetadata(false));
		public static readonly DependencyProperty Win32InputModeProperty = DependencyProperty.Register(nameof(Win32InputMode), typeof(bool), typeof(BasicTerminalControl), new PropertyMetadata(true));
		

		public static readonly DependencyProperty FontFamilyWhenSettingThemeProperty = DependencyProperty.Register(nameof(FontFamilyWhenSettingTheme), typeof(FontFamily), typeof(BasicTerminalControl), new PropertyMetadata(new FontFamily("Cascadia Code")));

		public static readonly DependencyProperty FontSizeWhenSettingThemeProperty = DependencyProperty.Register(nameof(FontSizeWhenSettingTheme), typeof(int), typeof(BasicTerminalControl), new PropertyMetadata(12));

		#endregion
	}
}

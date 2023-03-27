using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.Terminal.Wpf;

namespace ConPtyTermEmulatorLib {
	public class BasicTerminalControl : UserControl {
		public BasicTerminalControl() {
			InitializeComponent();
		}

		public TerminalTheme? Theme { set; get; }
		public static readonly DependencyProperty ThemeProperty = DependencyProperty.Register(nameof(Theme), typeof(TerminalTheme?), typeof(BasicTerminalControl), new FrameworkPropertyMetadata(null, CoerceTheme));

		private static object CoerceTheme(DependencyObject target, object value) {
			(target as BasicTerminalControl).Theme = value as TerminalTheme?;
			return default;
		}
		protected static readonly DependencyPropertyKey TerminalPropertyKey =
		 DependencyProperty.RegisterReadOnly(nameof(Terminal), typeof(TerminalControl), typeof(BasicTerminalControl),new PropertyMetadata());

		public static readonly DependencyProperty TerminalProperty = TerminalPropertyKey.DependencyProperty;

		public TerminalControl Terminal {
			get => (TerminalControl)GetValue(TerminalPropertyKey.DependencyProperty);
			set => SetValue(TerminalPropertyKey, value);
		}
		public static readonly DependencyPropertyKey ConPTYTermProperty =
		 DependencyProperty.RegisterReadOnly(nameof(ConPTYTerm), typeof(Term), typeof(BasicTerminalControl),new PropertyMetadata());

		public Term ConPTYTerm {
			get => (Term)GetValue(ConPTYTermProperty.DependencyProperty);
			private set => SetValue(ConPTYTermProperty, value);
		}
		public static readonly DependencyProperty StartupCommandLineProperty =
		 DependencyProperty.Register(nameof(StartupCommandLine), typeof(string), typeof(BasicTerminalControl), new
			PropertyMetadata("powershell.exe"));

		public string StartupCommandLine {
			get => (string)GetValue(StartupCommandLineProperty);
			set => SetValue(StartupCommandLineProperty, value);
		}
		public static readonly DependencyProperty LogConPTYOutputProperty =
		 DependencyProperty.Register(nameof(LogConPTYOutput), typeof(bool), typeof(BasicTerminalControl), new
			PropertyMetadata(false));

		public bool LogConPTYOutput {
			get => (bool)GetValue(LogConPTYOutputProperty);
			set => SetValue(LogConPTYOutputProperty, value);
		}

		public static readonly DependencyProperty FontFamilyWhenSettingThemeProperty =
		 DependencyProperty.Register(nameof(FontFamilyWhenSettingTheme), typeof(FontFamily), typeof(BasicTerminalControl), new
			PropertyMetadata(new FontFamily("Cascadia Code")));

		public FontFamily FontFamilyWhenSettingTheme {
			get => (FontFamily)GetValue(FontFamilyWhenSettingThemeProperty);
			set => SetValue(FontFamilyWhenSettingThemeProperty, value); 
		}
		public static readonly DependencyProperty FontSizeWhenSettingThemeProperty =
		 DependencyProperty.Register(nameof(FontSizeWhenSettingTheme), typeof(int), typeof(BasicTerminalControl), new
			PropertyMetadata(12));

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
			this.GotFocus += (_,_) => Terminal.Focus();
			//this.GotKeyboardFocus += (_,_) => Terminal.Focus();
		}



		private void Term_TermReady(object sender, EventArgs e) {
			//term.WinDirectInputMode(true);
			this.Dispatcher.Invoke(() => {
				Terminal.Connection = ConPTYTerm;
			});
		}
		private void StartTerm(int width, int height) {
			ConPTYTerm.TermReady += Term_TermReady;
			this.Dispatcher.Invoke(() => {
				var cmd = StartupCommandLine;//thread safety for dp
				var term = ConPTYTerm;
				var logOutput = LogConPTYOutput;
				Task.Run(() => term.Start(cmd, width, height,logOutput));
			});
		}
		private void Terminal_Loaded(object sender, RoutedEventArgs e) {
			StartTerm(Terminal.Columns, Terminal.Rows);
			if (Theme != null)
				Terminal.SetTheme(Theme.Value, FontFamilyWhenSettingTheme.Source, (short)FontSizeWhenSettingTheme);
			Terminal.Focus();
		}
	}
}

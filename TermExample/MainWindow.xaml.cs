using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Terminal.Wpf;

namespace TermExample {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public MainWindow() {
			DataContext = new DataBinds();
			InitializeComponent();
		}
		public class DataBinds {
			public string StartupCommand => "pwsh.exe";
			private static uint ColorToVal(Color color) => BitConverter.ToUInt32(new byte[] { color.R, color.G, color.B, color.A }, 0);
			private static readonly Color BackroundColor = Colors.DarkBlue;
			public SolidColorBrush BackroundColorBrush => new(BackroundColor);
			public TerminalTheme Theme { get; set; } = new() {
				DefaultBackground = ColorToVal(BackroundColor),
				DefaultForeground = ColorToVal(Colors.LightYellow),
				DefaultSelectionBackground = 0xcccccc,
				SelectionBackgroundAlpha = 0.5f,
				CursorStyle = CursorStyle.BlinkingBar,
				ColorTable = new uint[] { 0x0C0C0C, 0x1F0FC5, 0x0EA113, 0x009CC1, 0xDA3700, 0x981788, 0xDD963A, 0xCCCCCC, 0x767676, 0x5648E7, 0x0CC616, 0xA5F1F9, 0xFF783B, 0x9E00B4, 0xD6D661, 0xF2F2F2 },
			};

		}
		private async void RefocusKB() {
			await Task.Delay(50);
			basicTermControl.Focus();
			Keyboard.Focus(basicTermControl);
		}
		private void ShowBufferClicked(object sender, RoutedEventArgs e) {
			var msg = basicTermControl.ConPTYTerm.GetConsoleText();
			MessageBox.Show(msg);
			RefocusKB();

		}

		private void ClearBufferClicked(object sender, RoutedEventArgs e) {
			basicTermControl.ConPTYTerm.WriteToTerm("clear\r");
			basicTermControl.ConPTYTerm.ConsoleOutputLog.Clear();
			RefocusKB();

		}
	}
}

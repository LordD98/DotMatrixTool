using System;
using System.Collections.Generic;
using System.Linq;
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

namespace DotMatrixTool
{
	/// <summary>
	/// Interaktionslogik für TestControl.xaml
	/// </summary>
	public partial class TestControl : UserControl
	{
		public TestControl()
		{
			InitializeComponent();
		}
		
		private void TestControl_DoubleClick(object sender, MouseButtonEventArgs e)
		{
			(sender as TextBox).Focusable = true;
			(sender as TextBox).IsReadOnly = false;
			(sender as TextBox).CaretBrush = Brushes.Black;
			(sender as TextBox).Cursor = Cursors.IBeam;
			e.Handled = true;
			(sender as TextBox).Focus();
			(sender as TextBox).SelectAll();
		}

		private void TestControl_LostFocus(object sender, RoutedEventArgs e)
		{
			(sender as TextBox).Focusable = false;
			(sender as TextBox).IsReadOnly = true;
			(sender as TextBox).CaretBrush = Brushes.Transparent;
			(sender as TextBox).Cursor = Cursors.Arrow;
		}

		private void TestControl_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.Key != Key.Return)
			{
				return;
			}
			(sender as TextBox).Focusable = false;
			(sender as TextBox).IsReadOnly = true;
			(sender as TextBox).CaretBrush = Brushes.Transparent;
			(sender as TextBox).Cursor = Cursors.Arrow;
		}
	}
}


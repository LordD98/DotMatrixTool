using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Collections;

namespace DotMatrixTool
{
	/// <summary>
	/// Interaktionslogik für EditableListBox.xaml
	/// </summary>
	public partial class EditableListBox : UserControl
	{
		public EditableListBox()
		{
			InitializeComponent();
		}

		private void EditableListBoxItem_DoubleClick(object sender, MouseButtonEventArgs e)
		{
			(sender as TextBox).Focusable = true;
			(sender as TextBox).IsReadOnly = false;
			(sender as TextBox).CaretBrush = Brushes.Black;
			(sender as TextBox).Cursor = Cursors.IBeam;
			e.Handled = true;
			(sender as TextBox).Focus();
			(sender as TextBox).SelectAll();
		}

		private void EditableListBoxItem_LostFocus(object sender, RoutedEventArgs e)
		{
			(sender as TextBox).Focusable = false;
			(sender as TextBox).IsReadOnly = true;
			(sender as TextBox).CaretBrush = Brushes.Transparent;
			(sender as TextBox).Cursor = Cursors.Arrow;
		}

		private void EditableListBoxItem_KeyDown(object sender, KeyEventArgs e)
		{
			switch(e.Key)
			{
				case Key.Return:
				{
					(sender as TextBox).Focusable = false;
					(sender as TextBox).IsReadOnly = true;
					(sender as TextBox).CaretBrush = Brushes.Transparent;
					(sender as TextBox).Cursor = Cursors.Arrow;
					break;
				}
			}
		}

		private void EditableListBox_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.Key == Key.System)
			{
				switch(e.SystemKey)
				{
					case Key.Up:
					{
						if(Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
						{
							if(lbxMain.ItemsSource is ObservableCollection<DotMatrixSetting>)
							{
								ObservableCollection<DotMatrixSetting> settings = lbxMain.ItemsSource as ObservableCollection<DotMatrixSetting>;
								int oldIndex = lbxMain.SelectedIndex;
								if(oldIndex > 0)
								{
									DotMatrixSetting temp = lbxMain.SelectedItem as DotMatrixSetting;
									settings[oldIndex] = settings[oldIndex - 1];
									settings[oldIndex - 1] = temp;
									lbxMain.SelectedIndex = oldIndex - 1;
								}
							}
						}
						break;
					}
					case Key.Down:
					{
						if(Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
						{
							if(lbxMain.ItemsSource is ObservableCollection<DotMatrixSetting>)
							{
								ObservableCollection<DotMatrixSetting> settings = lbxMain.ItemsSource as ObservableCollection<DotMatrixSetting>;
								int oldIndex = lbxMain.SelectedIndex;
								if(oldIndex < settings.Count-1)
								{
									DotMatrixSetting temp = lbxMain.SelectedItem as DotMatrixSetting;
									settings[oldIndex] = settings[oldIndex+1];
									settings[oldIndex+1] = temp;
									lbxMain.SelectedIndex = oldIndex+1;
								}
							}
						}
						break;
					}
				}
			}
		}
	}
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace DotMatrixTool.Commands
{
    public static class SettingCommands
    {
		private static Window context;
		private static ListBox listBox;

        public static void BindCommandsToWindow(Window window, UserControl control)
        {
			context = window;
			listBox = control.Content as ListBox;
            window.CommandBindings.Add(new CommandBinding(Save, Save_Executed, Item_Selected));
            window.CommandBindings.Add(new CommandBinding(Load, Load_Executed, Item_Selected));
            window.CommandBindings.Add(new CommandBinding(New, New_Executed));
            window.CommandBindings.Add(new CommandBinding(Delete, Delete_Executed, Item_Selected));
			window.CommandBindings.Add(new CommandBinding(ClearAll, ClearAll_Executed, Item_Present));
			window.CommandBindings.Add(new CommandBinding(Export, Export_Executed, Item_Present));
			window.CommandBindings.Add(new CommandBinding(Import, Import_Executed));
			window.CommandBindings.Add(new CommandBinding(ExportAs, ExportAs_Executed, Item_Present));
			window.CommandBindings.Add(new CommandBinding(ImportFrom, ImportFrom_Executed));
		}

		private static void Item_Selected(object sender, CanExecuteRoutedEventArgs e)
        {
            MainWindow mainWindow = context as MainWindow;
            if (mainWindow != null)
            {
                e.CanExecute = (listBox.SelectedIndex != -1);
                return;
            }
            e.CanExecute = false;
        }

        private static void Item_Present(object sender, CanExecuteRoutedEventArgs e)
		{
			if(listBox != null)
			{
				e.CanExecute = (listBox.ItemsSource as ObservableCollection<DotMatrixSetting>).Count != -1;
				return;
			}
			e.CanExecute = false;
		}


        private static void New_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MainWindow mainWindow = context as MainWindow;
            if (mainWindow != null && listBox != null)
            {
                ObservableCollection<DotMatrixSetting> settingsList = listBox.ItemsSource as ObservableCollection<DotMatrixSetting>;
                settingsList.Add(new DotMatrixSetting
                (
                    $"Matrix #{listBox.Items.Count+1}",
                    mainWindow.width,
                    mainWindow.height
                ));
                listBox.SelectedIndex = listBox.Items.Count - 1;
            }
        }

        private static void Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (listBox != null)
            {
                if (listBox.SelectedIndex != -1)
                {
                    int oldIndex = listBox.SelectedIndex;
                    (listBox.ItemsSource as ObservableCollection<DotMatrixSetting>).Remove(listBox.SelectedItem as DotMatrixSetting);
                    if (!listBox.Items.IsEmpty)
                    {
                        if (listBox.Items.Count == oldIndex)
                        {
                            listBox.SelectedIndex = listBox.Items.Count - 1;
                        }
                        else
                        {
                            listBox.SelectedIndex = oldIndex;
                        }
                    }
                }
            }
        }

        private static void Load_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (context as MainWindow != null && listBox != null)
            {
                if (listBox.SelectedIndex != -1)
                {
                    DotMatrixSetting setting = listBox.SelectedItem as DotMatrixSetting;
                    (context as MainWindow).LoadDotMatrixFromSetting(setting);
                }
            }
        }

        private static void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (context as MainWindow != null && listBox != null)
            {
                if (listBox.SelectedIndex != -1)
                {
                    DotMatrixSetting setting = listBox.SelectedItem as DotMatrixSetting;
                    (context as MainWindow).SaveDotMatrixToSetting(setting);
                }
            }
        }

		private static void ClearAll_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			MessageBoxResult result = MessageBox.Show
			(
				"Alle gespeicherten Matrizen löschen?",
				"Alles löschen?",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning,
				MessageBoxResult.No
			);
			if(result == MessageBoxResult.Yes)
			{
				MainWindow mainWindow = context as MainWindow;
				if(mainWindow != null && listBox != null)
				{
					(listBox.ItemsSource as ObservableCollection<DotMatrixSetting>).Clear();
					mainWindow.BtnClear_Click(null, null);
				}
			}
		}

		private static void Export_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			MainWindow mainWindow = context as MainWindow;
			string oldText = mainWindow.tbxCode.Text;
			MainWindow.ConversionType oldConversionType = mainWindow.conversionType;
			mainWindow.conversionType = MainWindow.ConversionType.SettingsOut;
			mainWindow.Convert(null, null);
			string settingsOut = mainWindow.tbxCode.Text;
			mainWindow.tbxCode.Text = oldText;
			mainWindow.conversionType = oldConversionType;
			File.WriteAllText("Settings.txt", settingsOut);
			mainWindow.Title = "DotMatrixTool - Settings.txt";
		}

		private static void Import_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			MainWindow mainWindow = context as MainWindow;
			string oldText = mainWindow.tbxCode.Text;
			MainWindow.ConversionType oldConversionType = mainWindow.conversionType;
			mainWindow.conversionType = MainWindow.ConversionType.SettingsIn;
			if(!File.Exists("Settings.txt"))
			{
				return;
			}
			string settingsIn = File.ReadAllText("Settings.txt");
			mainWindow.tbxCode.Text = settingsIn;
			mainWindow.Convert(null, null);
			mainWindow.tbxCode.Text = oldText;
			mainWindow.conversionType = oldConversionType;
			mainWindow.Title = "DotMatrixTool - Settings.txt";
		}

		private static void ExportAs_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
			saveFileDialog.Filter = "All files (*.*) | *.*";
			saveFileDialog.FileName = "Settings.txt";
			saveFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
			if(saveFileDialog.ShowDialog(context) == true)
			{
				MainWindow mainWindow = context as MainWindow;
				string oldText = mainWindow.tbxCode.Text;
				MainWindow.ConversionType oldConversionType = mainWindow.conversionType;
				mainWindow.conversionType = MainWindow.ConversionType.SettingsOut;
				mainWindow.Convert(null, null);
				string settingsOut = mainWindow.tbxCode.Text;
				mainWindow.tbxCode.Text = oldText;
				mainWindow.conversionType = oldConversionType;
				File.WriteAllText(saveFileDialog.FileName, settingsOut);
				string settingName = saveFileDialog.FileName.Replace(Directory.GetCurrentDirectory() + "\\", "");
				mainWindow.Title = $"DotMatrixTool - {settingName}";
			}
		}

		private static void ImportFrom_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
			openFileDialog.Filter = "All files (*.*) | *.*";
			openFileDialog.FileName = "Settings.txt";
			openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
			if(openFileDialog.ShowDialog(context) == true)
			{
				MainWindow mainWindow = context as MainWindow;
				string oldText = mainWindow.tbxCode.Text;
				MainWindow.ConversionType oldConversionType = mainWindow.conversionType;
				mainWindow.conversionType = MainWindow.ConversionType.SettingsIn;
				string settingsIn = File.ReadAllText(openFileDialog.FileName);
				mainWindow.tbxCode.Text = settingsIn;
				mainWindow.Convert(null, null);
				mainWindow.tbxCode.Text = oldText;
				mainWindow.conversionType = oldConversionType;
				string settingName = openFileDialog.FileName.Replace(Directory.GetCurrentDirectory() + "\\", "");
				mainWindow.Title = $"DotMatrixTool - {settingName}";
			}
		}


		public static readonly RoutedUICommand New = new RoutedUICommand
            (
                "New",
                "New",
                typeof(SettingCommands),
                new InputGestureCollection()
                {
                    new KeyGesture(Key.N, ModifierKeys.Alt)
                }
            );

        public static readonly RoutedUICommand Delete = new RoutedUICommand
            (
                "Delete",
                "Delete",
                typeof(SettingCommands),
                new InputGestureCollection()
                {
                    new KeyGesture(Key.Delete, ModifierKeys.Alt)
                }
            );

        public static readonly RoutedUICommand Save = new RoutedUICommand
            (
                "Save",
                "Save",
                typeof(SettingCommands),
                new InputGestureCollection()
                {
                    new KeyGesture(Key.S, ModifierKeys.Alt)
                }
            );

        public static readonly RoutedUICommand Load = new RoutedUICommand
            (
                "Load",
                "Load",
                typeof(SettingCommands),
                new InputGestureCollection()
                {
                    new KeyGesture(Key.L, ModifierKeys.Alt)
                }
            );

		public static readonly RoutedUICommand ClearAll = new RoutedUICommand
			(
				"Clear All",
				"ClearAll",
				typeof(SettingCommands),
				new InputGestureCollection()
				{
					new KeyGesture(Key.X, ModifierKeys.Alt)
				}
			);

		public static readonly RoutedUICommand Export = new RoutedUICommand
			(
				"Export",
				"Export",
				typeof(SettingCommands),
				new InputGestureCollection()
				{
					new KeyGesture(Key.S, ModifierKeys.Control)
				}
			);

		public static readonly RoutedUICommand Import = new RoutedUICommand
			(
				"Import",
				"Import",
				typeof(SettingCommands),
				new InputGestureCollection()
				{
					new KeyGesture(Key.O, ModifierKeys.Control)
				}
			);

		public static readonly RoutedUICommand ExportAs = new RoutedUICommand
			(
				"Export As",
				"ExportAs",
				typeof(SettingCommands)
			);

		public static readonly RoutedUICommand ImportFrom = new RoutedUICommand
			(
				"Import From",
				"ImportFrom",
				typeof(SettingCommands)
			);
	}
}

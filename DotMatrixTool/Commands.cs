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
using DotMatrixTool;

namespace DotMatrixTool.Commands
{
    public static class SettingCommands
    {
        public static Window Context { get; set; }

        public static void BindCommandsToWindow(Window window)
        {
            window.CommandBindings.Add(new CommandBinding(Save, Save_Executed, Item_Selected));
            window.CommandBindings.Add(new CommandBinding(Load, Load_Executed, Item_Selected));
            window.CommandBindings.Add(new CommandBinding(New, New_Executed));
            window.CommandBindings.Add(new CommandBinding(Delete, Delete_Executed, Item_Selected));
			window.CommandBindings.Add(new CommandBinding(ClearAll, ClearAll_Executed, Item_Present));
		}

        private static void Item_Selected(object sender, CanExecuteRoutedEventArgs e)
        {
            MainWindow mainWindow = Context as MainWindow;
            if (mainWindow != null)
            {
                e.CanExecute = (mainWindow.lbxSavedPatterns.SelectedIndex != -1);
                return;
            }
            e.CanExecute = false;
        }

        private static void Item_Present(object sender, CanExecuteRoutedEventArgs e)
		{
			MainWindow mainWindow = Context as MainWindow;
			if(mainWindow != null)
			{
				e.CanExecute = (mainWindow.lbxSavedPatterns.ItemsSource as ObservableCollection<DotMatrixSetting>).Count != -1;
				return;
			}
			e.CanExecute = false;
		}


        private static void New_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MainWindow mainWindow = Context as MainWindow;
            if (mainWindow != null)
            {
                ObservableCollection<DotMatrixSetting> settingsList = mainWindow.lbxSavedPatterns.ItemsSource as ObservableCollection<DotMatrixSetting>;
                settingsList.Add(new DotMatrixSetting
                    (
                        $"Matrix #{mainWindow.lbxSavedPatterns.Items.Count+1}",
                        mainWindow.width,
                        mainWindow.height
                    ));
                mainWindow.lbxSavedPatterns.SelectedIndex = mainWindow.lbxSavedPatterns.Items.Count - 1;
            }
        }

        private static void Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MainWindow mainWindow = Context as MainWindow;
            if (mainWindow != null)
            {
                ListBox lbx = mainWindow.lbxSavedPatterns;
                if (lbx.SelectedIndex != -1)
                {
                    int oldIndex = lbx.SelectedIndex;
                    (lbx.ItemsSource as ObservableCollection<DotMatrixSetting>).Remove(lbx.SelectedItem as DotMatrixSetting);
                    if (!lbx.Items.IsEmpty)
                    {
                        if (lbx.Items.Count == oldIndex)
                        {
                            lbx.SelectedIndex = lbx.Items.Count - 1;
                        }
                        else
                        {
                            lbx.SelectedIndex = oldIndex;
                        }
                    }
                }
            }
        }

        private static void Load_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (Context as MainWindow != null)
            {
                if ((Context as MainWindow).lbxSavedPatterns.SelectedIndex != -1)
                {
                    DotMatrixSetting setting = ((Context as MainWindow).lbxSavedPatterns.SelectedItem as DotMatrixSetting);
                    (Context as MainWindow).LoadDotMatrixFromSetting(setting);
                }
            }
        }

        private static void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (Context as MainWindow != null)
            {
                if ((Context as MainWindow).lbxSavedPatterns.SelectedIndex != -1)
                {
                    DotMatrixSetting setting = ((Context as MainWindow).lbxSavedPatterns.SelectedItem as DotMatrixSetting);
                    (Context as MainWindow).SaveDotMatrixToSetting(setting);
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
				MainWindow mainWindow = Context as MainWindow;
				if(mainWindow != null)
				{
					(mainWindow.lbxSavedPatterns.ItemsSource as ObservableCollection<DotMatrixSetting>).Clear();
					mainWindow.BtnClear_Click(null, null);
				}
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
	}
}

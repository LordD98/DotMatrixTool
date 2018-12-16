using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using DotMatrixTool.Commands;

namespace DotMatrixTool
{
	public partial class MainWindow
	{

		public enum ConversionType
		{
			MatrixIn,
			MatrixOut,
			CodesIn,
			CodesOut,
			SettingsIn,
			SettingsOut
		}
		public ConversionType conversionType = ConversionType.CodesIn;

		enum ClickType
		{
			None,
			Red,
			White,
		}
		ClickType currentClick = ClickType.None;

		struct ClickPosition
		{
			public int I;
			public int J;
			public bool Valid;
			public MainWindow.ClickType Type;
		}
		ClickPosition lastClick;

		public int width = 7;
		public int height = 5;
		public bool useSmallLeds = false;
		public bool updateDimensions = true;
		public bool loadImmediately = true;
		public bool noImport = false;
		public bool flipDimensions = false;
		public bool trim = false;
		public bool overwriteSettings = true;
		public bool exportDimensions = true;

		const int canvasSideMargin = 14;
		const double u = 11.13;
		int cellSize;
		int diameter;
		List<List<Ellipse>> buttons;
		List<List<bool>> dotMatrix;
		ObservableCollection<DotMatrixSetting> listBoxItems;

		public MainWindow()
		{
			cellSize = useSmallLeds ? 22 : 33;
			diameter = useSmallLeds ? 11 : 22;

			buttons = new List<List<Ellipse>>(height);
			dotMatrix = new List<List<bool>>(height);


			InitializeList(buttons, width, height);
			InitializeList(dotMatrix, width, height);
			InitializeComponent();
			//SplitterWidthConverter.window = this;

			listBoxItems = new ObservableCollection<DotMatrixSetting>();
			(testControl.Content as ListBox).ItemsSource = listBoxItems;

			Loaded += new RoutedEventHandler(MainWindow_Loaded);
			Loaded += (x, y) => Keyboard.Focus(canvas);
		}

		private void RedrawGrid()
		{
			AssertCollectionSize(dotMatrix, width, height);
			AssertCollectionSize(buttons, width, height);
			int actualWidth = (int)canvas.ActualWidth;
			int actualHeight = (int)canvas.ActualHeight;
			int maxNx = (actualWidth - canvasSideMargin) / cellSize;
			int maxNy = (actualHeight - canvasSideMargin) / cellSize;
			maxNx = maxNx < width ? maxNx : width;
			maxNy = maxNy < height ? maxNy : height;
			for(int i = 0; i < buttons.Count; i++)
			{
				for(int j = 0; j < buttons[i].Count; j++)
				{
					if(!canvas.Children.Contains(buttons[i][j]))
					{
						buttons[i][j].Height = diameter;
						buttons[i][j].Width = diameter;
						Canvas.SetLeft(buttons[i][j], (j * cellSize + canvasSideMargin));
						Canvas.SetTop(buttons[i][j], (i * cellSize + canvasSideMargin));
						canvas.Children.Add(buttons[i][j]);
					}
					if(i < maxNy && j < maxNx)
					{
						buttons[i][j].Fill = dotMatrix[i][j] ? Brushes.Red : Brushes.White;
						buttons[i][j].Stroke = Brushes.Red;

						buttons[i][j].MouseLeftButtonDown -= LED_MouseLeftButtonDown;
						buttons[i][j].MouseLeftButtonUp -= LED_MouseLeftButtonUp;
						buttons[i][j].MouseEnter -= LED_MouseEnter;
						buttons[i][j].MouseLeave -= LED_MouseLeave;

						buttons[i][j].MouseLeftButtonDown += LED_MouseLeftButtonDown;
						buttons[i][j].MouseLeftButtonUp += LED_MouseLeftButtonUp;
						buttons[i][j].MouseEnter += LED_MouseEnter;
						buttons[i][j].MouseLeave += LED_MouseLeave;
					}
					else
					{
						buttons[i][j].Fill = Brushes.Transparent;
						buttons[i][j].Stroke = Brushes.Transparent;
						buttons[i][j].MouseLeftButtonDown -= LED_MouseLeftButtonDown;
						buttons[i][j].MouseLeftButtonUp -= LED_MouseLeftButtonUp;
						buttons[i][j].MouseEnter -= LED_MouseEnter;
						buttons[i][j].MouseLeave -= LED_MouseLeave;
					}
				}
			}
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			SettingCommands.BindCommandsToWindow(this, testControl);
		}

		private void DrawInit(object sender, RoutedEventArgs e)
		{
			RedrawGrid();
		}

		private void SldWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			width = (int)e.NewValue;
			lblWidth.Content = $"Width: {width}";
			RedrawGrid();
		}

		private void SldHeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			height = (int)e.NewValue;
			lblHeight.Content = $"Height: {height}";
			RedrawGrid();
		}

		public void BtnClear_Click(object sender, RoutedEventArgs e)
		{
			dotMatrix = new List<List<bool>>();
			RedrawGrid();
		}

		public void Convert(object sender, RoutedEventArgs e)
		{
			try
			{
				switch(conversionType)
				{
					case ConversionType.MatrixIn:
					{
						int oldCount = listBoxItems.Count;
						string[] textLines = tbxCode.Text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
						int newHeight = -1;
						for(int row = 0; row < textLines.Length; row++)
						{
							textLines[row] = textLines[row].Replace("\r", "").Replace(" ", "");
							if(newHeight == -1 && textLines[row] == "")
							{
								newHeight = row;
							}
						}
						int numChunks = textLines.Count(x => x == "") + 1;
						int newWidth = textLines[0].Length;
						byte[][] byteData = new byte[numChunks][];
						for(int chunk = 0; chunk < numChunks; ++chunk)
						{
							byteData[chunk] = new byte[newHeight];
							for(int i = 0; i < newHeight; ++i)
							{
								byteData[chunk][i] = System.Convert.ToByte(textLines[chunk * (newHeight + 1) + i], 2);
							}
							DotMatrixSetting newSetting = new DotMatrixSetting($"#{chunk}", newWidth, newHeight);
							newSetting.SetMatrix(byteData[chunk]);
							if(!noImport)
							{
								listBoxItems.Add(newSetting);
							}
							if(trim)
							{
								newSetting.TrimMatrix();
							}
						}
						if(listBoxItems.Count > oldCount && loadImmediately)
						{
							(testControl.Content as ListBox).SelectedIndex = oldCount;
							SettingCommands.Load.Execute(null, null);
						}
						break;
					}
					case ConversionType.MatrixOut:
					{
						string code = "";
						for(int i = 0; i < this.height; i++)
						{
							for(int j = 0; j < this.width; j++)
							{
								code += (dotMatrix[i][j] ? "1" : "0") + " ";
							}
							code += "\n";
						}
						tbxCode.Text = code;
						break;
					}
					case ConversionType.CodesIn:
					{
						if(overwriteSettings)
						{
							listBoxItems.Clear();
						}
						string[] strCodes = tbxCode.Text.Replace("\r", "").Replace(" ", "").Replace("0x", "").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
						int oldNumSettings = listBoxItems.Count;
						int newNumSettings = strCodes.Length;
						DotMatrixSetting firstSetting = null;
						for(int k = 0; k < strCodes.Length; k++)
						{
							int newHeight;
							int newWidth;
							if(strCodes[k].Count(x => (x == ',')) <= 1)
							{
								newWidth = 8;
							}
							else
							{
								newWidth = strCodes[k].IndexOf(',') * 4;
							}
							strCodes[k] = strCodes[k].Replace(",", "");
							newHeight = strCodes[k].Length / (newWidth / 4);
							UInt64 mask = UInt64.MaxValue;
							mask = mask >> (64 - newWidth);

							string[] matrixLines = new string[newHeight];
							UInt64[] splitMatrix = new UInt64[newHeight];
							DotMatrixSetting newSetting = new DotMatrixSetting($"#{k + oldNumSettings}", newWidth, newHeight);
							for(int i = newHeight; i != 0; i--)
							{
								matrixLines[i - 1] = strCodes[k].Substring(strCodes[k].Length - 2);
								splitMatrix[i - 1] = System.Convert.ToUInt64(matrixLines[i - 1], 16);
								strCodes[k] = strCodes[k].Remove(strCodes[k].Length - 2);
							}
							if(flipDimensions)
							{
								for(int i = 0; i < newHeight; i++)
								{
									for(int j = 0; j < newWidth; j++)
									{
										newSetting[i, j] = (splitMatrix[newHeight - i - 1] & (1UL << (j))) != 0;
									}
								}
							}
							else
							{
								for(int i = 0; i < newHeight; i++)
								{
									for(int j = 0; j < newWidth; j++)
									{
										newSetting[i, j] = (splitMatrix[i] & (1UL << (newWidth - j - 1))) != 0;
									}
								}
							}
							if(!noImport)
							{
								listBoxItems.Add(newSetting);
							}
							if(trim)
							{
								newSetting.TrimMatrix();
							}
							if(k == 0)
							{
								firstSetting = newSetting;
							}
						}
						if(loadImmediately && firstSetting != null)
						{
							LoadDotMatrixFromSetting(firstSetting);
							if(listBoxItems.Count > oldNumSettings)
							{
								(testControl.Content as ListBox).SelectedIndex = oldNumSettings;
							}
						}
						break;
					}
					case ConversionType.CodesOut:
					{
						string code = "";
						foreach(DotMatrixSetting setting in listBoxItems)
						{
							for(int i = 0; i < setting.Height; i++)
							{
								UInt32 num = 0;
								for(int j = 0; j < setting.Width; j++)
								{
									if(setting[i, j])
									{
										num |= (1U << (byte)(setting.Width - 1 - j));
									}
								}
								code += num.ToString("X2");
								//code += num.ToString("X2") + ",";
							}
							//code = code.Remove(code.Length - 1);
							code += "\n";
						}
						tbxCode.Text = code;
						break;
					}
					case ConversionType.SettingsIn:
					{
						if(overwriteSettings)
						{
							listBoxItems.Clear();
						}
						string[] strCodes = tbxCode.Text.Replace("\r", "").Replace(" ", "").Replace("0x", "").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
						int oldNumSettings = listBoxItems.Count;
						int newNumSettings = strCodes.Length;
						DotMatrixSetting firstSetting = null;
						for(int k = 0; k < strCodes.Length; k++)
						{
							string name = strCodes[k].Substring(0, strCodes[k].LastIndexOf(','));
							strCodes[k] = strCodes[k].Substring(strCodes[k].LastIndexOf(',') + 1);
							int newHeight;
							int newWidth = System.Convert.ToInt32(strCodes[k].Substring(0, 2), 16);
							int numSymbolsPerRow = (newWidth + 3) / 4;
							strCodes[k] = strCodes[k].Substring(2);
							strCodes[k] = strCodes[k].Replace(",", "");
							newHeight = strCodes[k].Length / numSymbolsPerRow;
							UInt64 mask = UInt64.MaxValue;
							mask = mask >> (64 - newWidth);

							string[] matrixLines = new string[newHeight];
							UInt64[] splitMatrix = new UInt64[newHeight];
							DotMatrixSetting newSetting = new DotMatrixSetting(name, newWidth, newHeight);
							for(int i = newHeight; i != 0; i--)
							{
								matrixLines[i - 1] = strCodes[k].Substring(strCodes[k].Length - numSymbolsPerRow);
								splitMatrix[i - 1] = System.Convert.ToUInt64(matrixLines[i - 1], 16);
								strCodes[k] = strCodes[k].Remove(strCodes[k].Length - numSymbolsPerRow);
							}
							if(flipDimensions)
							{
								for(int i = 0; i < newHeight; i++)
								{
									for(int j = 0; j < newWidth; j++)
									{
										newSetting[i, j] = (splitMatrix[newHeight - i - 1] & (1UL << (j))) != 0;
									}
								}
							}
							else
							{
								for(int i = 0; i < newHeight; i++)
								{
									for(int j = 0; j < newWidth; j++)
									{
										newSetting[i, j] = (splitMatrix[i] & (1UL << (newWidth - j - 1))) != 0;
									}
								}
							}
							if(!noImport)
							{
								listBoxItems.Add(newSetting);
							}
							if(trim)
							{
								newSetting.TrimMatrix();
							}
							if(k == 0)
							{
								firstSetting = newSetting;
							}
						}
						if(loadImmediately && firstSetting != null)
						{
							LoadDotMatrixFromSetting(firstSetting);
							if(listBoxItems.Count > oldNumSettings)
							{
								(testControl.Content as ListBox).SelectedIndex = oldNumSettings;
							}
						}
						break;
					}
					case ConversionType.SettingsOut:
					{
						string code = "";
						foreach(DotMatrixSetting setting in listBoxItems)
						{
							code += setting.Name;
							code += ",";
							code += setting.Width.ToString("X2");
							for(int i = 0; i < setting.Height; i++)
							{
								UInt64 num = 0;
								for(int j = 0; j < setting.Width; j++)
								{
									if(setting[i, j])
									{
										num |= (1UL << (byte)(setting.Width - 1 - j));
									}
								}
								code += num.ToString($"X{(setting.Width + 3) / 4}");
								//code += num.ToString("X2") + ",";
							}
							//code = code.Remove(code.Length - 1);
							code += "\r\n";
						}
						tbxCode.Text = code;
						break;
					}
				}
			}
			catch
			{
				MessageBox.Show("Invalid Conversion", "Invalid Conversion");
			}
		}

		private (int, int) GetClickedEllipseIndex()
		{
			for(int i = 0; i < height; i++)
			{
				for(int j = 0; j < width; j++)
				{
					if(buttons[i][j].IsMouseOver)
					{
						return (i, j);
					}
				}
			}
			return (-1, -1);
		}

		private void LED_MouseEnter(object sender, MouseEventArgs e)
		{
			if(Mouse.LeftButton != MouseButtonState.Pressed)
			{
				return;
			}
			(int i, int j) = GetClickedEllipseIndex();
			if(i < 0 || j < 0)
			{
				return;
			}
			switch(currentClick)
			{
				case MainWindow.ClickType.None:
				case MainWindow.ClickType.White:
					buttons[i][j].Fill = Brushes.Red;
					break;
				case MainWindow.ClickType.Red:
					buttons[i][j].Fill = Brushes.White;
					break;
			}
		}

		private void LED_MouseLeave(object sender, MouseEventArgs e)
		{
			if(e.LeftButton != MouseButtonState.Pressed)
			{
				return;
			}
			lastClick.Valid = false;
		}

		private void LED_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			currentClick = ClickType.None;
			for(int i = 0; i < height; ++i)
			{
				for(int j = 0; j < width; ++j)
				{
					dotMatrix[i][j] = buttons[i][j].Fill == Brushes.Red;
				}
			}
			e.Handled = true;
		}

		private void LED_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Keyboard.Focus(canvas);
			(int i, int j) = GetClickedEllipseIndex();
			int di = i - lastClick.I;
			int dj = j - lastClick.J;
			if(lastClick.Valid && Keyboard.IsKeyDown(Key.LeftShift))
			{
				if(Keyboard.IsKeyDown(Key.LeftCtrl))
				{
					while(di != 0)
					{
						dj = j - lastClick.J;
						while(dj != 0)
						{
							buttons[i - di][j - dj].Fill = lastClick.Type == MainWindow.ClickType.Red ? Brushes.White : Brushes.Red;
							if(dj > 0)
								dj--;
							if(dj < 0)
								dj++;
						}
						if(di > 0)
							di--;
						if(di < 0)
							di++;
					}
					dj = j - lastClick.J;
					while(dj != 0)
					{
						buttons[i - di][j - dj].Fill = lastClick.Type == MainWindow.ClickType.Red ? Brushes.White : Brushes.Red;
						if(dj > 0)
							dj--;
						if(dj < 0)
							dj++;
					}
					di = i - lastClick.I;
					while(di != 0)
					{
						buttons[i - di][j - dj].Fill = lastClick.Type == MainWindow.ClickType.Red ? Brushes.White : Brushes.Red;
						if(di > 0)
							di--;
						if(di < 0)
							di++;
					}
					buttons[i][j].Fill = lastClick.Type == ClickType.Red ? Brushes.White : Brushes.Red;
				}
				else if(i == lastClick.I || j == lastClick.J || di == dj || di == -dj)
				{
					while(di != 0 || dj != 0)
					{
						buttons[i - di][j - dj].Fill = lastClick.Type == MainWindow.ClickType.Red ? Brushes.White : Brushes.Red;
						if(di > 0)
							di--;
						if(dj > 0)
							dj--;
						if(di < 0)
							di++;
						if(dj < 0)
							dj++;
					}
					buttons[i][j].Fill = lastClick.Type == ClickType.Red ? Brushes.White : Brushes.Red;
				}
				else
				{
					LED_MouseEnter(sender, null);
				}
			}
			else
			{
				if(i < 0 || j < 0 || buttons[i][j].Fill == Brushes.White)
				{
					currentClick = ClickType.White;
				}
				else if(buttons[i][j].Fill == Brushes.Red)
				{
					currentClick = ClickType.Red;
				}
				lastClick.Type = currentClick;
				LED_MouseEnter(sender, null);	// LED on/off
			}
			lastClick.I = i;
			lastClick.J = j;
			lastClick.Valid = true;
		}

		public void LoadDotMatrixFromSetting(DotMatrixSetting setting)
		{
			dotMatrix = new List<List<bool>>();
			setting.LoadMatrix(dotMatrix);
			if(updateDimensions)
			{
				if(sldHeight.Maximum < setting.Height)
				{
					sldHeight.Maximum = setting.Height;
				}
				if(sldWidth.Maximum < setting.Width)
				{
					sldWidth.Maximum = setting.Width;
				}
				sldHeight.Value = setting.Height;
				sldWidth.Value = setting.Width;
			}
			RedrawGrid();
		}

		public void SaveDotMatrixToSetting(DotMatrixSetting setting)
		{
			int selectedIndex = (testControl.Content as ListBox).SelectedIndex;
			if(selectedIndex == -1)
			{
				return;
			}
			setting = new DotMatrixSetting(setting.Name, width, height);
			setting.SetMatrix(dotMatrix);
			listBoxItems[selectedIndex] = setting;
			(testControl.Content as ListBox).SelectedIndex = selectedIndex;
		}
		

		private void Canvas_Resize(object sender, SizeChangedEventArgs e)
		{
			int canvasWidth;
			int canvasHeight;
			if(e == null)
			{
				canvasWidth = (int)canvas.ActualWidth;
				canvasHeight = (int)canvas.ActualHeight;
			}
			else
			{
				canvasWidth = (int)e.NewSize.Width;
				canvasHeight = (int)e.NewSize.Height;
			}
			int maxNx = (canvasWidth - canvasSideMargin) / cellSize;
			int maxNy = (canvasHeight - canvasSideMargin) / cellSize;
			//int oldSldWidthVal = (int)sldWidth.Value;
			//int oldSldHeightVal = (int)sldHeight.Value;
			sldWidth.Maximum = maxNx > sldWidth.Maximum ? maxNx : sldWidth.Maximum;
			sldHeight.Maximum = maxNy > sldHeight.Maximum ? maxNy : sldHeight.Maximum;
			RedrawGrid();
		}
		
		private void Canvas_Click(object sender, MouseButtonEventArgs e)
		{
			Keyboard.Focus(canvas);
		}

		private void Canvas_KeyDown(object sender, KeyEventArgs e)
		{
			if(Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
			{
				bool noUpdate = false;
				if(e.Key == Key.System)
				{
					switch(e.SystemKey)
					{
						case Key.Up:
						{
							ShiftUp();
							break;
						}
						case Key.Down:
						{
							ShiftDown();
							break;
						}
						case Key.Right:
						{
							ShiftRight();
							break;
						}
						case Key.Left:
						{
							ShiftLeft();
							break;
						}
						default:
						{
							noUpdate = true;
							break;
						}
					}
				}
				if(!noUpdate)
				{
					RedrawGrid();
				}
			}
		}

		private void ListBoxSavedPatterns_KeyDown(object sender, KeyEventArgs e)
		{
			ListBox listBox = sender as ListBox;
			if(e.Key != Key.Delete)
			{
				return;
			}
			SettingCommands.Delete.Execute(null, null);
		}

		
		private void MenuItemConvert_Click(object sender, RoutedEventArgs e)
		{
			ItemCollection items = ((sender as MenuItem).Parent as MenuItem).Items;
			for(int index = 0; index < items.Count; index++)
			{
				if(sender == items[index])
				{
					(items[index] as MenuItem).IsChecked = true;
					conversionType = (ConversionType)index;
				}
				else
				{
					(items[index] as MenuItem).IsChecked = false;
				}
			}
		}

		private void SmallLEDs_Clicked(object sender, RoutedEventArgs e)
		{
			useSmallLeds = (sender as MenuItem).IsChecked;
			cellSize = useSmallLeds ? 22 : 33;
			diameter = useSmallLeds ? 11 : 22;
			int oldWidthValue = (int)sldWidth.Value;
			int oldHeightValue = (int)sldHeight.Value;
			if(!useSmallLeds)
			{

			}
			//sldWidth.Value = oldWidthValue > sldWidth.Maximum ? sldWidth.Maximum : oldWidthValue;
			//sldHeight.Value = oldHeightValue > sldHeight.Maximum ? sldHeight.Maximum : oldHeightValue;
			for(int i = 0; i < buttons.Count; i++)
			{
				for(int j = 0; j < buttons[i].Count; j++)
				{
					buttons[i][j].Height = diameter;
					buttons[i][j].Width = diameter;
					Canvas.SetLeft(buttons[i][j], (j * cellSize + canvasSideMargin));
					Canvas.SetTop(buttons[i][j], (i * cellSize + canvasSideMargin));
				}
			}
			Canvas_Resize(null, null);
		}


		private void UpdateDimensions_Click(object sender, RoutedEventArgs e)
		{
			updateDimensions = (sender as MenuItem).IsChecked;
		}

		private void LoadImmediately_Click(object sender, RoutedEventArgs e)
		{
			loadImmediately = (sender as MenuItem).IsChecked;
		}

		private void NoImport_Click(object sender, RoutedEventArgs e)
		{
			noImport = (sender as MenuItem).IsChecked;
		}

		private void FlipDimensions_Click(object sender, RoutedEventArgs e)
		{
			flipDimensions = (sender as MenuItem).IsChecked;
		}

		private void Trim_Click(object sender, RoutedEventArgs e)
		{
			trim = (sender as MenuItem).IsChecked;
		}

		private void OverwriteSettings_Click(object sender, RoutedEventArgs e)
		{
			overwriteSettings = (sender as MenuItem).IsChecked;
		}

		private void ExportDimensions_Click(object sender, RoutedEventArgs e)
		{
			exportDimensions = (sender as MenuItem).IsChecked;
		}


		public void ShiftDown()
		{
			for(int i = height - 1; i > 0; i--)
			{
				for(int j = 0; j < width; j++)
				{
					dotMatrix[i][j] = dotMatrix[i - 1][j];
				}
			}
			for(int j = 0; j < width; j++)
			{
				dotMatrix[0][j] = false;
			}
		}

		public void ShiftUp()
		{
			for(int i = 0; i < height - 1; i++)
			{
				for(int j = 0; j < width; j++)
				{
					dotMatrix[i][j] = dotMatrix[i+1][j];
				}
			}
			for(int j = 0; j < width; j++)
			{
				dotMatrix[height - 1][j] = false;
			}
		}

		public void ShiftRight()
		{
			for(int j = width - 1; j > 0; j--)
			{
				for(int i = 0; i < height; i++)
				{
					dotMatrix[i][j] = dotMatrix[i][j - 1];
				}
			}
			for(int i = 0; i < height; i++)
			{
				dotMatrix[i][0] = false;
			}
		}

		public void ShiftLeft()
		{
			for(int j = 0; j < width - 1; j++)
			{
				for(int i = 0; i < height; i++)
				{
					dotMatrix[i][j] = dotMatrix[i][j + 1];
				}
			}
			for(int i = 0; i < height; i++)
			{
				dotMatrix[i][width - 1] = false;
			}
		}

		public void FlipVertically()
		{
			bool temp = false;
			for(int i = 0; i < height/2; i++)
			{
				for(int j = 0; j < width; j++)
				{
					temp = dotMatrix[i][j];
					dotMatrix[i][j] = dotMatrix[height-i-1][j];
					dotMatrix[height-i-1][j] = temp;
				}
			}
		}

		public void FlipHorizontally()
		{
			bool temp = false;
			for(int j = 0; j < width/2; j++)
			{
				for(int i = 0; i < height; i++)
				{
					temp = dotMatrix[i][j];
					dotMatrix[i][j] = dotMatrix[i][width-j-1];
					dotMatrix[i][width-j-1] = temp;
				}
			}
		}

		//public void RotateClockwise()	// Maybe Add in the future
		//{
		//
		//}

		private void AssertCollectionSize<T>(List<List<T>> collection, int width, int height) where T : new()
		{
			while(collection.Count < height)
			{
				collection.Add(new List<T>());
			}
			for(int index = 0; index < height; ++index)
			{
				while(collection[index].Count < width)
				{
					collection[index].Add(Activator.CreateInstance<T>());
				}
			}
		}

		private void InitializeList<T>(List<List<T>> list, int width, int height) where T : new()
		{
			for(int i = 0; i < height; i++)
			{
				list.Add(new List<T>(width));
				for(int j = 0; j < width; j++)
				{
					list[i].Add(new T());
				}
			}
		}

		private void BackgroundColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
		{
			if(e.NewValue != null && canvas != null)
			{
				canvas.Background = new SolidColorBrush((Color)e.NewValue);
			}
		}

		private void ResetBackgroundColor(object sender, RoutedEventArgs e)
		{
			canvas.Background = new SolidColorBrush(Colors.DodgerBlue);
			backgroundPicker.SelectedColor = Colors.DodgerBlue;
		}

		private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			ColDef2.MaxWidth = mainWindow.ActualWidth - ColDef0.MinWidth - 21.5;
		}

		//private void GridSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		//{
		//	// Maybe adjust window max dimensions
		//}

		private void ResizeCanvas_Click(object sender, RoutedEventArgs e)
		{
			double canvasNewWidth = 2*canvasSideMargin + width * cellSize;
			double canvasNewHeight = 2*canvasSideMargin + height * cellSize;

			double canvasWidthDelta = canvas.ActualWidth-canvasNewWidth;
			double canvasHeightDelta = canvas.ActualHeight-canvasNewHeight;

			Width -= canvasWidthDelta;
			Height -= canvasHeightDelta;
		}

		private void WindowKeyDown(object sender, KeyEventArgs e)
		{
			if(e.Key == Key.System && e.OriginalSource is ListBoxItem)
			{
				e.Handled = true;
			}
		}
	}

	public class DotMatrixSetting
	{
		private bool[,] matrix;

		public string Name { get; set; }

		public int Width { get; set; }

		public int Height { get; set; }

		public DotMatrixSetting(string name, int w, int h)
		{
			Name = name;
			Width = w;
			Height = h;
			matrix = new bool[h, w];
		}

		public bool this[int i, int j]
		{
			get
			{
				return matrix[i, j];
			}
			set
			{
				matrix[i, j] = value;
			}
		}

		public void SetMatrix(List<List<bool>> newMatrix)
		{
			for(int i = 0; i < Height; i++)
			{
				for(int j = 0; j < Width; j++)
				{
					matrix[i, j] = newMatrix[i][j];
				}
			}
		}

		public void SetMatrix(byte[] newMatrix)
		{
			for(int i = 0; i < Height; i++)
			{
				for(int j = 0; j < Width; j++)
				{
					matrix[i, j] = (newMatrix[i] & (0x80 >> j)) > 0;
				}
			}
		}

		public void LoadMatrix(List<List<bool>> outMatrix)
		{
			while(outMatrix.Count < Height)
			{
				outMatrix.Add(new List<bool>());
			}
			for(int i = 0; i < Height; i++)
			{
				while(outMatrix[i].Count < Width)
				{
					outMatrix[i].Add(false);
				}
				for(int j = 0; j < Width; j++)
				{
					outMatrix[i][j] = matrix[i, j];
				}
			}
		}

		public void TrimMatrix()
		{
			int lastRow = Height;
			int lastColumn = Width;
			int firstRow = 0;
			int firstColumn = 0;

			bool endLoop = false;

			for(int i = 0; i < Height; i++)	// Check if first rows are empty
			{
				for(int j = 0; j < Width; j++)
				{
					if(matrix[i, j])
					{
						endLoop = true;
						break;
					}
				}
				if(endLoop)
				{
					break;
				}
				firstRow++;
			}
			for(int i = 0; i<firstRow; i++)
			{
				ShiftUp();
			}

			endLoop = false;
			for(int j = 0; j < Width; j++) // Check if first columns are empty
			{
				for(int i = 0; i < Height; i++)
				{
					if(matrix[i, j])
					{
						endLoop = true;
						break;
					}
				}
				if(endLoop)
				{
					break;
				}
				firstColumn++;
			}
			for(int j = 0; j < firstColumn; j++)
			{
				ShiftLeft();
			}

			endLoop = false;
			for(int i = Height - 1; i >= 0 && !endLoop; i--)
			{
				for(int j = 0; j < Width; j++)
				{
					if(matrix[i, j])
					{
						endLoop = true;
						break;
					}
				}
				lastRow--;
			}

			endLoop = false;
			for(int j = Width - 1; j >= 0 && !endLoop; j--)
			{
				for(int i = 0; i < Height; i++)
				{
					if(matrix[i, j])
					{
						endLoop = true;
						break;
					}
				}
				lastColumn--;
			}

			if(lastRow == 0 && lastColumn == 0)
			{

			}
			else
			{
				Width = lastColumn + 1;
				Height = lastRow + 1;
			}
		}
		
		/*
		public void ShiftDown()
		{
			for(int i = Height - 1; i > 0; i--)
			{
				for(int j = 0; j < Width; j++)
				{
					matrix[i, j] = matrix[i - 1, j];
				}
			}
			for(int j = 0; j < Width; j++)
			{
				matrix[0, j] = false;
			}
		}
		*/
		public void ShiftUp()
		{
			for(int i = 0; i < Height - 1; i++)
			{
				for(int j = 0; j < Width; j++)
				{
					matrix[i, j] = matrix[i + 1, j];
				}
			}
			for(int j = 0; j < Width; j++)
			{
				matrix[Height - 1, j] = false;
			}
		}
		/*
		public void ShiftRight()
		{
			for(int j = Width - 1; j > 0; j--)
			{
				for(int i = 0; i < Height; i++)
				{
					matrix[i, j] = matrix[i, j - 1];
				}
			}
			for(int i = 0; i < Height; i++)
			{
				matrix[i, 0] = false;
			}
		}
		*/
		public void ShiftLeft()
		{
			for(int j = 0; j < Width - 1; j++)
			{
				for(int i = 0; i < Height; i++)
				{
					matrix[i, j] = matrix[i, j + 1];
				}
			}
			for(int i = 0; i < Height; i++)
			{
				matrix[i, Width - 1] = false;
			}
		}
	}

	//public class SplitterWidthConverter : IValueConverter
	//{
	//	public static Window window { get; set; }
	//
	//	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	//	{
	//		if(window != null)
	//			return (window as MainWindow).ActualWidth - (double)value - 30;
	//		else
	//			return 0;
	//	}
	//
	//	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	//	{
	//		return 0;
	//	}
	//}
}
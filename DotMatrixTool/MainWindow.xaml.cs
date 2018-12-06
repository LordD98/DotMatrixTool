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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DotMatrixTool.Commands;

namespace DotMatrixTool
{
	public partial class MainWindow
	{

		enum ConversionType
		{
			MatrixIn,
			MatrixOut,
			CodesIn,
			CodesOut,
		}
		MainWindow.ConversionType conversionType = MainWindow.ConversionType.CodesIn;

		enum ClickType
		{
			None,
			Red,
			White,
		}
		MainWindow.ClickType currentClick = MainWindow.ClickType.None;

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

			listBoxItems = new ObservableCollection<DotMatrixSetting>();
			lbxSavedPatterns.ItemsSource = listBoxItems;
			SettingCommands.Context = this;

			Loaded += new RoutedEventHandler(MainWindow_Loaded);
			Loaded += (x, y) => Keyboard.Focus(canvas);
		}

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

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			SettingCommands.BindCommandsToWindow(this);
		}

		private void DrawInit(object sender, RoutedEventArgs e)
		{
			RedrawGrid();
		}

		private void Convert(object sender, RoutedEventArgs e)
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
						DotMatrixSetting newSetting = new DotMatrixSetting(string.Format("#{0}", (object)chunk), newWidth, newHeight);
						newSetting.SetMatrix(byteData[chunk]);
						listBoxItems.Add(newSetting);
					}
					if(listBoxItems.Count > oldCount && loadImmediately)
					{
						lbxSavedPatterns.SelectedIndex = oldCount;
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
					int numSettings = listBoxItems.Count;
					string[] strCodes = tbxCode.Text.Split('\n');
					for(int k = 0; k < strCodes.Length; ++k)
					{
						string[] code = strCodes[k].Replace("\r", "").Split(',');
						int newHeight = code.Length;
						byte[] newMatrix = new byte[newHeight];
						int newWidth = code[0].Length * 4;
						DotMatrixSetting newSetting = new DotMatrixSetting(string.Format("#{0}", (object)k), newWidth, newHeight);
						for(int index2 = 0; index2 < newHeight; ++index2)
						{
							newMatrix[index2] = System.Convert.ToByte(code[index2], 16);
						}
						newSetting.SetMatrix(newMatrix);
						listBoxItems.Add(newSetting);
					}
					if(listBoxItems.Count > numSettings && loadImmediately)
					{
						lbxSavedPatterns.SelectedIndex = numSettings;
						SettingCommands.Load.Execute(null, null);
					}
					break;
				}
				case ConversionType.CodesOut:
				{
					string code = "";
					foreach(DotMatrixSetting setting in listBoxItems)
					{
						for(int i = 0; i < height; i++)
						{
							byte num = 0;
							for(int j = 0; j < 8; j++)
							{
								if(setting[i, j])
								{
									num |= (byte)(1 << width - 1 - j);
								}
							}
							code += num.ToString("X2") + ",";
						}
						code = code.Remove(code.Length - 1);
						code += "\n";
					}
					tbxCode.Text = code;
					break;
				}
			}
		}

		private void RedrawGrid()
		{
			AssertCollectionSize(dotMatrix, width, height);
			AssertCollectionSize(buttons, width, height);
			int actualWidth = (int)canvas.ActualWidth;
			int actualHeight = (int)canvas.ActualHeight;
			int maxNx = (actualWidth-canvasSideMargin)/cellSize;
			int maxNy = (actualHeight-canvasSideMargin)/cellSize;
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
						Canvas.SetLeft(buttons[i][j], (j*cellSize + canvasSideMargin));
						Canvas.SetTop(buttons[i][j], (i*cellSize + canvasSideMargin));
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
			if(lastClick.Valid && Keyboard.IsKeyDown(Key.LeftShift) && (i == lastClick.I || j == lastClick.J || di == dj || di == -dj))
			{
				while(di != 0 || dj != 0)
				{
					buttons[i-di][j-dj].Fill = lastClick.Type == MainWindow.ClickType.Red ? Brushes.White : Brushes.Red;
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
				if(i < 0 || j < 0 || buttons[i][j].Fill == Brushes.White)
				{
					currentClick = ClickType.White;
				}
				else if(buttons[i][j].Fill == Brushes.Red)
				{
					currentClick = ClickType.Red;
				}
				lastClick.Type = currentClick;
				LED_MouseEnter(sender, null);
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
			int selectedIndex = lbxSavedPatterns.SelectedIndex;
			if(selectedIndex == -1)
			{
				return;
			}
			setting = new DotMatrixSetting(setting.Name, width, height);
			setting.SetMatrix(dotMatrix);
			listBoxItems[selectedIndex] = setting;
			lbxSavedPatterns.SelectedIndex = selectedIndex;
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

		private void BtnClear_Click(object sender, RoutedEventArgs e)
		{
			dotMatrix = new List<List<bool>>();
			RedrawGrid();
		}

		private void BtnInput_Click(object sender, RoutedEventArgs e)
		{
			string[] strArray = tbxCodeInput.Text.Split(';');
			List<List<bool>> boolListList = new List<List<bool>>();
			foreach(string s in strArray)
			{
				UInt64 result = UInt64.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
				for(int i = 0; i < 8; i++)
				{
					byte row = (byte)((result >> (i*8)) & 0xFF);
					for(int j = 0; j < 8; j++)
					{
						dotMatrix[i][j] = (row & (1 << j)) != 0;
					}
				}
			}
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
		
		private void Canvas_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.Key != Key.Return)
			{

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

		private void ListBoxSavedPatterns_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
		}

		private void CbxUseSmallLEDs_Clicked(object sender, RoutedEventArgs e)
		{
			useSmallLeds = cbxUseSmallLeds.IsChecked.Value;
			cellSize = useSmallLeds ? 22 : 33;
			diameter = useSmallLeds ? 11 : 22;
			int oldWidthValue = (int)sldWidth.Value;
			int oldHeightValue = (int)sldHeight.Value;
			if(!useSmallLeds)
			{

			}
			sldWidth.Value = oldWidthValue > sldWidth.Maximum ? sldWidth.Maximum : oldWidthValue;
			sldHeight.Value = oldHeightValue > sldHeight.Maximum ? sldHeight.Maximum : oldHeightValue;
			for(int i = 0; i < buttons.Count; i++)
			{
				for(int j = 0; j < buttons[i].Count; j++)
				{
					buttons[i][j].Height = diameter;
					buttons[i][j].Width = diameter;
					Canvas.SetLeft(buttons[i][j], (j*cellSize + canvasSideMargin));
					Canvas.SetTop(buttons[i][j], (i*cellSize + canvasSideMargin));
				}
			}
			Canvas_Resize(null, null);
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

		private void UpdateDimensions_Click(object sender, RoutedEventArgs e)
		{
			updateDimensions = (sender as MenuItem).IsChecked;
		}

		private void LoadImmediately_Click(object sender, RoutedEventArgs e)
		{
			loadImmediately = (sender as MenuItem).IsChecked;
		}


		private void TestText_DoubleClick(object sender, MouseButtonEventArgs e)
		{
			(sender as TextBox).Focusable = true;
			(sender as TextBox).IsReadOnly = false;
			(sender as TextBox).CaretBrush = Brushes.Black;
			(sender as TextBox).Cursor = Cursors.IBeam;
			e.Handled = true;
			(sender as TextBox).Focus();
			(sender as TextBox).SelectAll();
		}

		private void TestTextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			(sender as TextBox).Focusable = false;
			(sender as TextBox).IsReadOnly = true;
			(sender as TextBox).CaretBrush = Brushes.Transparent;
			(sender as TextBox).Cursor = Cursors.Arrow;
		}

		private void TestTextBox_KeyDown(object sender, KeyEventArgs e)
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
			for(int index1 = 0; index1 < Height; ++index1)
			{
				for(int index2 = 0; index2 < Width; ++index2)
					matrix[index1, index2] = newMatrix[index1][index2];
			}
		}

		public void SetMatrix(byte[] newMatrix)
		{
			for(int index1 = 0; index1 < Height; ++index1)
			{
				for(int index2 = 0; index2 < Width; ++index2)
					matrix[index1, index2] = ((int)newMatrix[index1] & 128 >> index2) > 0;
			}
		}

		public void LoadMatrix(List<List<bool>> outMatrix)
		{
			while(outMatrix.Count < Height)
				outMatrix.Add(new List<bool>());
			for(int index1 = 0; index1 < Height; ++index1)
			{
				while(outMatrix[index1].Count < Width)
					outMatrix[index1].Add(false);
				for(int index2 = 0; index2 < Width; ++index2)
					outMatrix[index1][index2] = matrix[index1, index2];
			}
		}
	}
}
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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using DotMatrixTool.Commands;

namespace DotMatrixTool
{
	public partial class MainWindow
	{

		private enum ConversionType
		{
			MatrixIn,
			MatrixOut,
			CodesIn,
			CodesOut,
		}
		private MainWindow.ConversionType conversionType = MainWindow.ConversionType.CodesIn;

		private enum ClickType
		{
			None,
			Red,
			White,
		}
		private MainWindow.ClickType currentClick = MainWindow.ClickType.None;

		private struct ClickPosition
		{
			public int I;
			public int J;
			public bool Valid;
			public MainWindow.ClickType Type;
		}
		private ClickPosition lastClick;

		public int width = 7;
		public int height = 5;
		public bool useSmallLeds = false;
		private bool updateDimensions = true;
		private bool loadImmediately = true;
		private const int canvasSideMargin = 14;
		private const double u = 11.13;
		private int cellSize;
		private int diameter;
		private List<List<Ellipse>> buttons;
		private List<List<bool>> dotMatrix;
		private ObservableCollection<DotMatrixSetting> listBoxItems;

		//internal CheckBox cbxUseSmallLeds;
		//internal Canvas canvas;
		//internal TextBox tbxCode;
		//internal ListBox lbxSavedPatterns;
		//internal Button btnSave;
		//internal Button btnLoad;
		//internal Button btnClear;
		//internal Label lblWidth;
		//internal Slider sldWidth;
		//internal Label lblHeight;
		//internal Slider sldHeight;
		//internal TextBox tbxCodeInput;
		//internal Button btnInput;
		//internal Button btnNew;
		//internal Button btnDelete;
		//internal Button btnConvert;
		//private bool _contentLoaded;
		

		public MainWindow()
		{
			cellSize = this.useSmallLeds ? 22 : 33;
			diameter = this.useSmallLeds ? 11 : 22;
			buttons = new List<List<Ellipse>>(this.height);
			dotMatrix = new List<List<bool>>(this.height);
			InitializeList<Ellipse>(this.buttons, this.width, this.height);
			InitializeList<bool>(this.dotMatrix, this.width, this.height);
			InitializeComponent();
			listBoxItems = new ObservableCollection<DotMatrixSetting>();
			lbxSavedPatterns.ItemsSource = (IEnumerable)this.listBoxItems;
			SettingCommands.Context = (Window)this;
			this.Loaded += new RoutedEventHandler(this.MainWindow_Loaded);
			this.Loaded += (x, y) => Keyboard.Focus(canvas);
		}

		private void AssertCollectionSize<T>(List<List<T>> collection, int width, int height) where T : new()
		{
			while(collection.Count < height)
				collection.Add(new List<T>());
			for(int index = 0; index < height; ++index)
			{
				while(collection[index].Count < width)
					collection[index].Add(Activator.CreateInstance<T>());
			}
		}

		private void InitializeList<T>(List<List<T>> list, int width, int height) where T : new()
		{
			for(int index1 = 0; index1 < height; ++index1)
			{
				list.Add(new List<T>(width));
				for(int index2 = 0; index2 < width; ++index2)
					list[index1].Add(Activator.CreateInstance<T>());
			}
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			SettingCommands.BindCommandsToWindow((Window)this);
		}

		private void DrawInit(object sender, RoutedEventArgs e)
		{
			this.RedrawGrid();
		}

		private void Convert(object sender, RoutedEventArgs e)
		{
			switch(this.conversionType)
			{
				case MainWindow.ConversionType.MatrixIn:
					int count1 = this.listBoxItems.Count;
					string[] strArray1 = this.tbxCode.Text.Split(new char[1]
					{
			'\n'
					}, StringSplitOptions.RemoveEmptyEntries);
					int h = -1;
					for(int index = 0; index < strArray1.Length; ++index)
					{
						strArray1[index] = strArray1[index].Replace("\r", "").Replace(" ", "");
						if(h == -1 && strArray1[index] == "")
							h = index;
					}
					int length1 = ((IEnumerable<string>)strArray1).Count<string>((Func<string, bool>)(x => x == "")) + 1;
					int length2 = strArray1[0].Length;
					byte[][] numArray = new byte[length1][];
					for(int index1 = 0; index1 < length1; ++index1)
					{
						numArray[index1] = new byte[h];
						for(int index2 = 0; index2 < h; ++index2)
							numArray[index1][index2] = System.Convert.ToByte(strArray1[index1 * (h + 1) + index2], 2);
						DotMatrixSetting dotMatrixSetting = new DotMatrixSetting(string.Format("#{0}", (object)index1), length2, h);
						dotMatrixSetting.SetMatrix(numArray[index1]);
						this.listBoxItems.Add(dotMatrixSetting);
					}
					if(this.listBoxItems.Count <= count1 || !this.loadImmediately)
						break;
					this.lbxSavedPatterns.SelectedIndex = count1;
					SettingCommands.Load.Execute((object)null, (IInputElement)null);
					break;
				case MainWindow.ConversionType.MatrixOut:
					string str1 = "";
					for(int index1 = 0; index1 < this.height; ++index1)
					{
						for(int index2 = 0; index2 < this.width; ++index2)
							str1 = str1 + (this.dotMatrix[index1][index2] ? "1" : "0") + " ";
						str1 += "\n";
					}
					this.tbxCode.Text = str1;
					break;
				case MainWindow.ConversionType.CodesIn:
					int count2 = this.listBoxItems.Count;
					int height = this.height;
					int width = this.width;
					string[] strArray2 = this.tbxCode.Text.Split('\n');
					for(int index1 = 0; index1 < strArray2.Length; ++index1)
					{
						string[] strArray3 = strArray2[index1].Replace("\r", "").Split(',');
						int length3 = strArray3.Length;
						byte[] newMatrix = new byte[length3];
						int w = strArray3[0].Length * 4;
						DotMatrixSetting dotMatrixSetting = new DotMatrixSetting(string.Format("#{0}", (object)index1), w, length3);
						for(int index2 = 0; index2 < length3; ++index2)
							newMatrix[index2] = System.Convert.ToByte(strArray3[index2], 16);
						dotMatrixSetting.SetMatrix(newMatrix);
						this.listBoxItems.Add(dotMatrixSetting);
					}
					if(this.listBoxItems.Count <= count2 || !this.loadImmediately)
						break;
					this.lbxSavedPatterns.SelectedIndex = count2;
					SettingCommands.Load.Execute((object)null, (IInputElement)null);
					break;
				case MainWindow.ConversionType.CodesOut:
					string str2 = "";
					foreach(DotMatrixSetting listBoxItem in (Collection<DotMatrixSetting>)this.listBoxItems)
					{
						for(int index1 = 0; index1 < this.height; ++index1)
						{
							uint num = 0;
							for(int index2 = 0; index2 < 8; ++index2)
							{
								if(listBoxItem[index1, index2])
									num |= (uint)(1 << this.width - 1 - index2);
							}
							str2 = str2 + num.ToString("X2") + ",";
						}
						str2 = str2.Remove(str2.Length - 1);
						str2 += "\n";
					}
					this.tbxCode.Text = str2;
					break;
			}
		}

		private void RedrawGrid()
		{
			this.AssertCollectionSize<bool>(this.dotMatrix, this.width, this.height);
			this.AssertCollectionSize<Ellipse>(this.buttons, this.width, this.height);
			int actualWidth = (int)this.canvas.ActualWidth;
			int actualHeight = (int)this.canvas.ActualHeight;
			int num1 = (actualWidth - 14) / this.cellSize;
			int num2 = num1 < this.width ? num1 : this.width;
			int num3 = (actualHeight - 14) / this.cellSize;
			int num4 = num3 < this.height ? num3 : this.height;
			for(int index1 = 0; index1 < this.buttons.Count; ++index1)
			{
				for(int index2 = 0; index2 < this.buttons[index1].Count; ++index2)
				{
					if(!this.canvas.Children.Contains((UIElement)this.buttons[index1][index2]))
					{
						this.buttons[index1][index2].Height = (double)this.diameter;
						this.buttons[index1][index2].Width = (double)this.diameter;
						Canvas.SetLeft((UIElement)this.buttons[index1][index2], (double)(index2 * this.cellSize + 14));
						Canvas.SetTop((UIElement)this.buttons[index1][index2], (double)(index1 * this.cellSize + 14));
						this.canvas.Children.Add((UIElement)this.buttons[index1][index2]);
					}
					if(index1 < num4 && index2 < num2)
					{
						this.buttons[index1][index2].Fill = this.dotMatrix[index1][index2] ? (Brush)Brushes.Red : (Brush)Brushes.White;
						this.buttons[index1][index2].Stroke = (Brush)Brushes.Red;
						this.buttons[index1][index2].MouseLeftButtonDown -= new MouseButtonEventHandler(this.LED_MouseLeftButtonDown);
						this.buttons[index1][index2].MouseLeftButtonUp -= new MouseButtonEventHandler(this.LED_MouseLeftButtonUp);
						this.buttons[index1][index2].MouseEnter -= new MouseEventHandler(this.LED_MouseEnter);
						this.buttons[index1][index2].MouseLeave -= new MouseEventHandler(this.LED_MouseLeave);
						this.buttons[index1][index2].MouseLeftButtonDown += new MouseButtonEventHandler(this.LED_MouseLeftButtonDown);
						this.buttons[index1][index2].MouseLeftButtonUp += new MouseButtonEventHandler(this.LED_MouseLeftButtonUp);
						this.buttons[index1][index2].MouseEnter += new MouseEventHandler(this.LED_MouseEnter);
						this.buttons[index1][index2].MouseLeave += new MouseEventHandler(this.LED_MouseLeave);
					}
					else
					{
						this.buttons[index1][index2].Fill = (Brush)Brushes.Transparent;
						this.buttons[index1][index2].Stroke = (Brush)Brushes.Transparent;
						this.buttons[index1][index2].MouseLeftButtonDown -= new MouseButtonEventHandler(this.LED_MouseLeftButtonDown);
						this.buttons[index1][index2].MouseLeftButtonUp -= new MouseButtonEventHandler(this.LED_MouseLeftButtonUp);
						this.buttons[index1][index2].MouseEnter -= new MouseEventHandler(this.LED_MouseEnter);
						this.buttons[index1][index2].MouseLeave -= new MouseEventHandler(this.LED_MouseLeave);
					}
				}
			}
		}

		private void LED_MouseEnter(object sender, MouseEventArgs e)
		{
			if(Mouse.LeftButton != MouseButtonState.Pressed)
				return;
			(int i, int j) = GetClickedEllipseIndex();
			if(i < 0 || j < 0)
				return;
			switch(this.currentClick)
			{
				case MainWindow.ClickType.None:
				case MainWindow.ClickType.White:
					this.buttons[i][j].Fill = (Brush)Brushes.Red;
					break;
				case MainWindow.ClickType.Red:
					this.buttons[i][j].Fill = (Brush)Brushes.White;
					break;
			}
		}

		private void LED_MouseLeave(object sender, MouseEventArgs e)
		{
			if(e.LeftButton != MouseButtonState.Pressed)
				return;
			this.lastClick.Valid = false;
		}

		private void LED_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			this.currentClick = MainWindow.ClickType.None;
			for(int index1 = 0; index1 < this.height; ++index1)
			{
				for(int index2 = 0; index2 < this.width; ++index2)
					this.dotMatrix[index1][index2] = this.buttons[index1][index2].Fill == Brushes.Red;
			}
			e.Handled = true;
		}

		private void LED_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Keyboard.Focus((IInputElement)this.canvas);
			(int i, int j) = GetClickedEllipseIndex();
			int num1 = i - this.lastClick.I;
			int num2 = j - this.lastClick.J;
			if(this.lastClick.Valid && Keyboard.IsKeyDown(Key.LeftShift) && (i == this.lastClick.I || j == this.lastClick.J || num1 == num2 || num1 == -num2))
			{
				while(num1 != 0 || (uint)num2 > 0U)
				{
					this.buttons[i - num1][j - num2].Fill = this.lastClick.Type == MainWindow.ClickType.Red ? (Brush)Brushes.White : (Brush)Brushes.Red;
					if(num1 > 0)
						--num1;
					if(num2 > 0)
						--num2;
					if(num1 < 0)
						++num1;
					if(num2 < 0)
						++num2;
				}
				this.buttons[i][j].Fill = this.lastClick.Type == MainWindow.ClickType.Red ? (Brush)Brushes.White : (Brush)Brushes.Red;
			}
			else
			{
				if(i < 0 || j < 0 || this.buttons[i][j].Fill == Brushes.White)
					this.currentClick = MainWindow.ClickType.White;
				else if(this.buttons[i][j].Fill == Brushes.Red)
					this.currentClick = MainWindow.ClickType.Red;
				this.lastClick.Type = this.currentClick;
				this.LED_MouseEnter(sender, (MouseEventArgs)null);
			}
			ref int local1 = ref this.lastClick.I;
			ref int local2 = ref this.lastClick.J;
			int num3 = i;
			int num4 = j;
			local1 = num3;
			int num5 = num4;
			local2 = num5;
			this.lastClick.Valid = true;
		}

		public void LoadDotMatrixFromSetting(DotMatrixSetting setting)
		{
			this.dotMatrix = new List<List<bool>>();
			setting.LoadMatrix(this.dotMatrix);
			if(this.updateDimensions)
			{
				if(this.sldHeight.Maximum < (double)setting.Height)
					this.sldHeight.Maximum = (double)setting.Height;
				if(this.sldWidth.Maximum < (double)setting.Width)
					this.sldWidth.Maximum = (double)setting.Width;
				this.sldHeight.Value = (double)setting.Height;
				this.sldWidth.Value = (double)setting.Width;
			}
			this.RedrawGrid();
		}

		public void SaveDotMatrixToSetting(DotMatrixSetting setting)
		{
			int selectedIndex = this.lbxSavedPatterns.SelectedIndex;
			if(selectedIndex == -1)
				return;
			setting = new DotMatrixSetting(setting.Name, this.width, this.height);
			setting.SetMatrix(this.dotMatrix);
			this.listBoxItems[selectedIndex] = setting;
			this.lbxSavedPatterns.SelectedIndex = selectedIndex;
		}

		private void SldWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			this.width = (int)e.NewValue;
			this.lblWidth.Content = (object)string.Format("Width: {0}", (object)this.width);
			this.RedrawGrid();
		}

		private void SldHeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			this.height = (int)e.NewValue;
			this.lblHeight.Content = (object)string.Format("Height: {0}", (object)this.height);
			this.RedrawGrid();
		}

		private (int, int) GetClickedEllipseIndex()
		{
			for(int index1 = 0; index1 < this.height; ++index1)
			{
				for(int index2 = 0; index2 < this.width; ++index2)
				{
					if(this.buttons[index1][index2].IsMouseOver)
						return (index1, index2);
				}
			}
			return (-1, -1);
		}

		private void BtnClear_Click(object sender, RoutedEventArgs e)
		{
			this.dotMatrix = new List<List<bool>>();
			this.RedrawGrid();
		}

		private void BtnInput_Click(object sender, RoutedEventArgs e)
		{
			string[] strArray = this.tbxCodeInput.Text.Split(';');
			List<List<bool>> boolListList = new List<List<bool>>();
			foreach(string s in strArray)
			{
				ulong result;
				if(ulong.TryParse(s, NumberStyles.HexNumber, (IFormatProvider)CultureInfo.InvariantCulture, out result))
				{
					for(int index1 = 0; index1 < 8; ++index1)
					{
						byte num = (byte)(result >> index1 * 8 & (ulong)byte.MaxValue);
						for(int index2 = 0; index2 < 8; ++index2)
							this.dotMatrix[index1][index2] = (1 << index2 & (int)num) != 0;
					}
				}
			}
		}

		private void Canvas_Resize(object sender, SizeChangedEventArgs e)
		{
			Size newSize;
			double num1;
			if(e == null)
			{
				num1 = this.canvas.ActualWidth;
			}
			else
			{
				newSize = e.NewSize;
				num1 = newSize.Width;
			}
			int num2 = (int)num1;
			double num3;
			if(e == null)
			{
				num3 = this.canvas.ActualHeight;
			}
			else
			{
				newSize = e.NewSize;
				num3 = newSize.Height;
			}
			int num4 = (int)num3;
			int num5 = (num2 - 14) / this.cellSize;
			int num6 = (num4 - 14) / this.cellSize;
			int num7 = (int)this.sldWidth.Value;
			int num8 = (int)this.sldHeight.Value;
			this.sldWidth.Maximum = (double)num5 > this.sldWidth.Maximum ? (double)num5 : this.sldWidth.Maximum;
			this.sldHeight.Maximum = (double)num6 > this.sldHeight.Maximum ? (double)num6 : this.sldHeight.Maximum;
			this.RedrawGrid();
		}

		private void TestText_DoubleClick(object sender, MouseButtonEventArgs e)
		{
			(sender as TextBox).Focusable = true;
			(sender as TextBox).IsReadOnly = false;
			(sender as TextBox).CaretBrush = (Brush)Brushes.Black;
			(sender as TextBox).Cursor = Cursors.IBeam;
			e.Handled = true;
			(sender as TextBox).Focus();
			(sender as TextBox).SelectAll();
		}

		private void TestTextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			(sender as TextBox).Focusable = false;
			(sender as TextBox).IsReadOnly = true;
			(sender as TextBox).CaretBrush = (Brush)Brushes.Transparent;
			(sender as TextBox).Cursor = Cursors.Arrow;
		}

		private void TestTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.Key != Key.Return)
				return;
			(sender as TextBox).Focusable = false;
			(sender as TextBox).IsReadOnly = true;
			(sender as TextBox).CaretBrush = (Brush)Brushes.Transparent;
			(sender as TextBox).Cursor = Cursors.Arrow;
		}

		private void Canvas_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.Key != Key.Return)
				;
		}

		private void ListBoxSavedPatterns_KeyDown(object sender, KeyEventArgs e)
		{
			ListBox listBox = sender as ListBox;
			if(e.Key != Key.Delete)
				return;
			SettingCommands.Delete.Execute((object)null, (IInputElement)null);
		}

		private void ListBoxSavedPatterns_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
		}

		private void CbxUseSmallLEDs_Clicked(object sender, RoutedEventArgs e)
		{
			this.useSmallLeds = this.cbxUseSmallLeds.IsChecked.Value;
			this.cellSize = this.useSmallLeds ? 22 : 33;
			this.diameter = this.useSmallLeds ? 11 : 22;
			int num1 = (int)this.sldWidth.Value;
			int num2 = (int)this.sldHeight.Value;
			if(!this.useSmallLeds)
				;
			this.sldWidth.Value = (double)num1 > this.sldWidth.Maximum ? this.sldWidth.Maximum : (double)num1;
			this.sldHeight.Value = (double)num2 > this.sldHeight.Maximum ? this.sldHeight.Maximum : (double)num2;
			for(int index1 = 0; index1 < this.buttons.Count; ++index1)
			{
				for(int index2 = 0; index2 < this.buttons[index1].Count; ++index2)
				{
					this.buttons[index1][index2].Height = (double)this.diameter;
					this.buttons[index1][index2].Width = (double)this.diameter;
					Canvas.SetLeft((UIElement)this.buttons[index1][index2], (double)(index2 * this.cellSize + 14));
					Canvas.SetTop((UIElement)this.buttons[index1][index2], (double)(index1 * this.cellSize + 14));
				}
			}
			this.Canvas_Resize((object)null, (SizeChangedEventArgs)null);
		}

		private void MenuItemConvert_Click(object sender, RoutedEventArgs e)
		{
			ItemCollection items = ((sender as MenuItem).Parent as MenuItem).Items;
			for(int index = 0; index < items.Count; ++index)
			{
				if(sender == items[index])
				{
					(items[index] as MenuItem).IsChecked = true;
					this.conversionType = (MainWindow.ConversionType)index;
				}
				else
					(items[index] as MenuItem).IsChecked = false;
			}
		}

		private void UpdateDimensions_Click(object sender, RoutedEventArgs e)
		{
			this.updateDimensions = (sender as MenuItem).IsChecked;
		}

		private void LoadImmediately_Click(object sender, RoutedEventArgs e)
		{
			this.loadImmediately = (sender as MenuItem).IsChecked;
		}
		

		//[DebuggerNonUserCode]
		//[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
		//[EditorBrowsable(EditorBrowsableState.Never)]
		//void IComponentConnector.Connect(int connectionId, object target)
		//{
		//	switch(connectionId)
		//	{
		//		case 1:
		//			((MenuItem)target).Click += new RoutedEventHandler(this.CbxUseSmallLEDs_Clicked);
		//			break;
		//		case 2:
		//			((MenuItem)target).Click += new RoutedEventHandler(this.MenuItemConvert_Click);
		//			break;
		//		case 3:
		//			((MenuItem)target).Click += new RoutedEventHandler(this.MenuItemConvert_Click);
		//			break;
		//		case 4:
		//			((MenuItem)target).Click += new RoutedEventHandler(this.MenuItemConvert_Click);
		//			break;
		//		case 5:
		//			((MenuItem)target).Click += new RoutedEventHandler(this.MenuItemConvert_Click);
		//			break;
		//		case 6:
		//			((MenuItem)target).Click += new RoutedEventHandler(this.UpdateDimensions_Click);
		//			break;
		//		case 7:
		//			((MenuItem)target).Click += new RoutedEventHandler(this.LoadImmediately_Click);
		//			break;
		//		case 8:
		//			this.cbxUseSmallLeds = (CheckBox)target;
		//			this.cbxUseSmallLeds.Click += new RoutedEventHandler(this.CbxUseSmallLEDs_Clicked);
		//			break;
		//		case 9:
		//			this.canvas = (Canvas)target;
		//			this.canvas.MouseUp += new MouseButtonEventHandler(this.LED_MouseLeftButtonUp);
		//			this.canvas.Loaded += new RoutedEventHandler(this.DrawInit);
		//			this.canvas.SizeChanged += new SizeChangedEventHandler(this.Canvas_Resize);
		//			this.canvas.KeyDown += new KeyEventHandler(this.Canvas_KeyDown);
		//			break;
		//		case 10:
		//			this.tbxCode = (TextBox)target;
		//			break;
		//		case 11:
		//			this.lbxSavedPatterns = (ListBox)target;
		//			this.lbxSavedPatterns.KeyDown += new KeyEventHandler(this.ListBoxSavedPatterns_KeyDown);
		//			this.lbxSavedPatterns.SelectionChanged += new SelectionChangedEventHandler(this.ListBoxSavedPatterns_SelectionChanged);
		//			break;
		//		case 13:
		//			this.btnSave = (Button)target;
		//			break;
		//		case 14:
		//			this.btnLoad = (Button)target;
		//			break;
		//		case 15:
		//			this.btnClear = (Button)target;
		//			this.btnClear.Click += new RoutedEventHandler(this.BtnClear_Click);
		//			break;
		//		case 16:
		//			this.lblWidth = (Label)target;
		//			break;
		//		case 17:
		//			this.sldWidth = (Slider)target;
		//			this.sldWidth.ValueChanged += new RoutedPropertyChangedEventHandler<double>(this.SldWidth_ValueChanged);
		//			break;
		//		case 18:
		//			this.lblHeight = (Label)target;
		//			break;
		//		case 19:
		//			this.sldHeight = (Slider)target;
		//			this.sldHeight.ValueChanged += new RoutedPropertyChangedEventHandler<double>(this.SldHeight_ValueChanged);
		//			break;
		//		case 20:
		//			this.tbxCodeInput = (TextBox)target;
		//			break;
		//		case 21:
		//			this.btnInput = (Button)target;
		//			this.btnInput.Click += new RoutedEventHandler(this.BtnInput_Click);
		//			break;
		//		case 22:
		//			this.btnNew = (Button)target;
		//			break;
		//		case 23:
		//			this.btnDelete = (Button)target;
		//			break;
		//		case 24:
		//			this.btnConvert = (Button)target;
		//			this.btnConvert.Click += new RoutedEventHandler(this.Convert);
		//			break;
		//		default:
		//			this._contentLoaded = true;
		//			break;
		//	}
		//}
		//
		//[DebuggerNonUserCode]
		//[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
		//[EditorBrowsable(EditorBrowsableState.Never)]
		//void IStyleConnector.Connect(int connectionId, object target)
		//{
		//	if(connectionId != 12)
		//		return;
		//	((Control)target).MouseDoubleClick += new MouseButtonEventHandler(this.TestText_DoubleClick);
		//	((UIElement)target).LostFocus += new RoutedEventHandler(this.TestTextBox_LostFocus);
		//	((UIElement)target).PreviewKeyDown += new KeyEventHandler(this.TestTextBox_KeyDown);
		//}
	}

	public class DotMatrixSetting
	{
		private bool[,] matrix;

		public string Name { get; set; }

		public int Width { get; set; }

		public int Height { get; set; }

		public DotMatrixSetting(string name, int w, int h)
		{
			this.Name = name;
			this.Width = w;
			this.Height = h;
			this.matrix = new bool[h, w];
		}

		public bool this[int i, int j]
		{
			get
			{
				return this.matrix[i, j];
			}
			set
			{
				this.matrix[i, j] = value;
			}
		}

		public void SetMatrix(List<List<bool>> newMatrix)
		{
			for(int index1 = 0; index1 < this.Height; ++index1)
			{
				for(int index2 = 0; index2 < this.Width; ++index2)
					this.matrix[index1, index2] = newMatrix[index1][index2];
			}
		}

		public void SetMatrix(byte[] newMatrix)
		{
			for(int index1 = 0; index1 < this.Height; ++index1)
			{
				for(int index2 = 0; index2 < this.Width; ++index2)
					this.matrix[index1, index2] = ((int)newMatrix[index1] & 128 >> index2) > 0;
			}
		}

		public void LoadMatrix(List<List<bool>> outMatrix)
		{
			while(outMatrix.Count < this.Height)
				outMatrix.Add(new List<bool>());
			for(int index1 = 0; index1 < this.Height; ++index1)
			{
				while(outMatrix[index1].Count < this.Width)
					outMatrix[index1].Add(false);
				for(int index2 = 0; index2 < this.Width; ++index2)
					outMatrix[index1][index2] = this.matrix[index1, index2];
			}
		}
	}
}
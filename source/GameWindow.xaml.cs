﻿using System;
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
using System.Windows.Shapes;

namespace Impalers {
	/// <summary>
	/// Interaction logic for GameWindow.xaml
	/// </summary>
	public partial class GameWindow : Window {
		//---------------------------- Fields ---------------------------------
		Game game = new Game();
		byte startClickX, startClickY, endClickX, endClickY;
		bool isBotThinking = false;
		double widthMod;
		Line impaleLine;
		Size gridSize, gridHalfSize;

		//---------------------------- Properties ---------------------------------


		//---------------------------- Methods ---------------------------------
		public void StartGame(Game.Enemy enemy) {
			foreach(var c in TopGrid.Children)
				(c as Image).Source = null;
			foreach(var c in LeftGrid.Children)
				(c as Image).Source = null;
			foreach(var c in BottomGrid.Children)
				(c as Image).Source = null;
			foreach(var c in RightGrid.Children)
				(c as Image).Source = null;
			this.ScoreEnemy.Text = this.ScorePlayer.Text = "0";

			impaleLine = new Line() {
				Fill = Brushes.Red,
				StrokeThickness = 5,
				Stroke = Brushes.Red,

			};
			MeatCanvas.Children.Add(impaleLine);

			game.Start(enemy);
		}

		public GameWindow() {
			InitializeComponent();

			game.BottomGrid = BottomGrid;
			game.TopGrid = TopGrid;
			game.LeftGrid = LeftGrid;
			game.RightGrid = RightGrid;

			MeatCanvas.MouseLeave += (a, b) => {
				impaleLine.X1 = impaleLine.X2 = impaleLine.Y1 = impaleLine.Y2 = -1000;
			};

			this.SizeChanged += (a, b) => {
				gridSize = new Size(game.map[0, 0].grid.RenderSize.Width, game.map[0, 0].grid.RenderSize.Height);
				gridHalfSize = new Size(gridSize.Width / 2, gridSize.Height / 2);
			};

			WindowManager.AddWindow(this);
			for(byte i = 0; i < Settings.sizeY; ++i) {
				MeatGrid.RowDefinitions.Add(new RowDefinition());

				LeftGrid.RowDefinitions.Add(new RowDefinition());
				LeftGrid.Children.Add(new Image() {
					VerticalAlignment = VerticalAlignment.Stretch,
					HorizontalAlignment = HorizontalAlignment.Left,
				});
				Grid.SetRow(LeftGrid.Children[i], i);

				RightGrid.RowDefinitions.Add(new RowDefinition());
				RightGrid.Children.Add(new Image() {
					VerticalAlignment = VerticalAlignment.Stretch,
					HorizontalAlignment = HorizontalAlignment.Left,
					RenderTransformOrigin = new Point(0.5, 0.5),
					RenderTransform = new ScaleTransform(-1, 1),
				});
				Grid.SetRow(RightGrid.Children[i], i);
			}

			for(byte i = 0; i < Settings.sizeX; ++i) {
				MeatGrid.ColumnDefinitions.Add(new ColumnDefinition());

				TopGrid.ColumnDefinitions.Add(new ColumnDefinition());
				TopGrid.Children.Add(new Image() {
					VerticalAlignment = VerticalAlignment.Bottom,
					HorizontalAlignment = HorizontalAlignment.Stretch,
				});
				Grid.SetColumn(TopGrid.Children[i], i);

				BottomGrid.ColumnDefinitions.Add(new ColumnDefinition());
				BottomGrid.Children.Add(new Image() {
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					RenderTransformOrigin = new Point(0.5, 0.5),
					RenderTransform = new ScaleTransform(1, -1),
				});
				Grid.SetColumn(BottomGrid.Children[i], i);
			}

			for(byte i = 0; i < Settings.sizeY; ++i) {
				for(byte j = 0; j < Settings.sizeX; ++j) {
					this.MeatGrid.Children.Add(game.map[i, j].grid);
					Grid.SetRow(game.map[i, j].grid, i);
					Grid.SetColumn(game.map[i, j].grid, j);

					game.map[i, j].grid.PreviewMouseLeftButtonDown += (a, b) => {
						byte x = (byte) Grid.GetColumn(a as UIElement);
						byte y = (byte) Grid.GetRow(a as UIElement);
						startClickX = x;
						startClickY = y;
					};

					game.map[i, j].grid.PreviewMouseMove += (a, b) => {
						if(b.LeftButton == MouseButtonState.Pressed && game.isPlayerTurn) {
							byte x = (byte) Grid.GetColumn(a as UIElement);
							byte y = (byte) Grid.GetRow(a as UIElement);
							if((x != startClickX && y == startClickY) || (x == startClickX && y != startClickY)) {
								impaleLine.X1 = startClickX * gridSize.Width + gridHalfSize.Width;
								impaleLine.X2 = x * gridSize.Width + gridHalfSize.Width;
								impaleLine.Y1 = startClickY * gridSize.Height + gridHalfSize.Height;
								impaleLine.Y2 = y * gridSize.Height + gridHalfSize.Height;

								if(x < startClickX)
									impaleLine.X1 += gridSize.Width;
								else
								if(x > startClickX)
									impaleLine.X1 -= gridSize.Width;

								if(y < startClickY)
									impaleLine.Y1 += gridSize.Height;
								else if(y > startClickY)
									impaleLine.Y1 -= gridSize.Height;
							}
						}
					};

					game.map[i, j].grid.PreviewMouseLeftButtonUp += (a, b) => {
						impaleLine.X1 = impaleLine.X2 = impaleLine.Y1 = impaleLine.Y2 = -1000;

						byte x = (byte) Grid.GetColumn(a as UIElement);
						byte y = (byte) Grid.GetRow(a as UIElement);
						endClickX = x;
						endClickY = y;

						if(game.isPlayerTurn || (!game.isPlayerTurn && game.enemy == Game.Enemy.Player)) {
							if(startClickX == endClickX && startClickY == endClickY) {
								if(game.map[y, x].IsEmpty())
									game.PlaceMeat(startClickX, startClickY);
							}
							else {
								game.PlaceStick(startClickX, startClickY, endClickX, endClickY);
							}

							WriteScore();
							CheckGameOver();
						}

						if(!game.isPlayerTurn && game.enemy == Game.Enemy.Bot && !isBotThinking) {
							isBotThinking = true;
							System.Timers.Timer t = new System.Timers.Timer() {
								AutoReset = false,
								Interval = 500,
							};
							t.Elapsed += (c, d) => {
								System.Windows.Application.Current.Dispatcher.Invoke((Action) delegate {
									game.BotTurn();
									isBotThinking = false;
									WriteScore();
									CheckGameOver();
								});
							};
							t.Start();
						}
					};
				}
			}
		}

		private void Window_SizeChanged(object sender, SizeChangedEventArgs e) {
			if(e.HeightChanged)
				this.Width = Height * widthMod;
			else
				Height = Width / widthMod;
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			if(IsActive) {
				e.Cancel = true;
				WindowManager.ReopenWindow(this, MainWindow.MenuWindow);
			}
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			widthMod = this.Width / this.Height;
		}

		void WriteScore() {
			this.ScoreEnemy.Text = game.scoreEnemy.ToString();
			this.ScorePlayer.Text = game.scorePlayer.ToString();
		}

		void CheckGameOver() {
			var gameResult = game.IsGameOver();
			WriteScore();
			if(gameResult != Game.GameOverResult.None) {
				string winnerStr;
				if(gameResult == Game.GameOverResult.Draw)
					winnerStr = "Draw!";
				else {
					if(gameResult == Game.GameOverResult.WinPlayer)
						winnerStr = game.enemy == Game.Enemy.Bot ? "Player win!" : "Player 1 win!";
					else
						winnerStr = game.enemy == Game.Enemy.Bot ? "Bot win!" : "Player 2 win!";
				}

				if(MessageBox.Show(winnerStr + "\nDo you want to play again?", "Game over!", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
					this.StartGame(game.enemy);
				else
					WindowManager.ReopenWindow(this, MainWindow.MenuWindow);
			}
		}
	}
}

using System;
using System.Windows;
using ChessGame.WPF.Views;

namespace ChessGame.WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ShowMainMenu();
        }

        private void ShowMainMenu()
        {
            var menuView = new MenuView();
            menuView.StartGameRequested += OnStartGameRequested;
            MainContent.Content = menuView;
        }

        private void OnStartGameRequested(object? sender, StartGameEventArgs e)
        {
            var gameView = new GameView(e.GameMode, e.AiDifficulty);
            gameView.BackToMenuRequested += OnBackToMenuRequested;
            MainContent.Content = gameView;
        }

        private void OnBackToMenuRequested(object? sender, EventArgs e)
        {
            ShowMainMenu();
        }
    }
}
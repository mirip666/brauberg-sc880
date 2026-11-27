using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;

namespace SC880.Desktop
{
    public sealed class CalculatorWindow : Window
    {
        private readonly CalculatorView view;

        public CalculatorWindow()
        {
            Title = "BRAUBERG SC-880";
            CanResize = false;
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(Palette.FormBack);

            view = new CalculatorView();
            Content = view;

            try
            {
                Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://BraubergSC880/Assets/icon-256.png")));
            }
            catch
            {
                try
                {
                    Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://BraubergSC880/Assets/icon.ico")));
                }
                catch
                {
                }
            }

            KeyDown += async delegate(object sender, KeyEventArgs e)
            {
                await view.ProcessKeyAsync(e);
            };

            Opened += delegate
            {
                view.Focus();
            };
        }
    }
}

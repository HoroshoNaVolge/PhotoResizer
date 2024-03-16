using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using ExifLib;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Diagnostics.Metrics;
using System.Reflection;
using PhotoPreparation.ViewModels;
using System.Diagnostics.CodeAnalysis;

namespace PhotosPreparation
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
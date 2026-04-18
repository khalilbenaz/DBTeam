using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using DBTeam.Core.Infrastructure;
using DBTeam.Modules.Diagram.ViewModels;

namespace DBTeam.Modules.Diagram.Views;

public partial class DiagramView : UserControl
{
    private TableBoxViewModel? _draggedBox;
    private Point _dragStart;
    private double _startX, _startY;

    public DiagramView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.TryGet<DiagramViewModel>();
    }

    private void Box_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is TableBoxViewModel box)
        {
            _draggedBox = box;
            _dragStart = e.GetPosition(CanvasRoot);
            _startX = box.X; _startY = box.Y;
            fe.CaptureMouse();
            e.Handled = true;
        }
    }

    private void Box_MouseMove(object sender, MouseEventArgs e)
    {
        if (_draggedBox is null || e.LeftButton != MouseButtonState.Pressed) return;
        var p = e.GetPosition(CanvasRoot);
        _draggedBox.X = _startX + (p.X - _dragStart.X);
        _draggedBox.Y = _startY + (p.Y - _dragStart.Y);
    }

    private void Box_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.IsMouseCaptured) fe.ReleaseMouseCapture();
        _draggedBox = null;
    }

    private void Surface_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;
        var factor = e.Delta > 0 ? 1.1 : 1.0 / 1.1;
        var newScale = Math.Clamp(ZoomTransform.ScaleX * factor, 0.25, 3.0);
        ZoomTransform.ScaleX = newScale;
        ZoomTransform.ScaleY = newScale;
        e.Handled = true;
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        var s = Math.Min(3.0, ZoomTransform.ScaleX * 1.2);
        ZoomTransform.ScaleX = s; ZoomTransform.ScaleY = s;
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        var s = Math.Max(0.25, ZoomTransform.ScaleX / 1.2);
        ZoomTransform.ScaleX = s; ZoomTransform.ScaleY = s;
    }

    private void ZoomReset_Click(object sender, RoutedEventArgs e)
    {
        ZoomTransform.ScaleX = 1.0; ZoomTransform.ScaleY = 1.0;
    }

    private void ExportPng_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            FileName = $"diagram-{DateTime.Now:yyyyMMdd-HHmmss}.png",
            Filter = "PNG image (*.png)|*.png"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            int w = (int)CanvasRoot.ActualWidth;
            int h = (int)CanvasRoot.ActualHeight;
            if (w <= 0 || h <= 0) return;
            var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            CanvasRoot.Measure(new Size(w, h));
            CanvasRoot.Arrange(new Rect(new Size(w, h)));
            rtb.Render(CanvasRoot);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            using var fs = File.Create(dlg.FileName);
            encoder.Save(fs);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Export failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FastScreeny
{
    public partial class OverlaySelectionWindow : Window
    {
        private System.Windows.Point _startPoint;
        private System.Windows.Rect _selection;
        private TaskCompletionSource<Rect?>? _tcs;

        public OverlaySelectionWindow()
        {
            InitializeComponent();
            Cursor = System.Windows.Input.Cursors.Cross;
        }

        public Task<System.Windows.Rect?> SelectRegionAsync()
        {
            _tcs = new TaskCompletionSource<System.Windows.Rect?>();
            SelectionRect.Visibility = Visibility.Collapsed;
            _selection = System.Windows.Rect.Empty;
            KeyDown += OverlaySelectionWindow_KeyDown;
            MouseDown += OverlaySelectionWindow_MouseDown;
            MouseMove += OverlaySelectionWindow_MouseMove;
            MouseUp += OverlaySelectionWindow_MouseUp;
            return _tcs.Task;
        }

        private void OverlaySelectionWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CleanupHandlers();
                _tcs?.TrySetResult(null);
                Close();
            }
            else if (e.Key == Key.Enter)
            {
                CleanupHandlers();
                _tcs?.TrySetResult(_selection);
                Close();
            }
        }

        private void OverlaySelectionWindow_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(this);
            _selection = new System.Windows.Rect(_startPoint, _startPoint);
            SelectionRect.Visibility = Visibility.Visible;
        }

        private void OverlaySelectionWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var current = e.GetPosition(this);
                _selection = new System.Windows.Rect(_startPoint, current);
                Canvas.SetLeft(SelectionRect, _selection.X);
                Canvas.SetTop(SelectionRect, _selection.Y);
                SelectionRect.Width = _selection.Width;
                SelectionRect.Height = _selection.Height;
            }
        }

        private void OverlaySelectionWindow_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // no-op; confirm by Enter
        }

        private void CleanupHandlers()
        {
            KeyDown -= OverlaySelectionWindow_KeyDown;
            MouseDown -= OverlaySelectionWindow_MouseDown;
            MouseMove -= OverlaySelectionWindow_MouseMove;
            MouseUp -= OverlaySelectionWindow_MouseUp;
        }
    }
}



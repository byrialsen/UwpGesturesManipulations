using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace GesturesManipulations
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            FrameworkElement origin = sender as FrameworkElement;
            FrameworkElement parent = origin.Parent as FrameworkElement;

            var localCoords = e.Position;
            var relativeTransform = origin.TransformToVisual(parent);
            Point parentContainerCoords = relativeTransform.TransformPoint(localCoords);
            var center = parentContainerCoords;

            // translate/panning
            translateTransform.X += e.Delta.Translation.X;
            translateTransform.Y += e.Delta.Translation.Y;

            rotateTransform.CenterX = center.X;
            rotateTransform.CenterY = center.Y;
            rotateTransform.Angle += e.Delta.Rotation;

            scaleTransform.CenterX = center.X;
            scaleTransform.CenterY = center.Y;
            scaleTransform.ScaleX *= e.Delta.Scale;
            scaleTransform.ScaleY *= e.Delta.Scale;
        }

        private void AddHotspot(Color color, Point center)
        {
            var spot = new Windows.UI.Xaml.Shapes.Ellipse();
            spot.Width = spot.Height = 20;
            spot.Fill = new SolidColorBrush(color);
            Canvas.SetLeft(spot, center.X - 10.0);
            Canvas.SetTop(spot, center.Y - 10.0);

            //canvasHotspot.Children.Add(spot);
        }
    }
}

using System;
using System.Numerics;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace GesturesManipulations
{
    /// <summary>
    /// https://stackoverflow.com/questions/36727020/uwp-manipulation-with-rotation-scale-and-pan
    /// https://blogs.msdn.microsoft.com/wsdevsol/2014/06/10/constraining-manipulations/
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const double MAX_ZOOM = 5.0;
        private const double MIN_ZOOM = 0.2;
        private MatrixTransform previousTransform;
        private CompositeTransform deltaTransform;


        public MainPage()
        {
            this.InitializeComponent();
            (this.Content as FrameworkElement).DataContext = this;

            InitializeAndReset();
        }

        void Viewbox_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // store existing transformation and to prepare new
            Point center = StoreTransformationsAndGetCenter(e.Position);

            deltaTransform.CenterX = center.X;
            deltaTransform.CenterY = center.Y;

            // rotation
            deltaTransform.Rotation = e.Delta.Rotation;

            // scale
            deltaTransform.ScaleX = e.Delta.Scale;
            deltaTransform.ScaleY = e.Delta.Scale;
            ConstrainScale();

            // pan
            deltaTransform.TranslateX = e.Delta.Translation.X;
            deltaTransform.TranslateY = e.Delta.Translation.Y;
            ConstrainPan();
        }

        private void ConstrainScale()
        {
            if (transformGroup.Value.GetScale() > MAX_ZOOM || transformGroup.Value.GetScale() < MIN_ZOOM)
            {
                deltaTransform.ScaleX = deltaTransform.ScaleY = 1.0;
            }
        }

        private void ConstrainPan()
        {
            // do nothing yet
        }

        /// <summary>
        /// Reset all transformation
        /// </summary>
        private void InitializeAndReset()
        {
            transformGroup.Children.Clear();
            previousTransform = new MatrixTransform() { Matrix = Matrix.Identity };
            deltaTransform = new CompositeTransform();
            transformGroup.Children.Add(previousTransform);
            transformGroup.Children.Add(deltaTransform);
        }

        /// <summary>
        /// Handle double tab using mouse/touch/touchpad
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // store existing transformation and to prepare new
            Point posOnElement = e.GetPosition(ManipulationElement);
            Point center = StoreTransformationsAndGetCenter(posOnElement);

            // handle scale
            deltaTransform.CenterX = center.X;
            deltaTransform.CenterY = center.Y;

            if (deltaTransform.ScaleX < 1.1)
            {
                // zoom in
                deltaTransform.ScaleX = deltaTransform.ScaleY = 3.0;
            }
            else
            {
                // reset zoom
                deltaTransform.ScaleX = deltaTransform.ScaleY = (1 / previousTransform.Matrix.GetScale());
            }
        }

        /// <summary>
        /// Store existing transformation in matrix 
        /// and return current center position in respect to this
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private Point StoreTransformationsAndGetCenter(Point pos)
        {
            // store transformation into matrix
            previousTransform.Matrix = transformGroup.Value;

            // return center in respect to previous transformations
            return previousTransform.TransformPoint(pos);
        }

        private void ManipulationElement_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            /*
            //System.Diagnostics.Debug.WriteLine(e.Cumulative.Scale);
            double newRotation =  previousTransform.Matrix.GetRotation() + deltaTransform.Rotation;
            
            if (newRotation > -45.0 && newRotation <= 45.0)
            {
                // 0 degress
                deltaTransform.Rotation = 0.0;
            }
            else if (newRotation > 45.0 && newRotation <= 135.0)
            {
                // 90 degress
                deltaTransform.Rotation = 90.0;
            }
            else if (newRotation > -135.0 && newRotation <= -45.0)
            {
                // 270 degress
                deltaTransform.Rotation = -90;
            }
            else
            {
                // 180 degress
                deltaTransform.Rotation = 180.0;
            }
            */
        }

        /// <summary>
        /// Handle zoom by mouse wheel and touchpad
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            // get keystates
            bool shift = (e.KeyModifiers & VirtualKeyModifiers.Shift) == VirtualKeyModifiers.Shift;
            bool ctrl = (e.KeyModifiers & VirtualKeyModifiers.Control) == VirtualKeyModifiers.Control;

            if (ctrl)
            {
                // handle rotation
                if (shift)
                {
                    #region rotation   
                    /*
                    // store earlier manipulations
                    previousTransform.Matrix = transformGroup.Value;
                    System.Diagnostics.Debug.WriteLine($"Offset ({previousTransform.Matrix.OffsetX},{previousTransform.Matrix.OffsetY})");

                    // init
                    var delta = e.GetCurrentPoint(ManipulationElement).Properties.MouseWheelDelta;

                    // init center
                    var center = previousTransform.TransformPoint(e.GetCurrentPoint(ManipulationElement).Position);
                    deltaTransform.CenterX = center.X;
                    deltaTransform.CenterY = center.Y;

                    //var delta = PointerPoint.GetCurrentPoint(e.Pointer.PointerId).Properties.MouseWheelDelta;
                    //// With my mouse, delta is a multiple of 30.
                    //deltaTransform.CenterX = PointerPoint.GetCurrentPoint(e.Pointer.PointerId).Position.X;
                    //deltaTransform.CenterY = PointerPoint.GetCurrentPoint(e.Pointer.PointerId).Position.Y;

                    // rotation
                    if (delta > 0)
                    {
                        deltaTransform.Rotation = -90.0;
                        System.Diagnostics.Debug.WriteLine($"delta > 0 -> {deltaTransform.Rotation}");
                    }
                    else if (delta < 0)
                    {
                        deltaTransform.Rotation = 90.0;
                        System.Diagnostics.Debug.WriteLine($"dekta < 0 -> {deltaTransform.Rotation}");
                    }
                    */
                    #endregion
                }
                // handle scale
                else
                {
                    // get pointer pos
                    PointerPoint pointerPoint = e.GetCurrentPoint(this.ManipulationElement);

                    // handle zoom/scale
                    double deltaScroll = pointerPoint.Properties.MouseWheelDelta * -1;
                    deltaScroll = (deltaScroll > 0) ? 0.90 : 1.1;
                    double? newScale = ZoomWithContraints(transformGroup.Value, deltaScroll); // deltaTransform.ScaleX * deltaScroll;

                    if (newScale != null)
                    {
                        // store earlier manipulations
                        previousTransform.Matrix = transformGroup.Value;

                        // init center

                        var center = previousTransform.TransformPoint(pointerPoint.Position);

                        deltaTransform.CenterX = center.X;
                        deltaTransform.CenterY = center.Y;
                        deltaTransform.ScaleX = deltaTransform.ScaleY = newScale.Value;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("");
                    }
                }
            }
        }

        private double? ZoomWithContraints(Matrix matrix, double deltaZoom)
        {
            // calc new scale
            double newScale = matrix.GetScale() * deltaZoom;

            // ensure newScale is between constraints
            double result = (newScale <= MAX_ZOOM && newScale >= MIN_ZOOM) ? deltaZoom : 1.0;

            if (result == 1.0)
                return null;
            else
                return newScale;

        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            InitializeAndReset();
        }
    }
}

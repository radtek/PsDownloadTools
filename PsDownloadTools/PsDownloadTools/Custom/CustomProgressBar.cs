using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PsDownloadTools.Custom
{
    class ShapeProgressIndicator : Shape
    {
        public static readonly DependencyProperty DependencyMaskRatio = DependencyProperty.Register(nameof(MaskRatio), typeof(Double), typeof(ShapeProgressIndicator), new FrameworkPropertyMetadata(0D, FrameworkPropertyMetadataOptions.AffectsRender));
        private Double height = 0D;
        private Double width = 0D;
        private Rect rect1 = new Rect(0D, 0D, 0D, 0D);
        private Rect rect2 = new Rect(0D, 0D, 0D, 0D);

        public Double MaskRatio
        {
            get { return (Double)GetValue(DependencyMaskRatio); }
            set { SetValue(DependencyMaskRatio, value); InvalidateVisual(); }
        }

        protected override Size MeasureOverride(Size finalSize)
        {
            width = Math.Max(0D, finalSize.Width - StrokeThickness);
            height = Math.Max(0D, finalSize.Height - StrokeThickness);
            return base.MeasureOverride(finalSize);
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                rect1.X = StrokeThickness / 2;
                rect1.Y = StrokeThickness / 2;
                rect1.Width = width;
                rect1.Height = height;

                rect2.X = StrokeThickness / 2;
                rect2.Y = StrokeThickness / 2;
                rect2.Width = Math.Round(width * MaskRatio);
                rect2.Height = height;

                return new CombinedGeometry()
                {
                    GeometryCombineMode = GeometryCombineMode.Intersect,
                    Geometry1 = new RectangleGeometry() { RadiusX = height / 2, RadiusY = height / 2, Rect = rect1 },
                    Geometry2 = new RectangleGeometry() { Rect = rect2 }
                };
            }
        }
    }

    [TemplatePartAttribute(Name = "Shape_Indicator", Type = typeof(ShapeProgressIndicator))]
    [TemplatePartAttribute(Name = "Shape_Indicator_Dummy", Type = typeof(ShapeProgressIndicator))]
    [TemplatePartAttribute(Name = "Tb_Percentage", Type = typeof(TextBlock))]
    class CustomProgressBar : Control
    {
        public static readonly DependencyProperty DependencyProgressMax = DependencyProperty.Register("ProgressMax", typeof(Double), typeof(CustomProgressBar), new PropertyMetadata(100D, OnPropertyChanged));
        public static readonly DependencyProperty DependencyProgressValue = DependencyProperty.Register("ProgressValue", typeof(Double), typeof(CustomProgressBar), new PropertyMetadata(0D, OnPropertyChanged));
        public static readonly DependencyProperty DependencyIsIndeterminate = DependencyProperty.Register("IsIndeterminate", typeof(Boolean), typeof(CustomProgressBar), new PropertyMetadata(false, OnPropertyChanged));
        public static readonly DependencyProperty DependencyIsStop = DependencyProperty.Register("IsStop", typeof(Boolean), typeof(CustomProgressBar), new PropertyMetadata(true, OnPropertyChanged));
        private ShapeProgressIndicator shapeIndicator;
        private ShapeProgressIndicator shapeIndicatorDummy;
        private TextBlock tbPercentage;

        public Double ProgressMax
        {
            get { return (Double)GetValue(DependencyProgressMax); }
            set
            {
                SetValue(DependencyProgressMax, value);
                UpdateMeasure();
            }
        }

        public Double ProgressValue
        {
            get { return (Double)GetValue(DependencyProgressValue); }
            set
            {
                SetValue(DependencyProgressValue, value);
                UpdateMeasure();
            }
        }

        public Boolean IsIndeterminate
        {
            get { return (Boolean)GetValue(DependencyIsIndeterminate); }
            set
            {
                SetValue(DependencyIsIndeterminate, value);
                UpdateMeasure();
            }
        }

        public Boolean IsStop
        {
            get { return (Boolean)GetValue(DependencyIsStop); }
            set
            {
                SetValue(DependencyIsStop, value);
                UpdateMeasure();
            }
        }

        private static void OnPropertyChanged(DependencyObject dependency, DependencyPropertyChangedEventArgs e)
        {
            (dependency as CustomProgressBar).UpdateMeasure();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            shapeIndicator = (ShapeProgressIndicator)GetTemplateChild("Shape_Indicator");
            shapeIndicatorDummy = (ShapeProgressIndicator)GetTemplateChild("Shape_Indicator_Dummy");
            tbPercentage = (TextBlock)GetTemplateChild("Tb_Percentage");
            VisualStateManager.GoToState(this, "Run", false);
            UpdateMeasure();
        }

        public void UpdateMeasure()
        {
            if (shapeIndicator != null && shapeIndicatorDummy != null)
            {
                Double ratio = ProgressValue / ProgressMax;
                shapeIndicator.MaskRatio = IsIndeterminate ? 1D : ratio;
                tbPercentage.Text = $"{(ratio * 100).ToString("0.00")} %";

                if (IsStop)
                {
                    shapeIndicatorDummy.MaskRatio = IsIndeterminate ? 1D : ratio;
                    VisualStateManager.GoToState(this, "Stop", false);
                    VisualStateManager.GoToState(this, "Hide", false);
                }
                else
                {
                    VisualStateManager.GoToState(this, "Show", false);
                    VisualStateManager.GoToState(this, "Start", false);
                }
            }
        }
    }
}

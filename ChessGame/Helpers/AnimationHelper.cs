using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace ChessGame.WPF.Helpers
{
    public static class AnimationHelper
    {
        public static void AnimateMove(FrameworkElement element, Point from, Point to,
            Duration duration, Action? onCompleted = null)
        {
            var translateTransform = new TranslateTransform();
            element.RenderTransform = translateTransform;

            var xAnimation = new DoubleAnimation
            {
                From = from.X,
                To = to.X,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            var yAnimation = new DoubleAnimation
            {
                From = from.Y,
                To = to.Y,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            if (onCompleted != null)
            {
                xAnimation.Completed += (s, e) => onCompleted();
            }

            translateTransform.BeginAnimation(TranslateTransform.XProperty, xAnimation);
            translateTransform.BeginAnimation(TranslateTransform.YProperty, yAnimation);
        }

        public static void PulseAnimation(FrameworkElement element)
        {
            var scaleTransform = new ScaleTransform(1, 1);
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            element.RenderTransform = scaleTransform;

            var animation = new DoubleAnimation
            {
                From = 1,
                To = 1.2,
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                AutoReverse = true,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }

        public static void FadeIn(FrameworkElement element, Duration duration)
        {
            var animation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            element.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        public static void FadeOut(FrameworkElement element, Duration duration, Action? onCompleted = null)
        {
            var animation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            if (onCompleted != null)
            {
                animation.Completed += (s, e) => onCompleted();
            }

            element.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        public static void HighlightSquare(FrameworkElement element, Color color)
        {
            var colorAnimation = new ColorAnimation
            {
                To = color,
                Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                AutoReverse = true,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            var brush = new SolidColorBrush();
            element.Effect = new DropShadowEffect
            {
                Color = color,
                BlurRadius = 20,
                ShadowDepth = 0
            };

            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }
    }
}
// (C) Copyright 2014 by Autodesk, Inc.
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted, 
// provided that the above copyright notice appears in all copies and 
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting 
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS. 
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC. 
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.

//- Written by Cyrille Fauvel, Autodesk Developer Network (ADN)
//- http://www.autodesk.com/joinadn
//- August 18th, 2014
//
using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Autodesk.AutoCAD.Windows {
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
	///     xmlns:MyNamespace="clr-namespace:Autodesk.AutoCAD.Windows"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
	///     xmlns:MyNamespace="clr-namespace:Autodesk.AutoCAD.Windows;assembly=SlideCtrl"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
	///     <MyNamespace:SlideCtrl/>
    ///
    /// </summary>
    public class SlideCtrl : Button {

		static SlideCtrl () {
			DefaultStyleKeyProperty.OverrideMetadata (typeof(SlideCtrl), new FrameworkPropertyMetadata (typeof(SlideCtrl))) ;
        }

		#region Properties
		private SlideObject _Slide { get; set; }

		/// <summary>Slide object displayed in the control.</summary>
		public SlideObject Slide { //get; set;
			get { return ((SlideObject)GetValue (SlideProperty)) ; }
			set {
				SetValue (SlideProperty, value) ;
				_Slide =value ;
				if ( _Slide != null ) {
					SetValue (SlideNameProperty, _Slide._Name) ;
					_Slide.Draw (GetSlidePart ()) ;
					_Slide.ApplyFitInTranforms (GetSlidePart (), null, RespectRatio) ;
				} else {
					GetSlidePart ().Children.Clear () ;
				}
			}
		}
		public static readonly DependencyProperty SlideProperty =
			DependencyProperty.Register (
				"Slide", typeof(SlideObject),
				typeof(SlideCtrl), new UIPropertyMetadata (null)
			) ;

		/// <summary>Slide file name displayed in the control.</summary>
		public string SlideFileName { //get; set;
			get { return ((string)GetValue (SlideNameProperty)) ; }
			set {
				SetValue (SlideNameProperty, value) ;
				if ( !string.IsNullOrEmpty (value) ) {
					_Slide =new SlideObject (value) ;
					_Slide.Draw (GetSlidePart ()) ;
					_Slide.ApplyFitInTranforms (GetSlidePart (), null, RespectRatio) ;
				} else {
					_Slide =null ;
					GetSlidePart ().Children.Clear () ;
				}
			}
		}
		public static readonly DependencyProperty SlideNameProperty =
			DependencyProperty.Register (
				"SlideName", typeof(string),
				typeof(SlideCtrl), new UIPropertyMetadata ("")
			) ;

		/// <summary>Should the scaling respect the width/height ratio slide definition or use all the space.</summary>
		public bool RespectRatio {
			get { return ((bool)GetValue (RespectRatioProperty)) ; }
			set { SetValue (RespectRatioProperty, value) ; }
		}
		public static readonly DependencyProperty RespectRatioProperty =
			DependencyProperty.Register (
				"RespectRatio", typeof(bool),
				typeof(SlideCtrl), new UIPropertyMetadata (true)
			) ;
		public static readonly RoutedEvent RespectRatioEvent =
			EventManager.RegisterRoutedEvent (
				"RespectRatio", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SlideCtrl)
			) ;
		public event RoutedEventHandler RespectRatioChanged {
			add { base.AddHandler (RespectRatioEvent, value) ; }
			remove { base.RemoveHandler (RespectRatioEvent, value) ; }
		}
		protected void FireRespectRatioChanged () {
			base.RaiseEvent (new RoutedEventArgs (RespectRatioEvent)) ;
		}

		#endregion

		#region Rendering
		private Panel GetSlidePart () {
			return ((Grid)this.Template.FindName ("PART_SLIDE", this)) ;
		}

		/// <summary>OnApplyTemplate overrides.</summary>
		public override void OnApplyTemplate () {
			base.OnApplyTemplate () ;
			_Slide =new SlideObject (SlideFileName) ;
			_Slide.Draw (GetSlidePart ()) ;
		}

		/*protected override void OnRender (DrawingContext drawingContext) {
			base.OnRender (drawingContext) ;
			//Rect rect =new Rect (0, 0, base.ActualWidth, base.ActualHeight) ;
			//drawingContext.DrawRectangle (Foreground, null, rect) ;
		}*/

		/// <summary>OnRenderSizeChanged overrides.</summary>
		protected override void OnRenderSizeChanged (SizeChangedInfo sizeInfo) {
			base.OnRenderSizeChanged (sizeInfo) ;
			if ( _Slide != null )
				_Slide.ApplyFitInTranforms (GetSlidePart (), null, RespectRatio) ;
		}

		#endregion

	}

}

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
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Autodesk.ADN.Slm {

	public class TileView : ViewBase {

		public static readonly DependencyProperty ItemContainerStyleProperty =ItemsControl.ItemContainerStyleProperty.AddOwner (typeof (TileView)) ;

		public Style ItemContainerStyle {
			get { return ((Style)GetValue (ItemContainerStyleProperty)) ; }
			set { SetValue (ItemContainerStyleProperty, value) ; }
		}

		public static readonly DependencyProperty ItemTemplateProperty =ItemsControl.ItemTemplateProperty.AddOwner (typeof (TileView)) ;

		public DataTemplate ItemTemplate {
			get { return ((DataTemplate)GetValue (ItemTemplateProperty)) ; }
			set { SetValue (ItemTemplateProperty, value) ; }
		}

		public static readonly DependencyProperty ItemWidthProperty =WrapPanel.ItemWidthProperty.AddOwner (typeof (TileView)) ;

		public double ItemWidth {
			get { return ((double)GetValue (ItemWidthProperty)) ; }
			set { SetValue (ItemWidthProperty, value) ; }
		}

		public static readonly DependencyProperty ItemHeightProperty =WrapPanel.ItemHeightProperty.AddOwner (typeof (TileView)) ;

		public double ItemHeight {
			get { return ((double)GetValue (ItemHeightProperty)) ; }
			set { SetValue (ItemHeightProperty, value) ; }
		}

		protected override object DefaultStyleKey {
			get { return (new ComponentResourceKey (GetType (), "myTileView")) ; }
		}

	}

}
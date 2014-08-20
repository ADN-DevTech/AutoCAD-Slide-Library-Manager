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
//- August 20th, 2014
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
using System.Windows.Shapes;
using System.Threading;
using System.Xml;
using System.Diagnostics;

namespace Autodesk.ADN.Slm {

	public partial class JobProgress : Window {
		protected Thickness _margins ;

		protected JobProgress () {
			InitializeComponent () ;
		}

		public JobProgress (Thickness margins) {
			InitializeComponent () ;
			_margins =margins ;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			if ( _margins.Right - _margins.Left < this.Width )
				this.Width =_margins.Right - _margins.Left ;
			this.Left =_margins.Left + (_margins.Right - _margins.Left - this.ActualWidth) / 2 ;
			this.Top =_margins.Top + (_margins.Bottom - _margins.Top - this.ActualHeight) / 2 ;
		}

	}

}

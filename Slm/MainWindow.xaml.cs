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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Windows.Threading;
using Autodesk.AutoCAD.Windows;
using System.Windows.Interop;

namespace Autodesk.ADN.Slm {

	public class SlmVignette {
		public string Name { get; set; }
		public string Type { get; set; }
		public BitmapImage Image { get; set; }

	}

	public class SlmProperties {

		#region Properties
		[Category("Library")]
		[DisplayName("Name")]
		[Description("Library name")]
		public string slbName { get; private set; }
		[Category("Library")]
		[DisplayName("File name")]
		[Description("Library file name")]
		public string slbFileName { get; private set; }
		[Category("Library")]
		[DisplayName("File size (byte)")]
		[Description("Library file size")]
		public long slbSize { get; private set; }
		[Category("Library")]
		[DisplayName("Nb Slide")]
		[Description("Nb slide in library")]
		public int slbNbSlide { get; private set; }
		[Category("Library")]
		[DisplayName("Version")]
		[Description("Slide library format version")]
		[DefaultValueAttribute("1.0")]
		public string slbVersion { get; private set; }

		[Category("Slide")]
		[DisplayName("Slide Name")]
		[Description("Slide Name")]
		public string sldName { get; private set; }
		[Category("Slide")]
		[DisplayName("File name (byte)")]
		[Description("Slide file name")]
		public string sldFileName { get; private set; }
		[Category("Slide")]
		[DisplayName("File Size (byte)")]
		[Description("Slide file size")]
		public long sldSize { get; private set; }
		[Category("Slide")]
		[DisplayName("Width")]
		[Description("Slide image width")]
		public int sldWidth { get; private set; }
		[Category("Slide")]
		[DisplayName("Height")]
		[Description("Slide image height")]
		public int sldHeight { get; private set; }
		[Category("Slide")]
		[DisplayName("Version")]
		[Description("Slide format version")]
		[DefaultValueAttribute("2.0")]
		public string sldVersion { get; private set; }

		#endregion

		#region Constructors
		public SlmProperties (SlideObject sld, SlideLibObject slb =null)
			: base ()
		{
			if ( slb != null ) {
				slbName =slb._Name ;
				slbFileName =slb._FileName ;
				slbSize =slb._FileLength ;
				slbNbSlide =slb._Slides.Count ;
				slbVersion ="1.0" ;
			}
			if ( sld != null ) {
				sldName =sld._Name ;
				sldFileName =sld._FileName ;
				sldSize =sld._FileLength ;
				sldWidth =(int)sld._Size.Width ;
				sldHeight =(int)sld._Size.Height ;
				sldVersion =sld._Version.ToString ("#.0#") ;
			}
		}

		protected SlmProperties () {
			slbName ="" ;
			slbFileName ="" ;
			slbSize =0 ;
			slbNbSlide =0 ;
			slbVersion ="1.0" ;
			sldName ="" ;
			sldFileName ="" ;
			sldSize =0 ;
			sldWidth =0 ;
			sldHeight =0 ;
			sldVersion ="2.0" ;
		}

		#endregion

	}

	public partial class MainWindow : Window {

		#region Properties
		private SlideLibObject _slideLib =new SlideLibObject () ;
		private bool _dirty =false ;
		private Point _mouseStart ;
		private Thread _waitThread ;
		private JobProgress _waitDialog ;
		private Thickness _margins ;
		#endregion

		#region Constructors
		public MainWindow () {
			InitializeComponent () ;
			Library.View =Library.FindResource ("slideView") as ViewBase ;

			//Image firstTime =new Image () ;
			//firstTime.BeginInit () ;
			//firstTime.Source =new BitmapImage (new Uri (
			//	  @"pack://application:,,,/" 
			//	+ Assembly.GetExecutingAssembly ().GetName ().Name 
			//	+ ";component" + "/Images/Slm-Help.png", UriKind.Absolute)) ;
			//firstTime.EndInit () ;
			//topElement.Children.Add (firstTime) ;
		}

		#endregion

		#region Events and Logic
		private void Library_SelectionChanged (object sender, SelectionChangedEventArgs e) {
			e.Handled =true ;
			if ( Library.SelectedItems.Count != 1 ) {
				preview.Slide =null ;
				propertyGrid.SelectedObject =new SlmProperties (null, _slideLib) ;
				return ;
			}

			SlmVignette item =Library.SelectedItem as SlmVignette ;
			SlideObject sld =_slideLib._Slides [item.Name] ;
			preview.Slide =sld ;
			propertyGrid.SelectedObject =new SlmProperties (sld, _slideLib) ;
		}

		private void RunWaitThread () {
			_waitDialog =new JobProgress (_margins) ;
			_waitDialog.ShowDialog () ;
		}

		private ObservableCollection<SlmVignette> MergeAndGenerateBadges (string [] files) {
			Point pt =Library.PointToScreen (new Point (0, 0)) ;
			_margins =new Thickness (pt.X, pt.Y, pt.X + Library.ActualWidth, pt.Y + Library.ActualHeight) ;

			_waitThread =new Thread (this.RunWaitThread) ;
			_waitThread.IsBackground =true ;
			_waitThread.SetApartmentState (ApartmentState.STA) ;
			_waitThread.Start () ;
			Thread.Sleep (0) ;

				ObservableCollection<SlmVignette> newItems =new ObservableCollection<SlmVignette> () ;
				ObservableCollection<SlmVignette> items =new ObservableCollection<SlmVignette> () ;
				if ( Library.ItemsSource != null )
					items =new ObservableCollection<SlmVignette> ((IEnumerable<SlmVignette>)Library.ItemsSource) ;
				foreach ( string fileName in files ) {
					if ( SlideLibObject.IsSlideLibrary (fileName) ) {
						SlideLibObject slb =new SlideLibObject (fileName) ;
						//_slideLib._Slides =_slideLib._Slides.Concat (slb._Slides).GroupBy (d => d.Key)
						//	.ToDictionary (d => d.Key, d => d.First ().Value) ;
						foreach ( KeyValuePair<string, SlideObject> slide in slb._Slides ) {
							string name =slide.Key ;
							if ( _slideLib._Slides.ContainsKey (slide.Key) ) {
								for ( int i =0 ;; i++ ) {
									string st =name + i ;
									if ( !_slideLib._Slides.ContainsKey (st) ) {
										name =st ;
										break ;
									}
								}
							}
							_slideLib._Slides.Add (name, slide.Value) ;

							SlmVignette badge =new SlmVignette () {
								Name =name,
								Type =slide.Value._Size.Width + "x" + slide.Value._Size.Height,
								Image =slide.Value.Export (70, 70)
							} ;
							items.Add (badge) ;
							newItems.Add (badge) ;
						}
						_dirty =true ;
					}
					if ( SlideObject.IsSlide (fileName) && !_slideLib._Slides.ContainsKey (System.IO.Path.GetFileNameWithoutExtension (fileName)) ) {
						SlideObject sld =new SlideObject (fileName) ;
						sld._Name =System.IO.Path.GetFileNameWithoutExtension (fileName) ;
						_slideLib._Slides.Add (sld._Name, sld) ;
						_dirty =true ;

						SlmVignette badge =new SlmVignette () {
							Name =sld._Name,
							Type =sld._Size.Width + "x" + sld._Size.Height,
							Image =sld.Export (70, 70)
						} ;
						items.Add (badge) ;
						newItems.Add (badge) ;
					}
					Thread.Sleep (0) ;
				}

				Library.ItemsSource =items ;
				Library.Items.Refresh () ;

			Thread.Sleep (200) ;
			while ( _waitDialog == null )
				Thread.Sleep (100) ;
			_waitDialog.Dispatcher.BeginInvoke (DispatcherPriority.Normal, (Action) (() => {
				_waitDialog.Close () ;
			})) ;
			if ( _waitThread.IsAlive )
				_waitThread.Abort () ;

			return (newItems) ;
		}

		private void GenerateBadges () {
			Point pt =Library.PointToScreen (new Point (0, 0)) ;
			_margins =new Thickness (pt.X, pt.Y, pt.X + Library.ActualWidth, pt.Y + Library.ActualHeight) ;

			_waitThread =new Thread (this.RunWaitThread) ;
			_waitThread.IsBackground =true ;
			_waitThread.SetApartmentState (ApartmentState.STA) ;
			_waitThread.Start () ;
			Thread.Sleep (0) ;

			ObservableCollection<SlmVignette> items =new ObservableCollection<SlmVignette> () ;
			foreach ( KeyValuePair<string, SlideObject> slide in _slideLib._Slides ) {
				items.Add (new SlmVignette () {
					Name =slide.Key,
					Type =slide.Value._Size.Width + "x" + slide.Value._Size.Height,
					Image =slide.Value.Export (70, 70)
				}) ;
				Thread.Sleep (0) ;
			}

			Library.ItemsSource =items ;
			Library.Items.Refresh () ;

			while ( _waitDialog == null )
				Thread.Sleep (10) ;
			_waitDialog.Dispatcher.BeginInvoke (DispatcherPriority.Normal, (Action) (() => {
				_waitDialog.Close () ;
			})) ;
			_waitThread.Abort () ;
		}

		private void Library_Drop (object sender, System.Windows.DragEventArgs e) {
			e.Handled =true ;
			string [] files =(string [])e.Data.GetData (System.Windows.DataFormats.FileDrop) ;
			ObservableCollection<SlmVignette> newItems =MergeAndGenerateBadges (files) ;
			//Library.SelectedItems.Clear () ;
			//Library.SelectedItems.Add (newItems) ;

			Library.ScrollIntoView (newItems [0]) ;
		}

		private void Library_MouseLeftButtonDown (object sender, MouseButtonEventArgs e) {
			_mouseStart =e.GetPosition (null) ;
		}

		private void Library_MouseMove (object sender, System.Windows.Input.MouseEventArgs e) {
			Point pos =e.GetPosition (null) ;
			Vector diff =this._mouseStart - pos ;

			if (   e.LeftButton == MouseButtonState.Pressed
				&& Math.Abs (diff.X) > SystemParameters.MinimumHorizontalDragDistance
				&& Math.Abs (diff.Y) > SystemParameters.MinimumVerticalDragDistance
			) {
				if ( Library.SelectedItems.Count == 0 )
					return ;

				string temp =System.IO.Path.GetTempPath () ;
				List<string> paths =new List<string> () ;
				foreach ( SlmVignette slide in Library.SelectedItems ) {
					string st =System.IO.Path.Combine (new string [] { temp, slide.Name + ".sld" }) ;
					paths.Add (st) ;
					_slideLib._Slides [slide.Name].SaveAs (st) ;
				}
				DragDrop.DoDragDrop (
					Library,
					new System.Windows.DataObject (System.Windows.DataFormats.FileDrop, paths.ToArray ()),
					System.Windows.DragDropEffects.Copy
				) ; 
			} 
		}

		private bool CheckLibraryFileName () {
			if ( string.IsNullOrEmpty (_slideLib._FileName) ) {
				Microsoft.Win32.SaveFileDialog dlg =new Microsoft.Win32.SaveFileDialog () ;
				dlg.FileName ="Document" ; // Default file name
				dlg.DefaultExt =".slb" ; // Default file extension
				dlg.Filter ="AutoCAD Slide Library (.slb)|*.slb" ; // Filter files by extension
				Nullable<bool> result =dlg.ShowDialog () ;
				if ( result == false )
					return (false) ;
				_slideLib._FileName =dlg.FileName ;
			}
			return (true) ;
		}

		private void NewLibrary_Click (object sender, RoutedEventArgs e) {
			e.Handled =true ;
			if ( _dirty ) {
				if ( System.Windows.MessageBox.Show ("Save Library first?", "Slm", MessageBoxButton.YesNo) == MessageBoxResult.Yes ) {
					if ( !CheckLibraryFileName () )
						return ;
					_slideLib.Save () ;
				}
			}
			// Clear / New library
			_slideLib =new SlideLibObject () ;
			Library.ItemsSource =new ObservableCollection<SlmVignette> () ;
			Library.Items.Refresh () ;
			_dirty =false ;

			preview.Slide =null ;
			propertyGrid.SelectedObject =new SlmProperties (null, _slideLib) ;
		}

		private void OpenLibrary_Click (object sender, RoutedEventArgs e) {
			e.Handled =true ;
			if ( _dirty ) {
				if ( System.Windows.MessageBox.Show ("Save Library first?", "Slm", MessageBoxButton.YesNo) == MessageBoxResult.Yes ) {
					if ( !CheckLibraryFileName () )
						return ;
					_slideLib.Save () ;
				}
			}
			// Clear / New library
			Microsoft.Win32.OpenFileDialog dlg =new Microsoft.Win32.OpenFileDialog () ;
			dlg.DefaultExt =".slb" ; // Default file extension
			dlg.Filter ="AutoCAD Slide Library|*.slb|AutoCAD Slide|*.sld" ; // Filter files by extension
			Nullable<bool> result =dlg.ShowDialog () ;
			if ( result == false )
				return ;

			_slideLib =new SlideLibObject (dlg.FileName) ;
			GenerateBadges () ;
			_dirty =false ;

			preview.Slide =null ;
			propertyGrid.SelectedObject =new SlmProperties (null, _slideLib) ;
		}

		private void SaveLibrary_Click (object sender, RoutedEventArgs e) {
			e.Handled =true ;
			if ( !CheckLibraryFileName () )
				return ;
			_slideLib.Save () ;
			_dirty =false ;
		}

		private void ExtractSlides_Click (object sender, RoutedEventArgs e) {
			e.Handled =true ;
			if ( Library.SelectedItems.Count == 0 )
				return ;

			var dialog =new System.Windows.Forms.FolderBrowserDialog () ;
			System.Windows.Forms.DialogResult result =dialog.ShowDialog () ;
			if ( result != System.Windows.Forms.DialogResult.OK )
				return ;

			string temp =dialog.SelectedPath ;
			foreach ( SlmVignette slide in Library.SelectedItems ) {
				string st =System.IO.Path.Combine (new string [] { temp, slide.Name + ".sld" }) ;
				_slideLib._Slides [slide.Name].SaveAs (st) ;
			}
		}

		private void ExportSlides_Click (object sender, RoutedEventArgs e) {
			e.Handled =true ;
			if ( Library.SelectedItems.Count == 0 )
				return ;

			var dialog =new System.Windows.Forms.FolderBrowserDialog () ;
			System.Windows.Forms.DialogResult result =dialog.ShowDialog () ;
			if ( result != System.Windows.Forms.DialogResult.OK )
				return ;

			string temp =dialog.SelectedPath ;
			foreach ( SlmVignette slide in Library.SelectedItems ) {
				string st =System.IO.Path.Combine (new string [] { temp, slide.Name + ".png" }) ;
				SlideObject sld =_slideLib._Slides [slide.Name] ;
				BitmapImage renderBitmap =sld.Export () ;

				PngBitmapEncoder encoder =new PngBitmapEncoder () ;
				encoder.Frames.Add (BitmapFrame.Create (renderBitmap)) ; // Push the rendered bitmap to it
				using ( FileStream outStream =new FileStream (st, FileMode.Create) ) { // Create a file stream for saving image
					encoder.Save (outStream) ;
				}
			}	
		}

		private void DeleteSlides_Click (object sender, RoutedEventArgs e) {
			e.Handled =true ;
			if ( Library.SelectedItems.Count == 0 )
				return ;
			ObservableCollection<SlmVignette> items =new ObservableCollection<SlmVignette> ((IEnumerable<SlmVignette>)Library.ItemsSource) ;
			foreach ( SlmVignette slide in Library.SelectedItems ) {
				_slideLib._Slides.Remove (slide.Name) ;
				items.Remove (slide) ;
			}
			Library.ItemsSource =items ;
			Library.Items.Refresh () ;
			_dirty =true ;

			preview.Slide =null ;
			propertyGrid.SelectedObject =new SlmProperties (null, _slideLib) ;
		}

		private void Help_Click (object sender, RoutedEventArgs e) {
			e.Handled =true ;
			System.Diagnostics.Process.Start (new System.Diagnostics.ProcessStartInfo ("https://github.com/ADN-DevTech/AutoCAD-Slide-Library-Manager")) ;
		}

		private void About_Click (object sender, RoutedEventArgs e) {
			e.Handled =true ;
			About dlg =new About () ;
			dlg.Owner =this ;
			dlg.Show () ;
		}

		private void Window_Closing (object sender, CancelEventArgs e) {
			if ( _dirty ) {
				if ( System.Windows.MessageBox.Show ("Save Library first?", "Slm", MessageBoxButton.YesNo) == MessageBoxResult.Yes ) {
					if ( !CheckLibraryFileName () )
						return ;
					_slideLib.Save () ;
				}
			}
		}

		#endregion

	}

}

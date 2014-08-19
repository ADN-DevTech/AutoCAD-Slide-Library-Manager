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
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;

namespace Autodesk.AutoCAD.Windows {

	/// <summary>Represents a Slide Library.</summary>
	public class SlideLibObject {
		/// <summary>Length of the Slide Library file header.</summary>
		protected const int _slbHeaderLength =68 ;
		/// <summary>Slide Library file header identifier string.</summary>
		protected const string _slbHeaderSignature =@"AutoCAD Slide Library 1.0" ;
		/// <summary>Maximum length of the Slide Library file header identifier string.</summary>
		protected const int _slbHeaderSignatureLength =32 ;
		/// <summary>Length of the Slide Library entry header (string + index).</summary>
		protected const int _slbHeaderSlideEntryLength =36 ;

		#region Properties
		/// <summary>Gets the Slide Library name.</summary>
		/// <value>The value of the Slide Library name.</value>
		public string _Name { get; internal set; }
		/// <summary>Gets the Slide Library entries.</summary>
		public Dictionary<string, SlideObject> _Slides { get; /*internal*/ set; }
		/// <summary>Gets the Slide Library filename.</summary>
		public string _FileName { get; set; }
		/// <summary>Gets the Slide Library file size on disk.</summary>
		public long _FileLength {
			get {
				long sz =0 ;
				foreach ( KeyValuePair<string, SlideObject> slide in _Slides )
					sz +=slide.Value._FileLength ;
				return (_slbHeaderLength + _Slides.Count * _slbHeaderSlideEntryLength + sz) ;
			}
		}

		#endregion

		#region Constructors
		/// <summary>Initializes a new empty instance of the <see cref="SlideLibObject"/> class.</summary>
		public SlideLibObject () {
			Clear () ;
		}

		/// <summary>Initializes a new empty instance of the <see cref="SlideLibObject"/> class.</summary>
		/// <param name="filename">Slide library file name.</param>
		public SlideLibObject (string filename) {
			Load (filename) ;
		}

		private void Clear () {
			_Name ="" ;
			_Slides =new Dictionary<string, SlideObject> () ;
		}

		#endregion

		#region IO
		/// <summary>Checks if the filename is a Slide Library.</summary>
		/// <param name="filename">Slide library file name.</param>
		public static bool IsSlideLibrary (string filename) {
			return (System.IO.Path.GetExtension (filename).ToLower () == /*NOXLATE*/".slb") ;
		}

		/// <summary>Reads a single single out of a Slide Library.</summary>
		/// <param name="slbFileName">Slide library file name.</param>
		/// <param name="slideName">Slide name to extract.</param>
		public static SlideObject LoadSlideFromLibrary (string slbFileName, string slideName) {
			SlideLibObject slb =new SlideLibObject () ;
			if ( !slb.Load (slbFileName) )
				return (null) ;
			return (slb._Slides.ContainsKey (slideName) ? slb._Slides [slideName] : null) ;
		}

		private bool ReadSlb (string filename) {
			if (   !SlideLibObject.IsSlideLibrary (filename)
				|| !File.Exists (filename)
			)
				return (false) ;

			FileInfo f =new FileInfo (filename) ;
			if ( f.Length < _slbHeaderLength )
				return (false) ;
			byte [] content =File.ReadAllBytes (filename) ;
			// Read Header
			// Check if it is really an AutoCAD slide file
			string st =System.Text.Encoding.Default.GetString (content, 0, _slbHeaderSignature.Length) ;
			if ( st != _slbHeaderSignature )
				return (false) ;
			// Load Slides
			List<int> indexes =new List<int> () ;
			for ( int start =_slbHeaderSignatureLength + _slbHeaderSlideEntryLength - 4 ;; start +=_slbHeaderSlideEntryLength ) {
				int pos =BitConverter.ToInt32 (content, start) ;
				if ( pos == 0 )
					break ;
				indexes.Add (pos) ;
			}
			indexes.Add ((int)f.Length) ;
			indexes.RemoveAt (0) ;

			for ( int start =_slbHeaderSignatureLength ; indexes.Count > 0 ; start +=_slbHeaderSlideEntryLength, indexes.RemoveAt (0) ) {
				st =System.Text.Encoding.Default.GetString (content, start, _slbHeaderSlideEntryLength - 4) ;
				st =st.Replace ("\0", "") ;
				int pos =BitConverter.ToInt32 (content, start + _slbHeaderSlideEntryLength - 4) ;
				int sldLength =indexes [0] - pos, i =1 ;
				while ( sldLength == 0 ) // In case a library has 2 entries for the same slide
					sldLength =indexes [i++] - pos ;
				byte [] sldContent =new byte [sldLength] ;
				Buffer.BlockCopy (content, pos, sldContent, 0, sldLength) ;
				_Slides.Add (st, new SlideObject (st, sldContent)) ;
			}

			_FileName =filename ;
			return (true) ;
		}

		/// <summary>Reads a Slide Library from disk.</summary>
		/// <param name="filename">Slide library file name.</param>
		public bool Load (string filename) {
			Clear () ;
			return (ReadSlb (filename)) ;
		}

		private bool WriteSlb (string filename) {
			if ( _Slides == null || _Slides.Count == 0 )
				return (false) ;
			byte [] file =new byte [_FileLength] ;
			System.Buffer.BlockCopy (ASCIIEncoding.ASCII.GetBytes (_slbHeaderSignature), 0, file, 0, _slbHeaderSignature.Length) ;
			int i =0, cIndex =(_Slides.Count + 1) * _slbHeaderSlideEntryLength + _slbHeaderSignatureLength ;
			foreach ( KeyValuePair<string, SlideObject> slide in _Slides ) {
				int hIndex =(i++) * _slbHeaderSlideEntryLength + _slbHeaderSignatureLength ;
				System.Buffer.BlockCopy (ASCIIEncoding.ASCII.GetBytes (slide.Key), 0, file, hIndex, slide.Key.Length) ;
				System.Buffer.BlockCopy (BitConverter.GetBytes (cIndex), 0, file, hIndex + _slbHeaderSignatureLength, 4) ;
				byte [] sld =slide.Value.Content () ;
				System.Buffer.BlockCopy (sld.ToArray (), 0, file, cIndex, (int)slide.Value._FileLength) ;
				cIndex +=(int)slide.Value._FileLength ;
			}
			File.Delete (filename) ;
			File.WriteAllBytes (filename, file) ;
			return (true) ;
		}

		/// <summary>Saves the Slide Library to disk with using the <see cref="_FileName"/> property value.</summary>
		public bool Save () {
			if ( _FileName == "" )
				return (false) ;
			return (WriteSlb (_FileName)) ;
		}

		/// <summary>Saves the Slide Library to disk.</summary>
		/// <param name="filename">Slide library file name to save to.</param>
		public bool SaveAs (string filename) {
			return (WriteSlb (filename)) ;
		}

		#endregion

	}

	/// <summary>Represents a Slide.</summary>
	public class SlideObject {
		/// <summary>Length of the Slide file header.</summary>
		protected const int _sldHeaderLength =33 ;
		/// <summary>Slide file header identifier string.</summary>
		protected const string _sldHeaderSignature =@"AutoCAD Slide" ;

		#region Properties
		/// <summary>Gets or sets the Slide name in a library (default is the filename for a slide).</summary>
		/// <value>The value of the Slide name.</value>
		public string _Name { get; set; }
		private byte [] _Slide =null ;
		/// <summary>Gets the Slide filename if the slide was read from file.</summary>
		/// <value>The filename of the Slide.</value>
		public string _FileName { get; internal set; }
		/// <summary>Gets the Slide file size on disk or size in library.</summary>
		/// <value>The size of the Slide.</value>
		public long _FileLength { get; internal set; }
		/// <summary>Gets the Slide format version.</summary>
		/// <value>The value of the Slide name.</value>
		public byte _Version { get; internal set; }
		private bool _bLowFirst =true ;
		/// <summary>Gets or sets the Slide name in a library (default is the filename for a slide).</summary>
		/// <value>The value of the Slide name.</value>
		public /*System.Drawing.*/Size _Size { get; internal set; }

		#endregion

		#region Constructors
		/// <summary>Initializes a new empty instance of the <see cref="SlideObject"/> class.</summary>
		public SlideObject () {
			Clear () ;
		}

		/// <summary>Initializes a new instance of the <see cref="SlideObject"/> class and reads the slide file content.</summary>
		/// <param name="filename">Slide file name.</param>
		public SlideObject (string filename) {
			Load (filename) ;
		}

		/// <summary>Initializes a new instance of the <see cref="SlideObject"/> class and reads its content from a byte array.</summary>
		/// <param name="name">Slide name (in library, not filename).</param>
		/// <param name="content">Slide definition (file content, or library slide definition).</param>
		public SlideObject (string name, byte [] content) {
			Load (name, content) ;
		}

		private void Clear () {
			_Name ="" ;
			_Slide =null ;
			_FileName ="" ;
			_FileLength =0 ;
			_Version =2 ;
			_bLowFirst =true ;
			_Size =Size.Empty ;
		}

		#endregion

		#region IO
		/// <summary>Checks if the filename is a Slide.</summary>
		/// <param name="filename">Slide file name.</param>
		public static bool IsSlide (string filename) {
			return (System.IO.Path.GetExtension (filename).ToLower () == /*NOXLATE*/".sld") ;
		}

		private bool DecodeSld () {
			ulong i ;
			//----- Check if it is really an AutoCAD slide file
			string st =System.Text.Encoding.Default.GetString (_Slide, 0, _sldHeaderSignature.Length) ;
			if ( st != _sldHeaderSignature )
				return (false) ;
			//----- Get version number, and byte order
			switch ( _Slide [18] ) {
				case 0x01: //----- Old Format
					//----- We have to test the low order byte
					i =Read2Bytes (_FileLength - 2, true) ;
					if ( i == 0xfc00 ) {
						_bLowFirst =true ;
					} else if ( i == 0x00fc ) {
						_bLowFirst =false ;
					} else {
						return (false) ;
					}
					_Version =1 ;
					break ;
				case 0x02: //----- Should be equal to 2, since r9
					_bLowFirst =(Read2Bytes (29, true) == 0x1234) ;
					_Version =2 ;
					break ;
				default:
					return (false) ;
			}
			_Size =new Size (Read2Bytes (19, _bLowFirst), Read2Bytes (21, _bLowFirst)) ;
			return (true) ;
		}

		private bool ReadSld (string filename) {
			if (   !SlideObject.IsSlide (filename)
				|| !File.Exists (filename)
			)
				return (false) ;

			FileInfo f =new FileInfo (filename) ;
			if ( (_FileLength =f.Length) < _sldHeaderLength )
				return (false) ;
			_Slide =File.ReadAllBytes (filename) ;
			//----- Check Slide
			bool ret =DecodeSld () ;
			if ( ret == false ) {
				_Slide =null ;
				_FileLength =0 ;
			} else {
				_FileName =filename ;
				_Name =System.IO.Path.GetFileNameWithoutExtension (filename) ;
			}
			return (ret) ;
		}

		private bool ReadSld (string name, byte [] content) {
			if ( (_FileLength =content.Count ()) < 33 ) // 32
				return (false) ;
			_Slide =new byte [_FileLength] ;
			Buffer.BlockCopy (content, 0 , _Slide, 0, (int)_FileLength) ;
			//----- Check Slide
			bool ret =DecodeSld () ;
			if ( ret == false ) {
				_Slide =null ;
				_FileLength =0 ;
			} else {
				_Name =name ;
			}
			return (ret) ;
		}

		/// <summary>Reads a Slide from disk.</summary>
		/// <param name="filename">Slide library file name.</param>
		public bool Load (string filename) {
			Clear () ;
			return (ReadSld (filename)) ;
		}

		/// <summary>Reads a Slide from a byte array.</summary>
		/// <param name="name">Slide name (in library, not filename).</param>
		/// <param name="content">Slide definition (file content, or library slide definition).</param>
		public bool Load (string name, byte [] content) {
			Clear () ;
			return (ReadSld (name, content)) ;
		}

		private bool WriteSld (string filename) {
			if ( _Slide == null )
				return (false) ;
			File.WriteAllBytes (filename, _Slide) ;
			return (true) ;
		}

		/// <summary>Saves the Slide to disk with using the <see cref="_FileName"/> property value.</summary>
		public bool Save () {
			if ( _FileName == "" )
				return (false) ;
			return (WriteSld (_FileName)) ;
		}

		/// <summary>Saves the Slide to disk.</summary>
		/// <param name="filename">Slide file name to save to.</param>
		public bool SaveAs (string filename) {
			return (WriteSld (filename)) ;
		}
		
		/// <summary>Gets the Slide content as byte array.</summary>
		public byte [] Content () {
			return ((byte [])_Slide.Clone ()) ;
		}

		#endregion

		#region Draw
		/// <summary>Draws the slide into a WPF panel based element.</summary>
		/// <param name="pdc">Panel based element to draw into.</param>
		/// <param name="bw">Unused for now.</param>
		public virtual void Draw (Panel pdc, bool bw =false) {
			if ( _Slide == null )
				return ;
			pdc.Children.Clear () ;
			Line line ;
			Brush brush =null ;
			Point pt1 =new Point (0, 0), pt2 =new Point (0, 0) ;

			long j, i =_Version == 0x01 ? 34 : 31 ;
			for ( ; i < _FileLength ; ) {
				//----- Read Field Start
				ushort val =Read2Bytes (i, _bLowFirst) ;
				switch ( HighByte (val) ) {
					case 0xff: //----- Color Change
						if ( !bw ) {
							if (   (j =LowByte (val)) > acadColor.Count ()
								|| (   ~(acadColor [0].R) == acadColor [j].R
									&& ~(acadColor [0].G) == acadColor [j].G
									&& ~(acadColor [0].B) == acadColor [j].B)
							)
								j =0 ;
							brush =new SolidColorBrush (acadColor [j]) ;
						} else {
							SolidColorBrush bck =(SolidColorBrush)pdc.Background ;
							brush =new SolidColorBrush (Color.FromRgb ((byte)(bck.Color.R ^ 0xff), (byte)(bck.Color.G ^ 0xff), (byte)(bck.Color.B ^ 0xff))) ;
						}
						i +=2 ;
						break ;
					case 0xfe: //----- Common Endpoint Vector
						pt2 =pt1 ;
						pt1.X +=(sbyte)LowByte (val) ;
						pt1.Y +=(sbyte)_Slide [i + 2] ;
						i +=3 ;
						//----- Draw
						line =new Line () ;
						line.Stroke =brush ;
						line.X1 =pt2.X ; line.Y1 =pt2.Y ;
						line.X2 =pt1.X ; line.Y2 =pt1.Y ;
						line.StrokeThickness =1 ;
						//----- Just in case we draw a point or a too small line.
						line.SnapsToDevicePixels =true ;
						//line.SetValue (RenderOptions.EdgeModeProperty, EdgeMode.Aliased) ;
						pdc.Children.Add (line) ;
						break ;
					case 0xfd: //----- Solid Fill
						j =Read2Bytes (i + 2, _bLowFirst) ;
						i +=6 ;
						if ( j == 0 )
							break ;
						PointCollection pts =new PointCollection () ;
						for ( long k =0 ; k < j ; k++, i +=6 )
							pts.Add (new Point (ReadPoint (i + 2, _bLowFirst), ReadPoint (i + 4, _bLowFirst))) ;
						//----- Draw
						Polygon polygon =new Polygon () ;
						polygon.Points =pts ;
						polygon.Fill =brush ;
						polygon.Stroke =brush ;
						polygon.StrokeThickness =1 ;
						break ;
					case 0xfc: //----- End of File
						i +=2 ;
						return ;
					case 0xfb: //----- Offset Vector
						pt2 =pt1 ;
						pt1.X +=(sbyte)LowByte (val) ;
						pt1.Y +=(sbyte)(_Slide [i + 2]) ;
						pt2.X +=(sbyte)(_Slide [i + 3]) ;
						pt2.Y +=(sbyte)(_Slide [i + 4]) ;
						i +=5 ;
						//----- Draw
						line =new Line () ;
						line.Stroke =brush ;
						line.X1 =pt2.X ; line.Y1 =pt2.Y ;
						line.X2 =pt1.X ; line.Y2 =pt1.Y ;
						line.StrokeThickness =1 ;
						//----- Just in case we draw a point or a too small line.
						line.SnapsToDevicePixels =true ;
						//line.SetValue (RenderOptions.EdgeModeProperty, EdgeMode.Aliased) ;
						pdc.Children.Add (line) ;
						break ;
					default:
						if ( HighByte (val) > 0x7f ) //----- Undefined
							return ;
						//----- Vector
						pt1.X =val ;
						pt1.Y =ReadPoint (i + 2, _bLowFirst) ;
						pt2.X =ReadPoint (i + 4, _bLowFirst) ;
						pt2.Y =ReadPoint (i + 6, _bLowFirst) ;
						i +=8 ;
						//----- Draw
						line =new Line () ;
						line.Stroke =brush ;
						line.X1 =pt2.X ; line.Y1 =pt2.Y ;
						line.X2 =pt1.X ; line.Y2 =pt1.Y ;
						line.StrokeThickness =1 ;
						//----- Just in case we draw a point or a too small line.
						line.SnapsToDevicePixels =true ;
						//line.SetValue (RenderOptions.EdgeModeProperty, EdgeMode.Aliased) ;
						pdc.Children.Add (line) ;
						break ;
				}
			}
		}
		
		//public virtual void Draw (Bitmap pdc, bool bw =false) {
		//	if ( _Slide == null )
		//		return ;
		//	//pdc.MakeTransparent () ;
		//	Graphics graphics =Graphics.FromImage (pdc) ;
		//	Pen brush =null ;
		//	Point pt1 =new Point (0, 0), pt2 =new Point (0, 0) ;

		//	long j, i =_Version == 0x01 ? 34 : 31 ;
		//	for ( ; i < _Length ; ) {
		//		//----- Read Field Start
		//		ushort val =Read2Bytes (i, _bLowFirst) ;
		//		switch ( HighByte (val) ) {
		//			case 0xff: //----- Color Change
		//				if ( !bw ) {
		//					if (   (j =LowByte (val)) > acadColor.Count ()
		//						|| (   ~(acadColor [0].R) == acadColor [j].R
		//							&& ~(acadColor [0].G) == acadColor [j].G
		//							&& ~(acadColor [0].B) == acadColor [j].B)
		//					)
		//						j =0 ;
		//					brush =new Pen (acadColor [j], 1) ;
		//				} else {
		//					//SolidColorBrush bck =(SolidColorBrush)pdc.Background ;
		//					//brush =new SolidColorBrush (Color.FromRgb ((byte)(bck.Color.R ^ 0xff), (byte)(bck.Color.G ^ 0xff), (byte)(bck.Color.B ^ 0xff))) ;
		//				}
		//				i +=2 ;
		//				break ;
		//			case 0xfe: //----- Common Endpoint Vector
		//				pt2 =pt1 ;
		//				pt1.X +=(sbyte)LowByte (val) ;
		//				pt1.Y +=(sbyte)_Slide [i + 2] ;
		//				i +=3 ;
		//				//----- Draw
		//				graphics.DrawLine (brush, pt1, pt2) ;
		//				break ;
		//			case 0xfd: //----- Solid Fill
		//				j =Read2Bytes (i + 2, _bLowFirst) ;
		//				i +=6 ;
		//				if ( j == 0 )
		//					break ;
		//				Point [] pts =new Point [j] ;
		//				for ( long k =0 ; k < j ; k++, i +=6 )
		//					pts [k] =new Point (ReadPoint (i + 2, _bLowFirst), ReadPoint (i + 4, _bLowFirst)) ;
		//				//----- Draw
		//				graphics.DrawPolygon (brush, pts) ;
		//				break ;
		//			case 0xfc: //----- End of File
		//				i +=2 ;
		//				return ;
		//			case 0xfb: //----- Offset Vector
		//				pt2 =pt1 ;
		//				pt1.X +=(sbyte)LowByte (val) ;
		//				pt1.Y +=(sbyte)(_Slide [i + 2]) ;
		//				pt2.X +=(sbyte)(_Slide [i + 3]) ;
		//				pt2.Y +=(sbyte)(_Slide [i + 4]) ;
		//				i +=5 ;
		//				//----- Draw
		//				graphics.DrawLine (brush, pt1, pt2) ;
		//				break ;
		//			default:
		//				if ( HighByte (val) > 0x7f ) //----- Undefined
		//					return ;
		//				//----- Vector
		//				pt1.X =val ;
		//				pt1.Y =ReadPoint (i + 2, _bLowFirst) ;
		//				pt2.X =ReadPoint (i + 4, _bLowFirst) ;
		//				pt2.Y =ReadPoint (i + 6, _bLowFirst) ;
		//				i +=8 ;
		//				//----- Draw
		//				graphics.DrawLine (brush, pt1, pt2) ;
		//				break ;
		//		}
		//	}
		//}
		
		#endregion

		#region Image Convertion
		/// <summary>Exports Slide to an image using a Canvas definition.</summary>
		/// <param name="surface">Canvas to draw into.</param>
		public BitmapImage Export (Canvas surface) {
			// Save current canvas transform
			Transform lyTransform =surface.LayoutTransform ;
			// Reset current transform (in case it is scaled or rotated)
			surface.LayoutTransform =null ;

			// Get the size of canvas
			Size size =new Size (surface.Width, surface.Height) ;
			// Measure and arrange the surface
			surface.Measure (size) ;
			surface.Arrange (new Rect (size)) ;

			// Create a render bitmap and push the surface to it
			RenderTargetBitmap renderBitmap =
				new RenderTargetBitmap (
					(int)size.Width,
					(int)size.Height,
					96d, 96d,
					PixelFormats.Pbgra32) ;
			renderBitmap.Render (surface) ;

			// Restore previously saved layout
			surface.LayoutTransform =lyTransform ;

			// Use png encoder for our data
			PngBitmapEncoder encoder =new PngBitmapEncoder () ;
			//// Push the rendered bitmap to it
			//encoder.Frames.Add (BitmapFrame.Create (renderBitmap)) ;
			//BitmapImage bitmapImage =new BitmapImage () ;
			//using ( var stream = new MemoryStream () ) {
			//	encoder.Save (stream) ;
			//	stream.Seek (0, SeekOrigin.Begin) ;
			//	bitmapImage.BeginInit () ;
			//	bitmapImage.CacheOption =BitmapCacheOption.OnLoad ;
			//	bitmapImage.StreamSource =stream ;
			//	bitmapImage.EndInit () ;
			//}

			var tb =new TransformedBitmap () ;
			tb.BeginInit () ;
			tb.Source =renderBitmap ;
			var transform =new ScaleTransform (1, -1, 0, 0) ;
			tb.Transform =transform ;
			tb.EndInit () ;

			BitmapImage bitmapImage =new BitmapImage () ;
			encoder =new PngBitmapEncoder () ;
			encoder.Frames.Add (BitmapFrame.Create (tb)) ;
			MemoryStream memoryStream =new MemoryStream () ;
			encoder.Save (memoryStream) ;
			bitmapImage.BeginInit () ;
			bitmapImage.StreamSource =new MemoryStream (memoryStream.ToArray ()) ;
			bitmapImage.EndInit () ;
			memoryStream.Close () ;

			return (bitmapImage) ;
		}

		/// <summary>Exports Slide to an image.</summary>
		/// <param name="width">Defines the Width for the image. If 0 (or negative), will use the Slide <see cref="_Size"/>.Width.</param>
		/// <param name="height">Defines the Height for the image. If 0 (or negative), will use the Slide <see cref="_Size"/>.Height.</param>
		public BitmapImage Export (double width =0, double height =0) {
			Canvas canvas =new Canvas () ;
			canvas.Width =(width <= 0 ? _Size.Width : width) ;
			canvas.Height =(height <= 0 ? _Size.Height : height) ;
			Draw (canvas) ;
			Transform transforms =FitIn (canvas, true) ;
			((transforms as TransformGroup).Children [0] as ScaleTransform).ScaleY *=-1 ;
			((transforms as TransformGroup).Children [1] as TranslateTransform).Y *=-1 ;
			ApplyFitInTranforms (canvas, transforms, true) ;
			return (Export (canvas)) ;
		}

		#endregion

		#region Canvas Transforms
		/// <summary>Builds Transforms to display the slide in a based Panel.</summary>
		/// <param name="pdc">Canvas to use to calculate transforms.</param>
		/// <param name="respectRatio">Should the scaling respect the width/height ratio slide definition or use all the space.</param>
		public virtual Transform FitIn (Panel pdc, bool respectRatio =true) {
			TransformGroup transforms =new TransformGroup () ;

			// Scaling
			double xs =(pdc.ActualWidth == double.NaN || pdc.ActualWidth == 0 ? pdc.Width : pdc.ActualWidth) / _Size.Width ;
			double ys =(pdc.ActualHeight == double.NaN || pdc.ActualHeight == 0 ? pdc.Height : pdc.ActualHeight) / _Size.Height ;
			if ( respectRatio )
				xs =ys =Math.Min (xs, ys) ;
			ScaleTransform scale =new ScaleTransform (xs, -ys) ;
			transforms.Children.Add (scale) ;
			
			// Center
			double x =((pdc.ActualWidth == double.NaN || pdc.ActualWidth == 0 ? pdc.Width : pdc.ActualWidth) - _Size.Width * xs) / 2 ;
			double y =((pdc.ActualHeight == double.NaN || pdc.ActualHeight == 0 ? pdc.Height : pdc.ActualHeight) - _Size.Height * ys) / 2 ;
			TranslateTransform translate =new TranslateTransform (x, -y) ;
			transforms.Children.Add (translate) ;
			
			return (transforms) ;
		}

		/// <summary>Applies scaling to the based Panel.</summary>
		/// <param name="pdc">Canvas to to apply scaling to.</param>
		/// <param name="scale">Scale Transform matrix.</param>
		public void ScaleElements (Panel pdc, ScaleTransform scale) {
			double x =1 / Math.Max (scale.ScaleX, scale.ScaleY) ;
			foreach ( Shape shape in pdc.Children ) {
				shape.LayoutTransform =scale ;
				shape.StrokeThickness =x ;
			}
		}

		/// <summary>Applies a transform to the elements contained into a based Panel.</summary>
		/// <param name="pdc">Canvas to to apply scaling to.</param>
		/// <param name="transform">Transform matrix to apply.</param>
		public void RenderTransform (Panel pdc, Transform transform) {
			foreach ( Shape shape in pdc.Children )
				shape.RenderTransform =transform ;
		}

		/// <summary>Calculates and Applies transforms to a based Panel and elements contained into a based Panel.</summary>
		/// <param name="pdc">Canvas to to apply scaling to.</param>
		/// <param name="transforms">Transform matrixes to apply. If null will call <see cref="FitIn"/>.</param>
		/// <param name="respectRatio">Should the scaling respect the width/height ratio slide definition or use all the space.</param>
		public Transform ApplyFitInTranforms (Panel pdc, Transform transforms, bool respectRatio =true) {
			if ( transforms == null )
				transforms =FitIn (pdc, respectRatio) ;
			ScaleTransform scale =(transforms as TransformGroup).Children [0] as ScaleTransform ;
			ScaleElements (pdc, scale) ;
			TranslateTransform translate =(transforms as TransformGroup).Children [1] as TranslateTransform ;
			RenderTransform (pdc, translate) ;
			return (transforms) ;
		}

		#endregion

		#region Utilities
		private ushort Read2Bytes (long index, bool b) {
			if ( b ) {
				ushort hi =(ushort)(_Slide [index + 1] << 8) ;
				ushort lo =(ushort)(_Slide [index]) ;
				return ((ushort)(lo + hi)) ;
			}
			ushort hi2 =(ushort)( _Slide[index] << 8) ;
			ushort lo2 =(ushort)(_Slide [index + 1]) ;
			return ((ushort)(lo2 + hi2)) ;
		}

		private ushort ReadPoint (long index, bool b) {
			ushort value =Read2Bytes (index, b) ;
			if ( value > 32767 ) //----- For a bug in MSLIDE which returns the negative value
				value =(ushort)(65535 - value + 1) ;
			return (value) ;
		}

		private byte HighByte (ushort val) {
			return ((byte)(val >> 8)) ;
		} 

		private byte LowByte (ushort val) {
			return ((byte)(val & 0xff)) ;
		}

		#endregion

		#region AutoCAD colors
		/// <summary>AutoCAD based colors definitions.</summary>
		public readonly Color[] acadColor =new Color[] {
			Color.FromRgb (255, 255, 255),	//----- 0 - ByBlock - White
			Color.FromRgb (255, 0, 0),		//----- 1 - Red 
			Color.FromRgb (255, 255, 0),	//----- 2 - Yellow
			Color.FromRgb (0, 255, 0),		//----- 3 - Green
			Color.FromRgb (0, 255, 255),	//----- 4 - Cyan
			Color.FromRgb (0, 0, 255),		//----- 5 - Blue
			Color.FromRgb (255, 0, 255),	//----- 6 - Magenta
			Color.FromRgb (255, 255, 255),	//----- 7 - White

			Color.FromRgb (128, 128, 128),	//----- 8 - Dark Gray
			Color.FromRgb (192, 192, 192),	//----- 9 - Light Gray
		
			Color.FromRgb (255, 0, 0),		//----- 10
			Color.FromRgb (255, 127, 127),	//----- 11
			Color.FromRgb (165, 0, 0),		//----- 12
			Color.FromRgb (165, 82, 82),	//----- 13
			Color.FromRgb (127, 0, 0),		//----- 14
			Color.FromRgb (127, 63, 63),	//----- 15
			Color.FromRgb (76, 0, 0),		//----- 16
			Color.FromRgb (76, 38, 38),		//----- 17
			Color.FromRgb (38, 0, 0),		//----- 18
			Color.FromRgb (38, 19, 19),		//----- 19
			Color.FromRgb (255, 63, 0),		//----- 20
			Color.FromRgb (255, 159, 127),	//----- 21
			Color.FromRgb (165, 41, 0),		//----- 22
			Color.FromRgb (165, 103, 82),	//----- 23
			Color.FromRgb (127, 31, 0),		//----- 24
			Color.FromRgb (127, 79, 63),	//----- 25
			Color.FromRgb (76, 19, 0),		//----- 26
			Color.FromRgb (76, 47, 38),		//----- 27
			Color.FromRgb (38, 9, 0),		//----- 28
			Color.FromRgb (38, 23, 19),		//----- 29
			Color.FromRgb (255, 127, 0),	//----- 30
			Color.FromRgb (255, 191, 127),	//----- 31
			Color.FromRgb (165, 82, 0),		//----- 32
			Color.FromRgb (165, 124, 82),	//----- 33
			Color.FromRgb (127, 63, 0),		//----- 34
			Color.FromRgb (127, 95, 63),	//----- 35
			Color.FromRgb (76, 38, 0),		//----- 36
			Color.FromRgb (76, 57, 38),		//----- 37
			Color.FromRgb (38, 19, 0),		//----- 38
			Color.FromRgb (38, 28, 19),		//----- 39
			Color.FromRgb (255, 191, 0),	//----- 40
			Color.FromRgb (255, 223, 127),	//----- 41
			Color.FromRgb (165, 124, 0),	//----- 42
			Color.FromRgb (165, 145, 82),	//----- 43
			Color.FromRgb (127, 95, 0),		//----- 44
			Color.FromRgb (127, 111, 63),	//----- 45
			Color.FromRgb (76, 57, 0),		//----- 46
			Color.FromRgb (76, 66, 38),		//----- 47
			Color.FromRgb (38, 28, 0),		//----- 48
			Color.FromRgb (38, 33, 19),		//----- 49
			Color.FromRgb (255, 255, 0),	//----- 50
			Color.FromRgb (255, 255, 127),	//----- 51
			Color.FromRgb (165, 165, 0),	//----- 52
			Color.FromRgb (165, 165, 82),	//----- 53
			Color.FromRgb (127, 127, 0),	//----- 54
			Color.FromRgb (127, 127, 63),	//----- 55
			Color.FromRgb (76, 76, 0),		//----- 56
			Color.FromRgb (76, 76, 38),		//----- 57
			Color.FromRgb (38, 38, 0),		//----- 58
			Color.FromRgb (38, 38, 19),		//----- 59
			Color.FromRgb (191, 255, 0),	//----- 60
			Color.FromRgb (223, 255, 127),	//----- 61
			Color.FromRgb (124, 165, 0),	//----- 62
			Color.FromRgb (145, 165, 82),	//----- 63
			Color.FromRgb (95, 127, 0),		//----- 64
			Color.FromRgb (111, 127, 63),	//----- 65
			Color.FromRgb (57, 76, 0),		//----- 66
			Color.FromRgb (66, 76, 38),		//----- 67
			Color.FromRgb (28, 38, 0),		//----- 68
			Color.FromRgb (33, 38, 19),		//----- 69
			Color.FromRgb (127, 255, 0),	//----- 70
			Color.FromRgb (191, 255, 127),	//----- 71
			Color.FromRgb (82, 165, 0),		//----- 72
			Color.FromRgb (124, 165, 82),	//----- 73
			Color.FromRgb (63, 127, 0),		//----- 74
			Color.FromRgb (95, 127, 63),	//----- 75
			Color.FromRgb (38, 76, 0),		//----- 76
			Color.FromRgb (57, 76, 38),		//----- 77
			Color.FromRgb (19, 38, 0),		//----- 78
			Color.FromRgb (28, 38, 19),		//----- 79
			Color.FromRgb (63, 255, 0),		//----- 80
			Color.FromRgb (159, 255, 127),	//----- 81
			Color.FromRgb (41, 165, 0),		//----- 82
			Color.FromRgb (103, 165, 82),	//----- 83
			Color.FromRgb (31, 127, 0),		//----- 84
			Color.FromRgb (79, 127, 63),	//----- 85
			Color.FromRgb (19, 76, 0),		//----- 86
			Color.FromRgb (47, 76, 38),		//----- 87
			Color.FromRgb (9, 38, 0),		//----- 88
			Color.FromRgb (23, 38, 19),		//----- 89
			Color.FromRgb (0, 255, 0),		//----- 90
			Color.FromRgb (127, 255, 127),	//----- 91
			Color.FromRgb (0, 165, 0),		//----- 92
			Color.FromRgb (82, 165, 82),	//----- 93
			Color.FromRgb (0, 127, 0),		//----- 94
			Color.FromRgb (63, 127, 63),	//----- 95
			Color.FromRgb (0, 76, 0),		//----- 96
			Color.FromRgb (38, 76, 38),		//----- 97
			Color.FromRgb (0, 38, 0),		//----- 98
			Color.FromRgb (19, 38, 19),		//----- 99
			Color.FromRgb (0, 255, 63),		//----- 100
			Color.FromRgb (127, 255, 159),	//----- 101
			Color.FromRgb (0, 165, 41),		//----- 102
			Color.FromRgb (82, 165, 103),	//----- 103
			Color.FromRgb (0, 127, 31),		//----- 104
			Color.FromRgb (63, 127, 79),	//----- 105
			Color.FromRgb (0, 76, 19),		//----- 106
			Color.FromRgb (38, 76, 47),		//----- 107
			Color.FromRgb (0, 38, 9),		//----- 108
			Color.FromRgb (19, 38, 23),		//----- 109
			Color.FromRgb (0, 255, 127),	//----- 110
			Color.FromRgb (127, 255, 191),	//----- 111
			Color.FromRgb (0, 165, 82),		//----- 112
			Color.FromRgb (82, 165, 124),	//----- 113
			Color.FromRgb (0, 127, 63),		//----- 114
			Color.FromRgb (63, 127, 95),	//----- 115
			Color.FromRgb (0, 76, 38),		//----- 116
			Color.FromRgb (38, 76, 57),		//----- 117
			Color.FromRgb (0, 38, 19),		//----- 118
			Color.FromRgb (19, 38, 28),		//----- 119
			Color.FromRgb (0, 255, 191),	//----- 120
			Color.FromRgb (127, 255, 223),	//----- 121
			Color.FromRgb (0, 165, 124),	//----- 122
			Color.FromRgb (82, 165, 145),	//----- 123
			Color.FromRgb (0, 127, 95),		//----- 124
			Color.FromRgb (63, 127, 111),	//----- 125
			Color.FromRgb (0, 76, 57),		//----- 126
			Color.FromRgb (38, 76, 66),		//----- 127
			Color.FromRgb (0, 38, 28),		//----- 128
			Color.FromRgb (19, 38, 33),		//----- 129
			Color.FromRgb (0, 255, 255),	//----- 130
			Color.FromRgb (127, 255, 255),	//----- 131
			Color.FromRgb (0, 165, 165),	//----- 132
			Color.FromRgb (82, 165, 165),	//----- 133
			Color.FromRgb (0, 127, 127),	//----- 134
			Color.FromRgb (63, 127, 127),	//----- 135
			Color.FromRgb (0, 76, 76),		//----- 136
			Color.FromRgb (38, 76, 76),		//----- 137
			Color.FromRgb (0, 38, 38),		//----- 138
			Color.FromRgb (19, 38, 38),		//----- 139
			Color.FromRgb (0, 191, 255),	//----- 140
			Color.FromRgb (127, 223, 255),	//----- 141
			Color.FromRgb (0, 124, 165),	//----- 142
			Color.FromRgb (82, 145, 165),	//----- 143
			Color.FromRgb (0, 95, 127),		//----- 144
			Color.FromRgb (63, 111, 127),	//----- 145
			Color.FromRgb (0, 57, 76),		//----- 146
			Color.FromRgb (38, 66, 76),		//----- 147
			Color.FromRgb (0, 28, 38),		//----- 148
			Color.FromRgb (19, 33, 38),		//----- 149
			Color.FromRgb (0, 127, 255),	//----- 150
			Color.FromRgb (127, 191, 255),	//----- 151
			Color.FromRgb (0, 82, 165),		//----- 152
			Color.FromRgb (82, 124, 165),	//----- 153
			Color.FromRgb (0, 63, 127),		//----- 154
			Color.FromRgb (63, 95, 127),	//----- 155
			Color.FromRgb (0, 38, 76),		//----- 156
			Color.FromRgb (38, 57, 76),		//----- 157
			Color.FromRgb (0, 19, 38),		//----- 158
			Color.FromRgb (19, 28, 38),		//----- 159
			Color.FromRgb (0, 63, 255),		//----- 160
			Color.FromRgb (127, 159, 255),	//----- 161
			Color.FromRgb (0, 41, 165),		//----- 162
			Color.FromRgb (82, 103, 165),	//----- 163
			Color.FromRgb (0, 31, 127),		//----- 164
			Color.FromRgb (63, 79, 127),	//----- 165
			Color.FromRgb (0, 19, 76),		//----- 166
			Color.FromRgb (38, 47, 76),		//----- 167
			Color.FromRgb (0, 9, 38),		//----- 168
			Color.FromRgb (19, 23, 38),		//----- 169
			Color.FromRgb (0, 0, 255),		//----- 170
			Color.FromRgb (127, 127, 255),	//----- 171
			Color.FromRgb (0, 0, 165),		//----- 172
			Color.FromRgb (82, 82, 165),	//----- 173
			Color.FromRgb (0, 0, 127),		//----- 174
			Color.FromRgb (63, 63, 127),	//----- 175
			Color.FromRgb (0, 0, 76),		//----- 176
			Color.FromRgb (38, 38, 76),		//----- 177
			Color.FromRgb (0, 0, 38),		//----- 178
			Color.FromRgb (19, 19, 38),		//----- 179
			Color.FromRgb (63, 0, 255),		//----- 180
			Color.FromRgb (159, 127, 255),	//----- 181
			Color.FromRgb (41, 0, 165),		//----- 182
			Color.FromRgb (103, 82, 165),	//----- 183
			Color.FromRgb (31, 0, 127),		//----- 184
			Color.FromRgb (79, 63, 127),	//----- 185
			Color.FromRgb (19, 0, 76),		//----- 186
			Color.FromRgb (47, 38, 76),		//----- 187
			Color.FromRgb (9, 0, 38),		//----- 188
			Color.FromRgb (23, 19, 38),		//----- 189
			Color.FromRgb (127, 0, 255),	//----- 190
			Color.FromRgb (191, 127, 255),	//----- 191
			Color.FromRgb (82, 0, 165),		//----- 192
			Color.FromRgb (124, 82, 165),	//----- 193
			Color.FromRgb (63, 0, 127),		//----- 194
			Color.FromRgb (95, 63, 127),	//----- 195
			Color.FromRgb (38, 0, 76),		//----- 196
			Color.FromRgb (57, 38, 76),		//----- 197
			Color.FromRgb (19, 0, 38),		//----- 198
			Color.FromRgb (28, 19, 38),		//----- 199
			Color.FromRgb (191, 0, 255),	//----- 200
			Color.FromRgb (223, 127, 255),	//----- 201
			Color.FromRgb (124, 0, 165),	//----- 202
			Color.FromRgb (145, 82, 165),	//----- 203
			Color.FromRgb (95, 0, 127),		//----- 204
			Color.FromRgb (111, 63, 127),	//----- 205
			Color.FromRgb (57, 0, 76),		//----- 206
			Color.FromRgb (66, 38, 76),		//----- 207
			Color.FromRgb (28, 0, 38),		//----- 208
			Color.FromRgb (33, 19, 38),		//----- 209
			Color.FromRgb (255, 0, 255),	//----- 210
			Color.FromRgb (255, 127, 255),	//----- 211
			Color.FromRgb (165, 0, 165),	//----- 212
			Color.FromRgb (165, 82, 165),	//----- 213
			Color.FromRgb (127, 0, 127),	//----- 214
			Color.FromRgb (127, 63, 127),	//----- 215
			Color.FromRgb (76, 0, 76),		//----- 216
			Color.FromRgb (76, 38, 76),		//----- 217
			Color.FromRgb (38, 0, 38),		//----- 218
			Color.FromRgb (38, 19, 38),		//----- 219
			Color.FromRgb (255, 0, 191),	//----- 220
			Color.FromRgb (255, 127, 223),	//----- 221
			Color.FromRgb (165, 0, 124),	//----- 222
			Color.FromRgb (165, 82, 145),	//----- 223
			Color.FromRgb (127, 0, 95),		//----- 224
			Color.FromRgb (127, 63, 111),	//----- 225
			Color.FromRgb (76, 0, 57),		//----- 226
			Color.FromRgb (76, 38, 66),		//----- 227
			Color.FromRgb (38, 0, 28),		//----- 228
			Color.FromRgb (38, 19, 33),		//----- 229
			Color.FromRgb (255, 0, 127),	//----- 230
			Color.FromRgb (255, 127, 191),	//----- 231
			Color.FromRgb (165, 0, 82),		//----- 232
			Color.FromRgb (165, 82, 124),	//----- 233
			Color.FromRgb (127, 0, 63),		//----- 234
			Color.FromRgb (127, 63, 95),	//----- 235
			Color.FromRgb (76, 0, 38),		//----- 236
			Color.FromRgb (76, 38, 57),		//----- 237
			Color.FromRgb (38, 0, 19),		//----- 238
			Color.FromRgb (38, 19, 28),		//----- 239
			Color.FromRgb (255, 0, 63),		//----- 240
			Color.FromRgb (255, 127, 159),	//----- 241
			Color.FromRgb (165, 0, 41),		//----- 242
			Color.FromRgb (165, 82, 103),	//----- 243
			Color.FromRgb (127, 0, 31),		//----- 244
			Color.FromRgb (127, 63, 79),	//----- 245
			Color.FromRgb (76, 0, 19),		//----- 246
			Color.FromRgb (76, 38, 47),		//----- 247
			Color.FromRgb (38, 0, 9),		//----- 248
			Color.FromRgb (38, 19, 23),		//----- 249

			Color.FromRgb (84, 84, 84),		//----- 250 - Gray Shades
			Color.FromRgb (118, 118, 118),	//----- 251
			Color.FromRgb (152, 152, 152),	//----- 252
			Color.FromRgb (186, 186, 186),	//----- 253
			Color.FromRgb (220, 220, 220),	//----- 254
			Color.FromRgb (255, 255, 255),	//----- 255
		
			Color.FromRgb (255, 255, 255)	//----- ByLayer - White
		} ;

		#endregion

	}

}

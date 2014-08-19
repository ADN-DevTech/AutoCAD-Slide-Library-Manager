
# Autodesk Slide Library Manager Application ( SLM )

Slm is a utility which helps you to create, edit, and manage AutoCAD slide libraries. 
It can read any version of AutoCAD slides, or slide libraries. 
It can also read any slide format (platform dependent such as UNIX, MAC, INTEL), and convert them into PNG transparent images.


## The WPF Slide Control

The ‘Slide' control is a WPF control which displays slides under Windows platforms.
It can be used by any other WPF application. It exposes three objects which are:

  * SlideObject - which gives you access to 'slides'. I.e. SLD 
  * SlideLibObject - which gives you access to Slide Libraries. I.e. SLB
  * SlideCtrl - the control itself

  
### Install using NuGet:
  1. Create a Wpf Application 
  2. Project -> Manage NuGet Packages...
  3. Search and Install 'AutoCAD Slide Control' package


### SlideObject

This class gives you access to 'slide' object.

  1. Add  the following line to your code<br />
      using Autodesk.AutoCAD.Windows;
  2. You can now use the SlideObject class.

Example
```  c#
	using Autodesk.AutoCAD.Windows;

	SlideObject sld = new SlideObject(@"MySlide.sld");
	BitmapImage renderBitmap = sld.Export();
	PngBitmapEncoder encoder = new PngBitmapEncoder();
	encoder.Frames.Add(BitmapFrame.Create(renderBitmap)); // Push the rendered bitmap to it
	using (FileStream outStream = new FileStream(@"MySlide.png", FileMode.Create)) // Create a file stream for saving image
	{
		encoder.Save(outStream);
	}
``` 


### SlideLibObject

This class gives you access to 'slide library' object.

  1. Add  the following line to your code<br />
      using Autodesk.AutoCAD.Windows;
  2. You can now use the SlideLibObject class.

Example
``` c#
	using Autodesk.AutoCAD.Windows;
	
	SlideLibObject slb = new SlideLibObject(@"acad.slb");
	SlideObject sld = slb._Slides["ZIGZAG"];
	sld.SaveAs("ZIGZAG.sld");
```
is equivalent to
``` c#
	using Autodesk.AutoCAD.Windows;
	
	SlideObject sld = SlideLibObject.LoadSlideFromLibrary(@"acad.slb", "ZIGZAG");
	sld.SaveAs("ZIGZAG.sld");
```


### SlideCtrl

This control allows you to display AutoCAD Slides in a WPF application.

  1. Add  the following line to your WPF Window xaml<br />
``` xml
      xmlns:SlideCtrlNS="clr-namespace:Autodesk.AutoCAD.Windows;assembly=SlideCtrl"
```
  2. Insert the control in you window like this<br />
``` xml
      <SlideCtrlNS:SlideCtrl x:Name="preview" />
```

Example
``` xml
<Window x:Class="WpfApplication1.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="MainWindow" Height="350" Width="525"
		xmlns:SlideCtrlNS="clr-namespace:Autodesk.AutoCAD.Windows;assembly=SlideCtrl"
		>
    <Grid>
		<SlideCtrlNS:SlideCtrl x:Name="preview" SlideFileName="MySlide.sld" />
	</Grid>
</Window>
```
``` c#
	using Autodesk.AutoCAD.Windows;

	SlideObject sld = new SlideObject(@"MySlide.sld");
	preview.Slide = sld;
```
or
``` c#
	using Autodesk.AutoCAD.Windows;

	preview.SlideFileName = @"MySlide.sld";
```


## License

Autodesk Slide Control / Slide Library Manager Application are licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT). Please see the [LICENSE](LICENSE) file for full details.


## Written by

Cyrille Fauvel

Autodesk Developer Network

Autodesk Inc.

http://www.autodesk.com/adn  

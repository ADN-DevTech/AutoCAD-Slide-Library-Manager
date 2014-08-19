
Autodesk Slide Library Manager Application ( SLM )
=======================
Slm is a utility which helps you to create, edit, and manage AutoCAD slide libraries. 
It can read any version of AutoCAD slides, or slide libraries. 
It can also read any slide format (platform dependent such as UNIX, MAC, INTEL), and convert them into PNG transparent images.


The WPF Slide Control
=======================
The ‘Slide' control is a WPF control which displays slides under Windows platforms.
It can be used by any other WPF application. It exposes three objects which are:

  * SlideObject - which gives you access to 'slides'. I.e. SLD 
  * SlideLibObject - which gives you access to Slide Libraries. I.e. SLB
  * SlideCtrl - the control itself

  
Install using NuGet:
  1. Create a Wpf Application 
  2. Project -> Manage NuGet Packages...
  3. Search and Install 'AutoCAD Slide Control' package


SlideObject
-------------------
This class gives you access to 'slide' object.


SlideLibObject
-------------------
This class gives you access to 'slide library' object.


SlideCtrl
-------------------
This control allows you to display AutoCAD Slides in a WPF application.

  1. Add  the following line to your WPF Window xaml
      xmlns:SlideCtrlNS="clr-namespace:Autodesk.AutoCAD.Windows;assembly=SlideCtrl"
  2. Insert the control in you window like this
      <SlideCtrlNS:SlideCtrl x:Name="preview" />


## License

Autodesk Slide Control / Slide Library Manager Application are licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT). Please see the [LICENSE](LICENSE) file for full details.


## Written by

Cyrille Fauvel

Autodesk Developer Network

Autodesk Inc.

http://www.autodesk.com/adn  

@echo off
%~d0
cd /d %~dp0

if exist m:\nul subst m: /d
subst m: "C:\Program Files (x86)\WiX Toolset v3.8"

set Certificate_Password=
set Certificate_AppStore=
set DEVBIN=m:

if exist "AutoCADSlideLibraryManager.msi" del "AutoCADSlideLibraryManager.msi"
if exist "AutoCADSlideLibraryManager.wixpdb" del "AutoCADSlideLibraryManager.wixpdb"

%DEVBIN%/bin/candle.exe root.wxs -out AutoCADSlideLibraryManager.wixobj
%DEVBIN%%/bin/light.exe -sw1076 AutoCADSlideLibraryManager.wixobj -out AutoCADSlideLibraryManager.msi
if exist "AutoCADSlideLibraryManager.wixobj" del "AutoCADSlideLibraryManager.wixobj"

if "%Certificate_AppStore%" == "" (
  echo Warning: MSI not signed, Windows7 and beyond will complain during install.
) else (
  Tools\signtool sign /v /f %Certificate_AppStore% /p %Certificate_Password% /t http://timestamp.verisign.com/scripts/timstamp.dll "AutoCADSlideLibraryManager.msi"
  Tools\signtool verify /v /pa "AutoCADSlideLibraryManager.msi"
)
pause
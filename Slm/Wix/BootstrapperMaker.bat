
mkdir bootstrapper
cd bootstrapper
%DEVBIN%\bin\candle.exe ..\net40.wxs -ext WixBalExtension -ext %DEVBIN%\bin\WiXUtilExtension.dll -out ..\net40.wixobj
%DEVBIN%\bin\light.exe ..\net40.wixobj -ext WixBalExtension
cd ..
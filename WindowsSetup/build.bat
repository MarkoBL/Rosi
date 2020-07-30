powershell -Command "(New-Object Net.WebClient).DownloadFile('https://github.com/wixtoolset/wix3/releases/download/wix3112rtm/wix311-binaries.zip', 'wix.zip')"
mkdir wix
cd wix
tar -xf ..\wix.zip
cd ..

wix\candle.exe Rosi.wxs -o wixobj\ -arch x64 -ext .\wix\\WixUIExtension.dll
wix\light.exe wixobj\*.wixobj -o Rosi.msi -ext .\wix\\WixUIExtension.dll


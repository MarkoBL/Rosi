# Changelog

## 1.2.5
- Compiler debug information fix

## 1.2.2
- Downgraded CSScript back to version 1.4.0 due assembly reference errors

## 1.2.1
- Reworked the scriban system

## 1.2.0
- Enabling debug symbols
- Upgraded dependencies
- Console UTF-8 output
- Console error color fix
- Added disposable support
- Internal changes

## 1.1.0
- Added support for custom translations

## 1.0.10
- Translation file fix
- Added VersionUpdate.rosi to automate version update in various files
- Fixed a bug in Log.Write not writing to the console

## 1.0.9
- No more exclusive file lock on the log file.
- Added runtime.consoleloglevel and runtime.fileloglevel options

## 1.0.8
- Added setlinux, setmacos, setwindows
- Set Scriban default newline to "\n"

## 1.0.7
- Added Newtonsoft.Json
- Downgraded Scriban

## 1.0.6
- Added "rosi.title" option to set the title of the console window
- Force inclusion of supplied assemblies (IPNetwork2, YamlDotNet)
- Updated Scriban
- Updated IPNetwork2

## 1.0.5
- Fixed wildcard includes when using a path
- Fixed generated script output

## 1.0.4
- Added wildcard includes #1
- Improved assembly cache behaviour
- Added option to output the generated scripts #6
- Added YamlDotNet https://github.com/aaubry/YamlDotNet

## 1.0.3
- Added Windows msi installer.
  - Installs Rosi into "C:\Program Files\Rosi\"
  - Adds the directory to the system PATH environment variable and makes rosi.exe available in the terminal
  - Adds the .rosi file association
  - Fixed invalid error handling in scriban runtime
  - Updated Dependencies

## 1.0.2
- Added new Test "Execute", which generates a new SSH key via 'ssh-keygen' 
- Added "runtime.logscript" option. This shows the actual script in the log ouput, if the compilation fails
- Fixed invalid filename in scriban runtime
- Fixed crash in Runtime, when script file is missing
- Fixed argument spelling: rosiruntimeverions -> rosiruntimeversions

## 1.0.0
- Initial Release

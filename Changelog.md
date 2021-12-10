# Changelog

## 6.0
- Ported Rosi to .NET 6
- Bumped the version of Rosi to reflect the underlying .NET version
- Use Roslyn to compile the script files into assemblies and remove the dependency of CS-Script
- Refactored Runtime.Compiler to be useable from scripts. Now, you can compile a script within a script.
- Directives can have an os modifier, e.g. `// include(windows) WindowsSource/**` includes the source files from the 'WindowsSource' directory only when running on Windows
- Changed the namespace for the runtime from Rosi to Rosi.Runtime
- Removed old and obsolete code
- Removed compile/postcompile directive
- Updated Scriban
- Updated IPNetwork2
- Updated YamlDotNet

## 2.1
- Bumped CS-Script.Core to 2.0.0
- Modified assembly loading to be more verbose
- Added runtime.assemblyresolveinfo for displaying assembly resolve information, default is true

## 2.0
- runtime.assemblypath now accepts a comma separated list for multiple locations
- Updated Scriban
- Updated IPNetwork2
- Updated Newtonsoft.Json
- Updated YamlDotNet

## 1.3.3
- Added ScriptObjectExtensions
- Added Property IgnoreUnknownMembers to ThrowableScriptObject

## 1.3.2
- TemplateContext from scriban runtime is now public

## 1.3.1
- Added script line numbers to scriban template errors

## 1.3.0
- Fixed FormatException in translation
- Fixed output bug by locking the console output
- Added script line numbers to scriban runtime exceptions
- Added ThrowableScriptObject
- Modified scriban runtime

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

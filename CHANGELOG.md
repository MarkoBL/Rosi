# Changelog

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

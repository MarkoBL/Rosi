# Changelog

## 1.0.3
- Added Windows msi installer.
  - Installs Rosi into "C:\Program Files\Rosi\"
  - Adds the directory to the system PATH environment variable and makes rosi.exe available in the terminal
  - Adds the .rosi file association


## 1.0.2
- Added new Test "Execute", which generates a new SSH key via 'ssh-keygen' 
- Added "runtime.logscript" option. This shows the actual script in the log ouput, if the compilation fails
- Fixed invalid filename in scriban runtime
- Fixed crash in Runtime, when script file is missing
- Fixed argument spelling: rosiruntimeverions -> rosiruntimeversions

## 1.0.0
- Initial Release

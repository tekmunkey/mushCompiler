@ECHO OFF
rem
rem args into this script:
rem   %0 - script file name
rem   %1 - remoteSourcePath - use double quotes
rem   %2 - localTargetPath - use double quotes
rem

rem back up custom coloring schemes
copy "C:\Users\abpainter\AppData\Roaming\JGsoft\EditPad Pro 6\MUSHSoftcode.jgcscs" .\ColorSchemes\
rem back up custom file syntax folding
copy "C:\Users\abpainter\AppData\Roaming\JGsoft\EditPad Pro 6\MUSHSoftcode.jgfns" .\SyntaxNavigation\


rem
rem ie:  debianAtBluePillMushPSCPReceiver.bat /home/debian/Backups/20170303.thebluepillmush.tar.gz .\thebluepillmush
rem * using ~/Backups WILL NOT WORK - pscp for some reason won't recognize ~ as the user's home directory
rem 
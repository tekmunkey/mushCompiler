@ECHO OFF
rem 
rem This is just a simple macro that opens a command prompt on this directory
rem
rem In case you run cmd.bat As Administrator, it will dump you into your C:\Windows\System32 directory (or whatever your default Windows system drive\Windows Install Directory is)
rem The wackiness brings you back to where you started out, regardless
rem 
cmd.exe /K cd /D "%~d0%~p0"
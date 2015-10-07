%windir%\system32\inetsrv\appcmd set config -section:urlCompression /doDynamicCompression:True /commit:apphost  >> "%TEMP%\StartupLog.txt" 2>&1
IF %ERRORLEVEL% EQU 183 DO VERIFY > NUL
IF %ERRORLEVEL% NEQ 0 (
   ECHO Error adding a compression section to the Web.config file. >> "%TEMP%\StartupLog.txt" 2>&1
   GOTO ErrorExit
)

%windir%\system32\inetsrv\appcmd  set config -section:httpCompression /MinFileSizeForComp:256

%windir%\system32\inetsrv\appcmd set config -section:system.webServer/httpCompression /+"dynamicTypes.[mimeType='application/json; charset=utf-8',enabled='True']" /commit:apphost  >> "%TEMP%\StartupLog.txt" 2>&1
IF %ERRORLEVEL% EQU 183 DO VERIFY > NUL
IF %ERRORLEVEL% NEQ 0 (
   ECHO Error adding a compression section to the Web.config file. >> "%TEMP%\StartupLog.txt" 2>&1
   GOTO ErrorExit
)

%windir%\system32\inetsrv\appcmd set config -section:system.webServer/httpCompression /+"dynamicTypes.[mimeType='application/json',enabled='True']" /commit:apphost  >> "%TEMP%\StartupLog.txt" 2>&1
IF %ERRORLEVEL% EQU 183 DO VERIFY > NUL
IF %ERRORLEVEL% NEQ 0 (
   ECHO Error adding a compression section to the Web.config file. >> "%TEMP%\StartupLog.txt" 2>&1
   GOTO ErrorExit
)

:ErrorExit
REM   Report the date, time, and ERRORLEVEL of the error.
DATE /T >> "%TEMP%\StartupLog.txt" 2>&1
TIME /T >> "%TEMP%\StartupLog.txt" 2>&1
ECHO An error occurred during startup. ERRORLEVEL = %ERRORLEVEL% >> "%TEMP%\StartupLog.txt" 2>&1
EXIT %ERRORLEVEL%
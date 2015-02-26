echo %~dp0
set batchDir= %~dp0
copy /y %batchDir%bin\debug\* %temp%
call %temp%\debugCurveViewer %1%
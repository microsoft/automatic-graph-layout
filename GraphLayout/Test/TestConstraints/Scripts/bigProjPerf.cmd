@echo off
setlocal

set verbose=
set gen=
set testdir=%~dp0..
set outdir=

:argloop
  if "%1" == "" goto :argdone
  if "%1" == "debug" (
    set flavor=debug
  ) else if "%1" == "release" (
    set flavor=release
  ) else if "%1" == "gen" (
    set gen=y
  ) else if "%1" == "outdir" (
    if "" == "%~2" echo. & echo outdir needs a dirname & goto :eof
    set outdir=%2
    shift
  ) else (
    echo unknown arg '%1'
    goto :usage
  )
  shift
  goto :argloop
:argdone

if "" == "%outdir%" (
  echo.
  echo outdir required
  goto :usage
)

REM DfDvDepth: 228; Numblocks: 594 - MinVars: 1, MaxVars: 4185; Vars w/Inactive csts: 5755 of 6000
call :doit 6000   0xF6FC300A 00:00:15.588
REM DfDvDepth: 241; Numblocks: 706 - MinVars: 1, MaxVars: 5084; Vars w/Inactive csts: 6726 of 7000
call :doit 7000   0xB719282  00:00:24.786
REM DfDvDepth: 271; Numblocks: 803 - MinVars: 1, MaxVars: 6096; Vars w/Inactive csts: 7660 of 8000
call :doit 8000   0x7743B426 00:00:41.135
REM DfDvDepth: 268; Numblocks: 883 - MinVars: 1, MaxVars: 6421; Vars w/Inactive csts: 8618 of 9000
call :doit 9000   0x950DF6A4 00:00:56.539
REM DfDvDepth: 262; Numblocks: 952 - MinVars: 1, MaxVars: 5127; Vars w/Inactive csts: 9571 of 10000
call :doit 10000  0xB9AC1D1D 00:01:26.958

goto :eof

REM DfDvDepth: 433; Numblocks: 2040 - MinVars: 1, MaxVars: 7850; Vars w/Inactive csts: 19164 of 20000
call :doit 20000  0xD1DDFBDD 00:08:46.882
REM DfDvDepth: 626; Numblocks: 3027 - MinVars: 1, MaxVars: 14355; Vars w/Inactive csts: 28745 of 30000
call :doit 30000  0xC4998D1 00:22:13.842
REM DfDvDepth: 596; Numblocks: 4020 - MinVars: 1, MaxVars: 24026; Vars w/Inactive csts: 38390 of 40000
call :doit 40000  0x27D26F69 00:47:49.984
REM DfDvDepth: 765; Numblocks: 4833 - MinVars: 1, MaxVars: 26615; Vars w/Inactive csts: 47881 of 50000
call :doit 50000  0xD71B5015 01:08:13.920
REM DfDvDepth: 744; Numblocks: 5095 - MinVars: 1, MaxVars: 49810; Vars w/Inactive csts: 57457 of 60000
call :doit 60000  0x60429CAA 01:52:47.424
REM DfDvDepth: 857; Numblocks: 6224 - MinVars: 1, MaxVars: 53353; Vars w/Inactive csts: 67008 of 70000
call :doit 70000  0x22DD6C40 02:47:49.927
REM DfDvDepth: 953; Numblocks: 6168 - MinVars: 1, MaxVars: 51975; Vars w/Inactive csts: 76573 of 80000
call :doit 80000  0x9676E3E3 03:50:04.301
REM DfDvDepth: 1062; Numblocks: 7361 - MinVars: 1, MaxVars: 68869; Vars w/Inactive csts: 86053 of 90000
call :doit 90000  0xBBD961F2 04:54:29.138
REM DfDvDepth: 978; Numblocks: 7955 - MinVars: 1, MaxVars: 88450; Vars w/Inactive csts: 95596 of 100000
call :doit 100000 0xE0FC4652 06:09:06.637

goto :eof

:doit
set count=%1
set seed=%2
set time=%3
echo.
echo Variable count %count%; Expected time: %time%
echo.
if "%gen%" == "" (
  %testdir%\bin\%flavor%\Test_MSAGL.exe proj -file %outdir%\rand_%count%_10.txt
) else (
  REM %testdir%\bin\%flavor%\Test_MSAGL.exe proj -createfile %count% 10 seed %seed% %outdir%\rand_%count%_10.txt
  %testdir%\bin\%flavor%\Test_MSAGL.exe proj -createfile %count% 10 %outdir%\rand_%count%_10.txt
)
echo --------------------------------------------------
goto :eof

:usage
echo.
echo usage: %~nx0 debug^|release outdir ^<dirname^> [gen]
echo.
goto :eof

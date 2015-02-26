@echo off
setlocal

REM Directions:
REM  (Go to http://officeperf to find out more about the Office profiler.)
REM  Open a new cmd shell
REM     Use a different colour to indicate it may have CSHARP ON - see below.
REM  Change to the msradt\tedhar\msagl directory.
REM  test\run_perf        (this creates appropriately-named .opf files in the test\bin\release directory)
REM
REM If running with 'time', nothing special is required except to ensure the machine isn't
REM thrashing CPU on other tasks.

set run_time=
set one_off_args=
set one_off_opf=

echo.
if /i "time" == "%1" (
  echo Generating *.time.txt
  set run_time=y
) else if "prof" == "%1" (
  echo Generating *.opf
) else (
  goto :usage
)

if not "" == "%~2" (
  if "one_off" == "%2" (
    if "" == "%~3" (
      echo one_off requires TestConstraints cmdline
      goto :usage
    )
    if "" == "%run_time%" if "" == "%~4" (
      echo run_prof one_off requires output .opf filename
      goto :usage
    )

    echo Generating %1 %2 run with TestConstraints args:
    echo   %3
    if not "" == "%~4" (
      echo to output file %4
    )
    set one_off_args=%~3
    set one_off_opf=%~4
  ) else (
    goto :usage
  )
)

echo.

set test_dir=%~dp0..
set prof_dir=%test_dir%\bin\profile\run_perf
set exe=%test_dir%\Bin\Release\TestConstraints.exe

REM Set up output directory.
if not exist %prof_dir%\ProjectionSolver md %prof_dir%\ProjectionSolver
if not exist %prof_dir%\OverlapRemoval md %prof_dir%\OverlapRemoval

if "" == "%run_time%" (
  call offprof_on %exe%
)

if not "" == "%one_off_args%" (
  if "" == "%run_time%" (
    if exist "%one_off_opf%" del "%one_off_opf%"
    %exe% %one_off_args%
    call :move_opf "%one_off_opf%"
  ) else (
    %exe% %one_off_args%
  )

) else (

  REM Run a representative subset of tests - one file per TestConstraints call
  REM Format is:
  REM    :run_file <run_type> <filename_without_extension> <params to TestConstraints>
  REM    :run_test <run_type> testName <params to TestConstraints>
  REM    :run_cmd <run_type>  cmdName <params to TestConstraints>
  REM The first two arguments are read by :run_* to do its commandline generation and file-copying operations;
  REM for :run_cmd, the cmdName is just an identifier used for the filename, not related to any test method name.
  REM the <params to TestConstraints> are inserted into the appropriate place in the final commandline to TestConstraints.
  REM The -perf option is because the smaller files are so fast that running them just once has too much
  REM file-load overhead etc. relative to the meaningful time spent.

  echo.
  echo ProjectionSolver...
  echo.

  call :run_file ProjectionSolver Solver1_Vars100_ConstraintsMax3               -perf 24000
  call :run_file ProjectionSolver Solver1_Vars100_ConstraintsMax10              -perf 12000

  call :run_file ProjectionSolver Solver1_Vars500_ConstraintsMax3               -perf 1500
  call :run_file ProjectionSolver Solver_Vars500_ConstraintsMax3_WeightMax1K    -perf 1500
  call :run_file ProjectionSolver Solver1_Vars500_ConstraintsMax10              -perf 600
  call :run_file ProjectionSolver Solver_Vars500_ConstraintsMax10_WeightMax1K   -perf 600
  call :run_file ProjectionSolver Solver_Vars1000_ConstraintsMax10              -perf 120
  call :run_file ProjectionSolver Solver_Vars1000_ConstraintsMax10_WeightMax1K  -perf 120

  call :run_file ProjectionSolver Solver_Vars1000_ConstraintsMax50              -perf 60
  call :run_file ProjectionSolver Solver_Vars2500_ConstraintsMax10              -perf 24
  call :run_file ProjectionSolver Solver_Vars5000_ConstraintsMax10              -perf 12

  call :run_file ProjectionSolver Solver_Vars1000_ConstraintsMax10_VarWeights_1_To_1E6_At_10_Percent    -perf 60
  call :run_file ProjectionSolver Solver_Vars1000_ConstraintsMax10_VarWeights_1_To_1E6_At_25_Percent    -perf 60

  call :run_file ProjectionSolver Neighbors_Vars1000_ConstraintsMax10_NeighborsMax10_NeighborWeightMax100_VarWeights_1_To_1E6_At_10_Percent      -perf 30

  call :run_test ProjectionSolver StartAtZero1000X100                           -perf 10
  call :run_test ProjectionSolver StartAtZero10000                              -perf 20

  call :run_cmd  ProjectionSolver TestStartAtZero5000x100 -TestStartAtZero 5000 100

  echo.
  echo OverlapRemoval...
  echo.
  call :run_file OverlapRemoval Overlap1_Vars100_ConstraintsMax3                -perf 24000
  call :run_file OverlapRemoval Overlap1_Vars100_ConstraintsMax10               -perf 12000

  call :run_file OverlapRemoval Overlap1_Vars500_ConstraintsMax3                -perf 6000
  call :run_file OverlapRemoval Overlap1_Vars500_ConstraintsMax3_WeightMax1K    -perf 6000
  call :run_file OverlapRemoval Overlap1_Vars500_ConstraintsMax10               -perf 1500
  call :run_file OverlapRemoval Overlap1_Vars500_ConstraintsMax10_WeightMax1K   -perf 1500
  call :run_file OverlapRemoval Overlap_Vars1000_ConstraintsMax10               -perf 900
  call :run_file OverlapRemoval Overlap1_Vars1000_ConstraintsMax10_WeightMax1K  -perf 480

  call :run_file OverlapRemoval Overlap_Vars5000_ConstraintsMax3                -perf 240
  call :run_file OverlapRemoval Overlap_Vars5000_ConstraintsMax10               -perf 270
)

if "" == "%run_time%" (
  call offprof_off
)

goto :eof

:run_file
echo Running File: [%2]

call :get_data_dir %test_dir%\..\MSAGLTests\Resources\Constraints\%1\Data

if "" == "%run_time%" (
  if exist %prof_dir%\%1\%2.opf del %prof_dir%\%1\%2.opf
  %exe% %1 %3 %4 %5 %6 -file %data_dir%\%2.txt
  call :move_opf %prof_dir%\%1\%2.opf
) else (
  %exe% %1 %3 %4 %5 %6 -file %data_dir%\%2.txt | findstr Elapsed >  %prof_dir%\%1\%2.time.txt
)
goto :eof

:run_test
echo Running Test Method: [%2]

if "" == "%run_time%" (
  if exist %prof_dir%\%1\%2.opf del %prof_dir%\%1\%2.opf
  %exe% %1 %3 %4 %5 %6 %2
  call :move_opf %prof_dir%\%1\%2.opf
) else (
  %exe% %1 %3 %4 %5 %6 %2 | findstr Elapsed >  %prof_dir%\%1\%2.time.txt
)
goto :eof

:run_cmd
echo Free-form command named: [%2]

if "" == "%run_time%" (
  if exist %prof_dir%\%1\%2.opf del %prof_dir%\%1\%2.opf
  %exe% %1 %3 %4 %5 %6 %7
  call :move_opf %prof_dir%\%1\%2.opf
) else (
  %exe% %1 %3 %4 %5 %6 %7 | findstr Elapsed >  %prof_dir%\%1\%2.time.txt
)
goto :eof

:get_data_dir
REM Due to the 260-char limit we must compact the path.
set data_dir=%~f1
goto :eof

:move_opf
waitprof
move %test_dir%\bin\release\TestConstraints.opf %1
goto :eof

:usage
echo usage: %~nx0 time^|prof [one_off "quoted TestConstraints cmdline with args" outFileName.opf]
goto :eof

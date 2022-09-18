:: Remote desktop for Agent-based project (VDI00382)
echo off
time /t

set SCENARIO_DIR=C:\Users\B007566\Documents\Output\Scenarios
set EXE_FILE=..\bin\Release\net6.0\Dream.Models.SOE_Basic.exe

if exist %SCENARIO_DIR% rmdir /q /s %SCENARIO_DIR%

for /l %%i in (1 1 5) do (
	for /l %%x in (1 1 16) do (
		echo %%i %%x
		start %EXE_FILE%
        	ping 127.0.0.1 -n 2 > nul 
	)
       	ping 127.0.0.1 -n 430 > nul 
)

time /t
pause
:: Remote desktop for Agent-based project (VDI00382)
echo off
time /t

set SCENARIO_DIR=C:\Users\B007566\Documents\Output\Scenarios
set EXE_FILE=..\bin\Release\net6.0\Dream.Models.SOE_Basic.exe

if exist %SCENARIO_DIR% rmdir /q /s %SCENARIO_DIR%

for /l %%i in (1 1 22) do (
	for /l %%x in (1 1 4) do (
		echo %%i %%x
		start %EXE_FILE%
        	ping 127.0.0.1 -n 2 > nul 
		start %EXE_FILE% 1
        	ping 127.0.0.1 -n 2 > nul 
		start %EXE_FILE% 2
        	ping 127.0.0.1 -n 2 > nul 
		start %EXE_FILE% 3
        	ping 127.0.0.1 -n 2 > nul 
	)
       	ping 127.0.0.1 -n 500 > nul 
)

time /t
pause
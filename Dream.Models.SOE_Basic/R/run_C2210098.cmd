:: Locale machine (C2210098)
echo off
time /t

set SCENARIO_DIR=C:\Users\B007566\Documents\Output\Scenarios
set EXE_FILE=..\bin\Release\net6.0\Dream.Models.SOE_Basic.exe
set EXE_FILE2=..\bin\Release\net6.0\Dream.Models.SOE_Basic2.exe

if exist %SCENARIO_DIR% rmdir /q /s %SCENARIO_DIR%
copy %EXE_FILE% %EXE_FILE2%

for /l %%i in (1 1 21) do (
	for /l %%x in (1 1 3) do (
		echo %%i %%x
		start %EXE_FILE2%
        	ping 127.0.0.1 -n 2 > nul 
		start %EXE_FILE2% 1
        	ping 127.0.0.1 -n 2 > nul 
		start %EXE_FILE2% 2
        	ping 127.0.0.1 -n 2 > nul 
		start %EXE_FILE2% 3
        	ping 127.0.0.1 -n 2 > nul 
	)
       	ping 127.0.0.1 -n 610 > nul 
)

time /t
pause
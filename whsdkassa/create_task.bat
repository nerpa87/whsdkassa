@ECHO OFF
set /p whsdpath=Set path to wshdkassa.exe file(like "C:\Program Files"): 
@ECHO ON
schtasks /Create /RU SYSTEM /SC MINUTE /TN WHSDKASSA_SYS /TR "%whsdpath%\whsdkassa.exe" /V1
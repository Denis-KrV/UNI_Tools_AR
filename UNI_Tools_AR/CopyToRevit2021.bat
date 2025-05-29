@echo off
setlocal EnableDelayedExpansion

echo ����������� ������ ������� UNI_Tools_AR � Revit 2021...

set SOURCE_DIR=%~dp0bin\x64\Debug
set TARGET_ROOT=%USERPROFILE%\AppData\Roaming\Autodesk\Revit\Addins\2021
set TARGET_DIR=%TARGET_ROOT%\UNI_Tools_AR

rem ������� ����������, ���� �� ����������
if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"

rem �������� ��� DLL �����
echo ����������� DLL ������...
xcopy /Y "%SOURCE_DIR%\*.dll" "%TARGET_DIR%\"

rem �������� PDB ����� (��� �������)
echo ����������� PDB ������...
xcopy /Y "%SOURCE_DIR%\*.pdb" "%TARGET_DIR%\"

rem �������� ���������������� �����
echo ����������� ���������������� ������...
if exist "%SOURCE_DIR%\*.config" xcopy /Y /I "%SOURCE_DIR%\*.config" "%TARGET_DIR%\"

rem �������� ���� .addin � �������� ���������� Addins
echo ����������� .addin �����...
xcopy /Y "%~dp0UNI_Tools_AR.addin" "%TARGET_ROOT%\"

echo.
echo ����������� ��������� �������.
echo ����� ����������� �: %TARGET_DIR%
echo .addin ���� ���������� �: %TARGET_ROOT%
pause
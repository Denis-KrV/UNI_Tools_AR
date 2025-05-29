@echo off
setlocal EnableDelayedExpansion

echo Копирование файлов плагина UNI_Tools_AR в Revit 2021...

set SOURCE_DIR=%~dp0bin\x64\Debug
set TARGET_ROOT=%USERPROFILE%\AppData\Roaming\Autodesk\Revit\Addins\2021
set TARGET_DIR=%TARGET_ROOT%\UNI_Tools_AR

rem Создаем директорию, если не существует
if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"

rem Копируем все DLL файлы
echo Копирование DLL файлов...
xcopy /Y "%SOURCE_DIR%\*.dll" "%TARGET_DIR%\"

rem Копируем PDB файлы (для отладки)
echo Копирование PDB файлов...
xcopy /Y "%SOURCE_DIR%\*.pdb" "%TARGET_DIR%\"

rem Копируем конфигурационные файлы
echo Копирование конфигурационных файлов...
if exist "%SOURCE_DIR%\*.config" xcopy /Y /I "%SOURCE_DIR%\*.config" "%TARGET_DIR%\"

rem Копируем файл .addin в корневую директорию Addins
echo Копирование .addin файла...
xcopy /Y "%~dp0UNI_Tools_AR.addin" "%TARGET_ROOT%\"

echo.
echo Копирование завершено успешно.
echo Файлы скопированы в: %TARGET_DIR%
echo .addin файл скопирован в: %TARGET_ROOT%
pause
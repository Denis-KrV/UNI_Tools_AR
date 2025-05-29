@echo off
setlocal EnableDelayedExpansion

set SOURCE_DIR=%~dp0bin\x64\Debug
set REVIT_YEARS=2021 2022 2023

rem Копирование во все версии Revit
for %%y in (%REVIT_YEARS%) do (
    set TARGET_ROOT=%USERPROFILE%\AppData\Roaming\Autodesk\Revit\Addins\%%y
    set TARGET_DIR=!TARGET_ROOT!\UNI_Tools_AR
    
    echo Processing Revit %%y...
    
    if not exist "!TARGET_DIR!" mkdir "!TARGET_DIR!"
    
    echo Copying DLL files to Revit %%y...
    xcopy /Y "%SOURCE_DIR%\*.dll" "!TARGET_DIR!\"
    
    echo Copying PDB files to Revit %%y...
    xcopy /Y "%SOURCE_DIR%\*.pdb" "!TARGET_DIR!\"
    
    echo Copying config files to Revit %%y...
    if exist "%SOURCE_DIR%\*.config" xcopy /Y "%SOURCE_DIR%\*.config" "!TARGET_DIR!\"
    
    echo Copying .addin file to Revit %%y...
    xcopy /Y "%~dp0UNI_Tools_AR.addin" "!TARGET_ROOT!\"
    
    echo Files copied to Revit %%y
    echo.
)

echo All files copied successfully!
param(
    [string]$sourcePath,
    [string]$targetRoot
)

# Путь к папке плагина в директории Revit 2021
$targetPluginDir = Join-Path -Path $targetRoot -ChildPath "UNI_Tools_AR"

# Создаем директорию для плагина, если она не существует
if (-not (Test-Path -Path $targetPluginDir)) {
    New-Item -Path $targetPluginDir -ItemType Directory -Force
}

# Копирование всех необходимых файлов в папку плагина
$filesToCopy = Get-ChildItem -Path $sourcePath -Include "*.dll", "*.pdb", "*.config" -File
foreach ($file in $filesToCopy) {
    $targetFile = Join-Path -Path $targetPluginDir -ChildPath $file.Name
    Copy-Item -Path $file.FullName -Destination $targetFile -Force
    Write-Host "Копирование: $($file.Name) -> $targetPluginDir"
}

# Копирование .addin файла в корневую директорию Addins
$addinFile = Join-Path -Path $sourcePath -ChildPath "UNI_Tools_AR.addin"
if (Test-Path -Path $addinFile) {
    Copy-Item -Path $addinFile -Destination $targetRoot -Force
    Write-Host "Копирование: UNI_Tools_AR.addin -> $targetRoot"
}

Write-Host "Копирование файлов завершено" -ForegroundColor Green
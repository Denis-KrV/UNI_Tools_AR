param(
    [string]$sourcePath,
    [string]$targetRoot
)

# ���� � ����� ������� � ���������� Revit 2021
$targetPluginDir = Join-Path -Path $targetRoot -ChildPath "UNI_Tools_AR"

# ������� ���������� ��� �������, ���� ��� �� ����������
if (-not (Test-Path -Path $targetPluginDir)) {
    New-Item -Path $targetPluginDir -ItemType Directory -Force
}

# ����������� ���� ����������� ������ � ����� �������
$filesToCopy = Get-ChildItem -Path $sourcePath -Include "*.dll", "*.pdb", "*.config" -File
foreach ($file in $filesToCopy) {
    $targetFile = Join-Path -Path $targetPluginDir -ChildPath $file.Name
    Copy-Item -Path $file.FullName -Destination $targetFile -Force
    Write-Host "�����������: $($file.Name) -> $targetPluginDir"
}

# ����������� .addin ����� � �������� ���������� Addins
$addinFile = Join-Path -Path $sourcePath -ChildPath "UNI_Tools_AR.addin"
if (Test-Path -Path $addinFile) {
    Copy-Item -Path $addinFile -Destination $targetRoot -Force
    Write-Host "�����������: UNI_Tools_AR.addin -> $targetRoot"
}

Write-Host "����������� ������ ���������" -ForegroundColor Green
$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

$pagesRoot = (Resolve-Path "Pages").Path
$resourcesRoot = Join-Path $projectRoot "Resources\Pages"

$template = @"
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <data name="__placeholder" xml:space="preserve">
    <value>TODO</value>
  </data>
</root>
"@

Get-ChildItem -Path $pagesRoot -Filter *.cshtml -Recurse | ForEach-Object {
    $relativePath = $_.FullName.Substring($pagesRoot.Length).TrimStart([char[]]"/\\")
    $relativeDir = Split-Path $relativePath -Parent
    $targetDir = if ([string]::IsNullOrWhiteSpace($relativeDir)) { $resourcesRoot } else { Join-Path $resourcesRoot $relativeDir }
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    foreach ($lang in 'en','uk') {
        $targetFile = Join-Path $targetDir ("{0}.{1}.resx" -f $_.BaseName, $lang)
        if (-not (Test-Path $targetFile)) {
            $template | Out-File -FilePath $targetFile -Encoding utf8
        }
    }
}

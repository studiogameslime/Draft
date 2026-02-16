$root = "Assets"
$out = "AllScriptables.txt"

Remove-Item $out -ErrorAction Ignore

Get-ChildItem $root -Recurse -Filter *.asset | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    if ($content -notmatch "MonoBehaviour:") { return }

    Add-Content $out "================================================"
    Add-Content $out "FILE: $($_.FullName)"
    Add-Content $out "================================================"
    Add-Content $out $content
    Add-Content $out "`n"
}

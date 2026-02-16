$root = "Assets/Scripts"
$out = "AllScripts.txt"

Remove-Item $out -ErrorAction Ignore

Get-ChildItem $root -Recurse -Filter *.cs | ForEach-Object {
    Add-Content $out "========================"
    Add-Content $out "FILE: $($_.Name)"
    Add-Content $out "PATH: $($_.FullName)"
    Add-Content $out "========================"
    Get-Content $_.FullName | Add-Content $out
    Add-Content $out "`n"
}

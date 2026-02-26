$root = "Assets/Scripts"
$out = "AllScripts.txt"

Remove-Item $out -ErrorAction Ignore

Get-ChildItem $root -Recurse -Filter *.cs | ForEach-Object {
    $content = Get-Content $_.FullName -Raw

    Add-Content $out "========================"
    Add-Content $out "FILE: $($_.Name)"
    Add-Content $out "PATH: $($_.FullName)"
    Add-Content $out "========================"
    Add-Content $out $content
    Add-Content $out "`n"
}
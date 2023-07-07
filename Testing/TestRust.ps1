function displayresults([string]$title, [object[]]$results) {
    Write-Host
    if ($null -eq $results) {
        Write-Host "$title"
        Write-Host "PASSED"
    }
    else {
        Write-Host "$title"
        Write-Host "FAILED. Details:"
        $results | Out-String
    }
}


Write-Host "Testing Rust Folder Compare"
$expected1 = Get-Content -Path "./rustresults_name.txt" | Sort-Object
$actual1 = folder_compare.exe -a ./foldera -b ./folderb -c name -r | Sort-Object
$results1 = Compare-Object $expected1 $actual1

$expected2 = Get-Content -Path "./rustresults_namesize.txt" | Sort-Object
$actual2 = folder_compare.exe -a ./foldera -b ./folderb -c namesize -r | Sort-Object
$results2 = Compare-Object $expected2 $actual2

$expected3 = Get-Content -Path "./rustresults_hash.txt" | Sort-Object
$actual3 = folder_compare.exe -a ./foldera -b ./folderb -c hash -r | Sort-Object
$results3 = Compare-Object $expected3 $actual3

displayresults "Compare by name" $results1
displayresults "Compare by name+size" $results2
displayresults "Compare by hash" $results3

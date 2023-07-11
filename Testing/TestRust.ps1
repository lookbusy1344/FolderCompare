function Show-Results([string]$title, [object[]]$results) {
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

function Compare-Folders([string]$comparisonType) {
    $expected = Get-Content -Path "./rustresults_$comparisonType.txt" | Sort-Object
    $actual = folder_compare.exe -a ./foldera -b ./folderb -c $comparisonType -r | Sort-Object
    $results = Compare-Object $expected $actual
    Show-Results "Compare by $comparisonType" $results
}

Write-Host "Testing Rust Folder Compare"

Compare-Folders "name"
Compare-Folders "namesize"
Compare-Folders "hash"

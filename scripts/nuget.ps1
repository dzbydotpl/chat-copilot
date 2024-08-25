# Get all .csproj files in the current directory and subdirectories
$projects = Get-ChildItem -Recurse -Filter *.csproj

foreach ($project in $projects) {
    $projectName = $project.BaseName
    $projectPath = $project.FullName
    
    Write-Host "Updating packages for $projectName" -ForegroundColor Cyan
    
    # Get outdated packages
    $outdated = dotnet list $projectPath package --outdated --format json | ConvertFrom-Json
    
    # Update each outdated package
    foreach ($package in $outdated.projects[0].frameworks[0].topLevelPackages) {
        if (-not $package.autoReferenced) {
            $packageId = $package.id
            $latestVersion = $package.latestVersion
            Write-Host "Updating $packageId to $latestVersion" -ForegroundColor Yellow
            dotnet add $projectPath package $packageId --version $latestVersion
        }
    }
}

Write-Host "Package update process completed." -ForegroundColor Green
#!/bin/bash

# Function to update packages for a single project
update_packages() {
    local project=$1
    echo "Updating packages for $project"
    
    # Get outdated packages
    outdated=$(dotnet list $project package --outdated --format json | jq -r '.projects[0].frameworks[0].topLevelPackages[] | select(.autoReferenced==false) | "\(.id):\(.latestVersion)"')
    
    # Update each outdated package
    while IFS=: read -r package version; do
        if [ ! -z "$package" ]; then
            echo "Updating $package to $version"
            dotnet add $project package $package --version $version
        fi
    done <<< "$outdated"
}

# Find all .csproj files and update their packages
find . -name "*.csproj" | while read -r project; do
    update_packages "$project"
done

echo "Package update process completed."
$services = @(
    "UserService",
    "CatalogueService",
    "ReviewService",
    "SocialService",
    "ActivityService",
    "FeedService"
)
$root = $PSScriptRoot

foreach ($svc in $services) {
    $path = Join-Path $root "services\$svc"

    Start-Process powershell `
        -ArgumentList "-NoExit", "-Command", "cd '$path'; dotnet run"
}
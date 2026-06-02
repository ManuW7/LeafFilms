$base5001 = "http://localhost:5001"
$base5002 = "http://localhost:5002"
$base5003 = "http://localhost:5003"
$base5004 = "http://localhost:5004"
$base5005 = "http://localhost:5005"
$base5006 = "http://localhost:5006"
Invoke-RestMethod "$base5001/users/register" -Method Post -ContentType "application/json" `
    -Body '{"username":"alice","email":"alice@test.com","password":"Password123!"}'

Invoke-RestMethod "$base5001/users/register" -Method Post -ContentType "application/json" `
    -Body '{"username":"bob","email":"bob@test.com","password":"Password123!"}'
$alice = Invoke-RestMethod "$base5001/users/login" -Method Post -ContentType "application/json" `
    -Body '{"email":"alice@test.com","password":"Password123!"}'
$aliceH = @{ Authorization = "Bearer $($alice.token)" }

$bob = Invoke-RestMethod "$base5001/users/login" -Method Post -ContentType "application/json" `
    -Body '{"email":"bob@test.com","password":"Password123!"}'
$bobH = @{ Authorization = "Bearer $($bob.token)" }
Invoke-RestMethod "$base5001/users/me" -Headers $aliceH
Invoke-RestMethod "$base5001/users/search?q=bob" -Headers $aliceH

Write-Host "✅ UserService OK"
$movies = Invoke-RestMethod "$base5002/movies"
Write-Host "Фильмов в каталоге: $($movies.Count)"

$movieId = $movies[0].id
Invoke-RestMethod "$base5002/movies/search?q=nolan"
Invoke-RestMethod "$base5002/movies/$movieId"

Write-Host "✅ CatalogueService OK"
$review = Invoke-RestMethod "$base5003/reviews" -Method Post -Headers $aliceH `
    -ContentType "application/json" `
    -Body (@{ movieId = $movieId; rating = 8; text = "Отличный фильм, смотрел несколько раз!" } | ConvertTo-Json)

Write-Host "Создан отзыв: $($review.id)"
Invoke-RestMethod "$base5003/reviews" -Method Post -Headers $bobH `
    -ContentType "application/json" `
    -Body (@{ movieId = $movieId; rating = 10; text = "Шедевр! Лучший фильм всех времён." } | ConvertTo-Json)
Start-Sleep -Seconds 2
$updatedMovie = Invoke-RestMethod "$base5002/movies/$movieId"
Write-Host "Рейтинг фильма: $($updatedMovie.averageRating) ($($updatedMovie.reviewCount) отзывов)"
Invoke-RestMethod "$base5003/reviews/movie/$movieId"
try {
    Invoke-RestMethod "$base5003/reviews" -Method Post -Headers $aliceH `
        -ContentType "application/json" `
        -Body (@{ movieId = $movieId; rating = 5; text = "Второй отзыв" } | ConvertTo-Json)
} catch {
    Write-Host "Дубль отзыва: $($_.Exception.Response.StatusCode)"
}
Invoke-RestMethod "$base5003/reviews/$($review.id)" -Method Put -Headers $aliceH `
    -ContentType "application/json" `
    -Body '{"rating": 9}'

Start-Sleep -Seconds 2

$updatedMovie2 = Invoke-RestMethod "$base5002/movies/$movieId"
Write-Host "Рейтинг после обновления: $($updatedMovie2.averageRating)"
try {
    Invoke-RestMethod "$base5003/reviews" -Method Post -Headers $aliceH `
        -ContentType "application/json" `
        -Body (@{ movieId = [guid]::NewGuid(); rating = 5; text = "Текст отзыва здесь" } | ConvertTo-Json)
} catch {
    Write-Host "Несуществующий фильм: $($_.Exception.Response.StatusCode)"
}

Write-Host "✅ ReviewService OK"
Invoke-RestMethod "$base5004/follow" -Method Post -Headers $aliceH `
    -ContentType "application/json" `
    -Body (@{ followedId = $bob.id } | ConvertTo-Json)
try {
    Invoke-RestMethod "$base5004/follow" -Method Post -Headers $aliceH `
        -ContentType "application/json" `
        -Body (@{ followedId = $bob.id } | ConvertTo-Json)
} catch {
    Write-Host "Повторная подписка: $($_.Exception.Response.StatusCode)"
}
try {
    Invoke-RestMethod "$base5004/follow" -Method Post -Headers $aliceH `
        -ContentType "application/json" `
        -Body (@{ followedId = $alice.id } | ConvertTo-Json)
} catch {
    Write-Host "Подписка на себя: $($_.Exception.Response.StatusCode)"
}
$followers = Invoke-RestMethod "$base5004/users/$($bob.id)/followers"
Write-Host "Подписчики Bob: $($followers.Count)"
$following = Invoke-RestMethod "$base5004/users/$($alice.id)/following"
Write-Host "Подписки Alice: $($following.Count)"

Write-Host "✅ SocialService OK"
Invoke-RestMethod "$base5005/history" -Method Post -Headers $aliceH `
    -ContentType "application/json" `
    -Body (@{ movieId = $movieId; movieTitle = $movies[0].title } | ConvertTo-Json)
$history = Invoke-RestMethod "$base5005/history" -Headers $aliceH
Write-Host "История Alice: $($history.Count) фильм(ов)"
Invoke-RestMethod "$base5005/watchlist" -Method Post -Headers $aliceH `
    -ContentType "application/json" `
    -Body (@{ movieId = $movies[1].id; movieTitle = $movies[1].title } | ConvertTo-Json)
try {
    Invoke-RestMethod "$base5005/watchlist" -Method Post -Headers $aliceH `
        -ContentType "application/json" `
        -Body (@{ movieId = $movies[1].id; movieTitle = $movies[1].title } | ConvertTo-Json)
} catch {
    Write-Host "Дубль в вишлисте: $($_.Exception.Response.StatusCode)"
}
$watchlist = Invoke-RestMethod "$base5005/watchlist" -Headers $aliceH
Write-Host "Вишлист Alice: $($watchlist.Count) фильм(ов)"
Invoke-RestMethod "$base5005/watchlist/$($movies[1].id)" -Method Delete -Headers $aliceH
$watchlistAfter = Invoke-RestMethod "$base5005/watchlist" -Headers $aliceH
Write-Host "Вишлист после удаления: $($watchlistAfter.Count)"
$playlist = Invoke-RestMethod "$base5005/playlists" -Method Post -Headers $aliceH `
    -ContentType "application/json" `
    -Body '{"name":"Любимые фильмы"}'
Invoke-RestMethod "$base5005/playlists/$($playlist.id)/movies" -Method Post -Headers $aliceH `
    -ContentType "application/json" `
    -Body (@{ movieId = $movieId; movieTitle = $movies[0].title } | ConvertTo-Json)
$playlistFull = Invoke-RestMethod "$base5005/playlists/$($playlist.id)" -Headers $aliceH
Write-Host "Плейлист '$($playlistFull.name)': $($playlistFull.movies.Count) фильм(ов)"

Write-Host "✅ ActivityService OK"
Start-Sleep -Seconds 3
$feed = Invoke-RestMethod "$base5006/feed" -Headers $aliceH
Write-Host "Лента Alice: $($feed.Count) событий"
$feed | ForEach-Object { Write-Host "  - $($_.eventType): $($_.movieTitle)" }

Write-Host "✅ FeedService HTTP OK"

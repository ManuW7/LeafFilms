import React, { useEffect, useState, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { apiFetch } from '../lib/api'
import type { Movie } from '../types'
import Input from '../components/ui/Input'
import Spinner from '../components/ui/Spinner'
import Badge from '../components/ui/Badge'
import './MoviesPage.css'

function MovieCard({ movie, onClick }: { movie: Movie; onClick: () => void }) {
  return (
    <div className="movie-card" onClick={onClick}>
      <div className="movie-card__poster">
        {movie.posterUrl
          ? <img src={movie.posterUrl} alt={movie.title} />
          : '🎬'}
      </div>
      <div className="movie-card__body">
        <h3 className="movie-card__title">{movie.title}</h3>
        <div className="movie-card__badges">
          <Badge>{movie.year}</Badge>
          <Badge>{movie.genre}</Badge>
        </div>
        <p className="movie-card__director">{movie.director}</p>
        {movie.reviewCount > 0 && (
          <div className="movie-card__rating">
            <span className="movie-card__rating-star">★</span>
            <span className="movie-card__rating-value">{movie.averageRating.toFixed(1)}</span>
            <span className="movie-card__rating-count">({movie.reviewCount})</span>
          </div>
        )}
      </div>
    </div>
  )
}

export default function MoviesPage() {
  const navigate = useNavigate()
  const [movies, setMovies] = useState<Movie[]>([])
  const [loading, setLoading] = useState(true)
  const [query, setQuery] = useState('')
  const [searching, setSearching] = useState(false)

  useEffect(() => {
    apiFetch<Movie[]>('GET', '/movies').then(data => setMovies(data)).finally(() => setLoading(false))
  }, [])

  const handleSearch = useCallback(async (q: string) => {
    setQuery(q)
    if (!q.trim()) {
      setSearching(false)
      apiFetch<Movie[]>('GET', '/movies').then(data => setMovies(data))
      return
    }
    setSearching(true)
    const data = await apiFetch<Movie[]>('GET', `/movies/search?q=${encodeURIComponent(q)}`)
    setMovies(data)
    setSearching(false)
  }, [])

  if (loading) return <div style={{ display: 'flex', justifyContent: 'center', padding: '4rem' }}><Spinner /></div>

  return (
    <div>
      <div className="movies-page__header">
        <div>
          <h1 className="movies-page__title">Каталог фильмов</h1>
          <p className="movies-page__count">{movies.length} фильмов</p>
        </div>
        <div className="movies-page__search">
          <Input placeholder="Поиск по названию, режиссёру..." value={query}
            onChange={e => handleSearch(e.target.value)} />
        </div>
      </div>

      {searching && <div className="movies-page__searching"><Spinner size={24} /></div>}

      {!searching && movies.length === 0 && (
        <div className="movies-page__empty">
          <p className="movies-page__empty-icon">🔍</p>
          <p>Ничего не найдено по запросу «{query}»</p>
        </div>
      )}

      <div className="movies-page__grid">
        {movies.map(m => (
          <MovieCard key={m.id} movie={m} onClick={() => navigate(`/movies/${m.id}`)} />
        ))}
      </div>
    </div>
  )
}

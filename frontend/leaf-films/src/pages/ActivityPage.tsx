import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { apiFetch } from '../lib/api'
import type { Movie, Playlist, Review, WatchHistoryItem, WatchlistItem } from '../types'
import { useAuthStore } from '../store/authStore'
import Button from '../components/ui/Button'
import Spinner from '../components/ui/Spinner'
import Toast from '../components/ui/Toast'
import './ActivityPage.css'

type Tab = 'history' | 'watchlist' | 'playlists'

export default function ActivityPage() {
  const navigate = useNavigate()
  const { user } = useAuthStore()
  const [tab, setTab] = useState<Tab>('history')
  const [history, setHistory] = useState<WatchHistoryItem[]>([])
  const [watchlist, setWatchlist] = useState<WatchlistItem[]>([])
  const [playlists, setPlaylists] = useState<Playlist[]>([])
  const [reviewsByMovie, setReviewsByMovie] = useState<Record<string, number>>({})
  const [movieSearchByPlaylist, setMovieSearchByPlaylist] = useState<Record<string, string>>({})
  const [movieResultsByPlaylist, setMovieResultsByPlaylist] = useState<Record<string, Movie[]>>({})
  const [loading, setLoading] = useState(true)
  const [toast, setToast] = useState<{ msg: string; type: 'success' | 'error' } | null>(null)
  const [newPlaylistName, setNewPlaylistName] = useState('')
  const [creating, setCreating] = useState(false)

  useEffect(() => {
    Promise.all([
      apiFetch<WatchHistoryItem[]>('GET', '/history'),
      apiFetch<WatchlistItem[]>('GET', '/watchlist'),
      apiFetch<Playlist[]>('GET', '/playlists'),
      user ? apiFetch<Review[]>('GET', `/reviews/user/${user.id}`) : Promise.resolve([]),
    ]).then(([historyData, watchlistData, playlistsData, reviewsData]) => {
      setHistory(historyData)
      setWatchlist(watchlistData)
      setPlaylists(playlistsData)
      setReviewsByMovie(Object.fromEntries(reviewsData.map(review => [review.movieId, review.rating])))
    }).finally(() => setLoading(false))
  }, [user])

  const handleRemoveWatchlist = async (movieId: string) => {
    await apiFetch('DELETE', `/watchlist/${movieId}`)
    setWatchlist(w => w.filter(x => x.movieId !== movieId))
    setToast({ msg: 'Удалено из списка "Буду смотреть"', type: 'success' })
  }

  const handleCreatePlaylist = async () => {
    if (!newPlaylistName.trim()) return
    setCreating(true)
    try {
      const playlist = await apiFetch<Playlist>('POST', '/playlists', { name: newPlaylistName.trim() })
      setPlaylists(p => [...p, playlist])
      setNewPlaylistName('')
    } catch {
      setToast({ msg: 'Ошибка создания плейлиста', type: 'error' })
    } finally {
      setCreating(false)
    }
  }

  const handleDeletePlaylist = async (id: string) => {
    await apiFetch('DELETE', `/playlists/${id}`)
    setPlaylists(p => p.filter(x => x.id !== id))
  }

  const handleSearchMovieForPlaylist = async (playlistId: string, query: string) => {
    setMovieSearchByPlaylist(state => ({ ...state, [playlistId]: query }))
    if (query.trim().length < 2) {
      setMovieResultsByPlaylist(state => ({ ...state, [playlistId]: [] }))
      return
    }
    const results = await apiFetch<Movie[]>('GET', `/movies/search?q=${encodeURIComponent(query)}`)
    setMovieResultsByPlaylist(state => ({ ...state, [playlistId]: results }))
  }

  const handleAddMovieToPlaylist = async (playlistId: string, movie: Movie) => {
    try {
      await apiFetch('POST', `/playlists/${playlistId}/movies`, { movieId: movie.id, movieTitle: movie.title })
      setPlaylists(items => items.map(pl => pl.id === playlistId && !pl.movies.some(m => m.movieId === movie.id)
        ? { ...pl, movies: [...pl.movies, { movieId: movie.id, movieTitle: movie.title, addedAt: new Date().toISOString() }] }
        : pl))
      setMovieSearchByPlaylist(state => ({ ...state, [playlistId]: '' }))
      setMovieResultsByPlaylist(state => ({ ...state, [playlistId]: [] }))
      setToast({ msg: 'Фильм добавлен в плейлист', type: 'success' })
    } catch {
      setToast({ msg: 'Не удалось добавить фильм', type: 'error' })
    }
  }

  if (loading) return <div style={{ display: 'flex', justifyContent: 'center', padding: '4rem' }}><Spinner /></div>

  const tabs: { key: Tab; label: string; count: number }[] = [
    { key: 'history', label: 'История просмотров', count: history.length },
    { key: 'watchlist', label: 'Буду смотреть', count: watchlist.length },
    { key: 'playlists', label: 'Плейлисты', count: playlists.length },
  ]

  return (
    <div>
      {toast && <Toast message={toast.msg} type={toast.type} onClose={() => setToast(null)} />}
      <h1 className="activity-page__title">Моя активность</h1>

      <div className="activity-tabs">
        {tabs.map(t => (
          <button key={t.key}
            className={`activity-tabs__btn ${tab === t.key ? 'activity-tabs__btn--active' : ''}`}
            onClick={() => setTab(t.key)}>
            {t.label} <span className="activity-tabs__count">({t.count})</span>
          </button>
        ))}
      </div>

      {tab === 'history' && (
        <div className="activity-list">
          {history.length === 0 && <p className="activity-empty">Вы еще ничего не смотрели</p>}
          {history.map(item => (
            <div key={item.id} className="activity-item" onClick={() => navigate(`/movies/${item.movieId}`)}>
              <div className="activity-item__left">
                <span className="activity-item__icon">🎬</span>
                <span className="activity-item__title">{item.movieTitle}</span>
              </div>
              <span className="activity-item__date">{new Date(item.watchedAt).toLocaleDateString('ru-RU')}</span>
              {reviewsByMovie[item.movieId] && (
                <span className="activity-item__rating">{'★'.repeat(reviewsByMovie[item.movieId])}</span>
              )}
            </div>
          ))}
        </div>
      )}

      {tab === 'watchlist' && (
        <div className="activity-list">
          {watchlist.length === 0 && <p className="activity-empty">Список пуст. Добавляйте фильмы со страниц фильмов</p>}
          {watchlist.map(item => (
            <div key={item.id} className="watchlist-item">
              <div className="watchlist-item__link" onClick={() => navigate(`/movies/${item.movieId}`)}>
                <span className="watchlist-item__icon">🌿</span>
                <div>
                  <p className="watchlist-item__title">{item.movieTitle}</p>
                  <p className="watchlist-item__added">Добавлен {new Date(item.addedAt).toLocaleDateString('ru-RU')}</p>
                </div>
              </div>
              <Button variant="ghost" size="sm" onClick={() => handleRemoveWatchlist(item.movieId)} style={{ color: 'var(--danger)' }}>
                Удалить
              </Button>
            </div>
          ))}
        </div>
      )}

      {tab === 'playlists' && (
        <div>
          <div className="playlist-create">
            <input className="playlist-create__input" value={newPlaylistName}
              onChange={e => setNewPlaylistName(e.target.value)}
              placeholder="Название нового плейлиста..."
              onKeyDown={e => e.key === 'Enter' && handleCreatePlaylist()} />
            <Button onClick={handleCreatePlaylist} loading={creating}>Создать</Button>
          </div>
          <div className="playlists-list">
            {playlists.length === 0 && <p className="activity-empty">Плейлистов пока нет</p>}
            {playlists.map(pl => (
              <div key={pl.id} className="playlist-card">
                <div className="playlist-card__header">
                  <div>
                    <h3 className="playlist-card__name">{pl.name}</h3>
                    <p className="playlist-card__meta">{pl.movies.length} фильмов · создан {new Date(pl.createdAt).toLocaleDateString('ru-RU')}</p>
                  </div>
                  <Button variant="ghost" size="sm" onClick={() => handleDeletePlaylist(pl.id)} style={{ color: 'var(--danger)' }}>Удалить</Button>
                </div>
                <div className="playlist-card__add">
                  <input
                    value={movieSearchByPlaylist[pl.id] || ''}
                    onChange={e => handleSearchMovieForPlaylist(pl.id, e.target.value)}
                    placeholder="Найти фильм по русскому или английскому названию..."
                  />
                  {(movieResultsByPlaylist[pl.id] || []).length > 0 && (
                    <div className="playlist-card__search-results">
                      {(movieResultsByPlaylist[pl.id] || [])
                        .filter(movie => !pl.movies.some(item => item.movieId === movie.id))
                        .slice(0, 6)
                        .map(movie => (
                          <button key={movie.id} onClick={() => handleAddMovieToPlaylist(pl.id, movie)}>
                            <span>{movie.title}</span>
                            {movie.titleRu && <small>{movie.titleRu}</small>}
                          </button>
                        ))}
                    </div>
                  )}
                </div>
                {pl.movies.length > 0 && (
                  <div className="playlist-card__movies">
                    {pl.movies.map(m => (
                      <span key={m.movieId} className="playlist-card__movie" onClick={() => navigate(`/movies/${m.movieId}`)}>
                        {m.movieTitle}
                      </span>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

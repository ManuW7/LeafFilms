import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { ApiError, apiFetch } from '../lib/api'
import type { Movie, Playlist, Review, WatchlistItem } from '../types'
import { useAuthStore } from '../store/authStore'
import Button from '../components/ui/Button'
import Badge from '../components/ui/Badge'
import Avatar from '../components/ui/Avatar'
import StarRating from '../components/ui/StarRating'
import Spinner from '../components/ui/Spinner'
import Toast from '../components/ui/Toast'
import './MoviePage.css'

const allowedImageExtensions = ['jpg', 'jpeg', 'png', 'webp', 'gif']
const maxImageSide = 4096

async function validateReviewImage(url: string): Promise<string> {
  const trimmed = url.trim()
  if (!trimmed) return ''

  let parsed: URL
  try {
    parsed = new URL(trimmed)
  } catch {
    return 'Ссылка на изображение должна быть корректным URL.'
  }

  const extension = parsed.pathname.split('.').pop()?.toLowerCase() || ''
  if (!allowedImageExtensions.includes(extension)) {
    return 'Поддерживаются только изображения JPG, PNG, WEBP или GIF.'
  }

  return new Promise(resolve => {
    const image = new Image()
    image.onload = () => {
      if (image.naturalWidth > maxImageSide || image.naturalHeight > maxImageSide) {
        resolve(`Изображение слишком большое: максимум ${maxImageSide}x${maxImageSide}px.`)
      } else {
        resolve('')
      }
    }
    image.onerror = () => resolve('Не удалось загрузить изображение по этой ссылке.')
    image.src = trimmed
  })
}

function ReviewCard({ review, canDelete, onDelete }: {
  review: Review
  canDelete: boolean
  onDelete: (id: string) => void
}) {
  const [deleting, setDeleting] = useState(false)
  const [confirming, setConfirming] = useState(false)

  const handleDelete = async () => {
    setDeleting(true)
    try {
      await apiFetch('DELETE', `/reviews/${review.id}`)
      onDelete(review.id)
    } finally {
      setDeleting(false)
      setConfirming(false)
    }
  }

  return (
    <div className="review-card">
      <div className="review-card__header">
        <Avatar name={review.username} size={36} />
        <div>
          <p className="review-card__user-name">{review.username}</p>
          <p className="review-card__date">{new Date(review.createdAt).toLocaleDateString('ru-RU')}</p>
        </div>
        <div className="review-card__rating">
          <span className="review-card__score">{review.rating}</span>
          <span className="review-card__score-max">/10</span>
          {canDelete && (
            <Button variant="danger" size="sm" onClick={() => setConfirming(true)} style={{ marginLeft: '0.5rem' }}>
              Удалить
            </Button>
          )}
        </div>
      </div>
      <p className="review-card__text">{review.text}</p>
      {review.imageUrl && <img className="review-card__image" src={review.imageUrl} alt="Изображение к отзыву" />}
      {confirming && (
        <div className="review-card__confirm">
          <span>Точно удалить отзыв?</span>
          <div>
            <Button variant="danger" size="sm" loading={deleting} onClick={handleDelete}>Удалить</Button>
            <Button variant="ghost" size="sm" onClick={() => setConfirming(false)}>Оставить</Button>
          </div>
        </div>
      )}
    </div>
  )
}

export default function MoviePage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { user, isAuth } = useAuthStore()
  const isAdmin = user?.role === 'admin'
  const [movie, setMovie] = useState<Movie | null>(null)
  const [reviews, setReviews] = useState<Review[]>([])
  const [loading, setLoading] = useState(true)
  const [form, setForm] = useState({ rating: 0, text: '', imageUrl: '' })
  const [reviewError, setReviewError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [toast, setToast] = useState<{ msg: string; type: 'success' | 'error' } | null>(null)
  const [inWatchlist, setInWatchlist] = useState(false)
  const [playlists, setPlaylists] = useState<Playlist[]>([])
  const [selectedPlaylistId, setSelectedPlaylistId] = useState('')
  const [newPlaylistName, setNewPlaylistName] = useState('')
  const [adminForm, setAdminForm] = useState({
    title: '',
    titleRu: '',
    year: '',
    genre: '',
    director: '',
    description: '',
    posterUrl: '',
  })

  useEffect(() => {
    if (!id) return
    Promise.all([
      apiFetch<Movie>('GET', `/movies/${id}`),
      apiFetch<Review[]>('GET', `/reviews/movie/${id}`),
    ]).then(([movieData, reviewsData]) => {
      setMovie(movieData)
      setReviews(reviewsData)
      setAdminForm({
        title: movieData.title,
        titleRu: movieData.titleRu || '',
        year: String(movieData.year),
        genre: movieData.genre,
        director: movieData.director,
        description: movieData.description || '',
        posterUrl: movieData.posterUrl || '',
      })
    }).finally(() => setLoading(false))
  }, [id])

  useEffect(() => {
    if (!id || !isAuth) return
    Promise.all([
      apiFetch<WatchlistItem[]>('GET', '/watchlist'),
      apiFetch<Playlist[]>('GET', '/playlists'),
    ]).then(([watchlistData, playlistsData]) => {
      setInWatchlist(watchlistData.some(item => item.movieId === id))
      setPlaylists(playlistsData)
      setSelectedPlaylistId(playlistsData[0]?.id || '')
    }).catch(() => setToast({ msg: 'Не удалось загрузить личные списки', type: 'error' }))
  }, [id, isAuth])

  const handleToggleWatchlist = async () => {
    if (!movie) return
    try {
      if (inWatchlist) {
        await apiFetch('DELETE', `/watchlist/${movie.id}`)
        setInWatchlist(false)
        setToast({ msg: 'Убрано из списка "Буду смотреть"', type: 'success' })
      } else {
        await apiFetch('POST', '/watchlist', { movieId: movie.id, movieTitle: movie.title })
        setInWatchlist(true)
        setToast({ msg: 'Добавлено в список "Буду смотреть"', type: 'success' })
      }
    } catch {
      setToast({ msg: 'Не удалось обновить список "Буду смотреть"', type: 'error' })
    }
  }

  const handleAddToPlaylist = async () => {
    if (!movie) return
    try {
      let playlistId = selectedPlaylistId
      if (!playlistId && newPlaylistName.trim()) {
        const playlist = await apiFetch<Playlist>('POST', '/playlists', { name: newPlaylistName.trim() })
        setPlaylists(p => [...p, playlist])
        playlistId = playlist.id
        setSelectedPlaylistId(playlist.id)
        setNewPlaylistName('')
      }

      if (!playlistId) {
        setToast({ msg: 'Создайте или выберите плейлист', type: 'error' })
        return
      }

      await apiFetch('POST', `/playlists/${playlistId}/movies`, { movieId: movie.id, movieTitle: movie.title })
      setPlaylists(p => p.map(pl => pl.id === playlistId && !pl.movies.some(m => m.movieId === movie.id)
        ? { ...pl, movies: [...pl.movies, { movieId: movie.id, movieTitle: movie.title, addedAt: new Date().toISOString() }] }
        : pl))
      setToast({ msg: 'Фильм добавлен в плейлист', type: 'success' })
    } catch {
      setToast({ msg: 'Не удалось добавить фильм в плейлист', type: 'error' })
    }
  }

  const handleMarkWatched = async () => {
    if (!movie) return
    try {
      await apiFetch('POST', '/history', { movieId: movie.id, movieTitle: movie.title })
      setToast({ msg: 'Отмечено как просмотренное', type: 'success' })
    } catch {
      setToast({ msg: 'Ошибка', type: 'error' })
    }
  }

  const handleUpdateMovie = async () => {
    if (!movie) return
    try {
      const updated = await apiFetch<Movie>('PUT', `/movies/${movie.id}`, {
        title: adminForm.title,
        titleRu: adminForm.titleRu,
        year: Number(adminForm.year),
        genre: adminForm.genre,
        director: adminForm.director,
        description: adminForm.description,
        posterUrl: adminForm.posterUrl,
      })
      setMovie(updated)
      setToast({ msg: 'Фильм обновлен', type: 'success' })
    } catch (err) {
      setToast({ msg: err instanceof ApiError ? err.data?.error || 'Не удалось обновить фильм' : 'Не удалось обновить фильм', type: 'error' })
    }
  }

  const handleDeleteMovie = async () => {
    if (!movie || !window.confirm('Удалить фильм из каталога? Отзывы о нем будут убраны из лент.')) return
    await apiFetch('DELETE', `/movies/${movie.id}`)
    navigate('/movies')
  }

  const handleSubmitReview = async (e: FormEvent) => {
    e.preventDefault()
    setReviewError('')
    if (!form.rating) {
      setReviewError('Поставьте оценку от 1 до 10.')
      return
    }
    if (form.text.trim().length < 10) {
      setReviewError('Отзыв должен быть не короче 10 символов.')
      return
    }
    if (!id) return

    const imageError = await validateReviewImage(form.imageUrl)
    if (imageError) {
      setReviewError(imageError)
      return
    }

    setSubmitting(true)
    try {
      const review = await apiFetch<Review>('POST', '/reviews', {
        movieId: id,
        rating: form.rating,
        text: form.text.trim(),
        imageUrl: form.imageUrl.trim() || undefined,
      })
      setReviews(r => [review, ...r])
      setForm({ rating: 0, text: '', imageUrl: '' })
      setToast({ msg: 'Отзыв опубликован!', type: 'success' })
    } catch (err) {
      setToast({ msg: err instanceof ApiError ? err.data?.error || 'Ошибка' : 'Ошибка', type: 'error' })
    } finally {
      setSubmitting(false)
    }
  }

  const userAlreadyReviewed = reviews.some(r => r.userId === user?.id)

  if (loading) return <div style={{ display: 'flex', justifyContent: 'center', padding: '4rem' }}><Spinner /></div>
  if (!movie) return <p style={{ color: 'var(--danger)' }}>Фильм не найден</p>

  return (
    <div className="movie-page">
      {toast && <Toast message={toast.msg} type={toast.type} onClose={() => setToast(null)} />}

      <div className="movie-page__header">
        <div>
          <div className="movie-page__badges">
            <Badge>{movie.year}</Badge>
            <Badge>{movie.genre}</Badge>
          </div>
          <h1 className="movie-page__title">{movie.title}</h1>
          {movie.titleRu && <p className="movie-page__title-alt">{movie.titleRu}</p>}
          <p className="movie-page__director">Режиссер: <strong>{movie.director}</strong></p>
          {movie.reviewCount > 0 && (
            <div className="movie-page__rating">
              <span className="movie-page__avg-score">{movie.averageRating.toFixed(1)}</span>
              <div>
                <StarRating value={Math.round(movie.averageRating)} readonly size={16} />
                <p className="movie-page__review-count">{movie.reviewCount} отзывов</p>
              </div>
            </div>
          )}
          {movie.description && <p className="movie-page__description">{movie.description}</p>}
          {isAuth && (
            <>
              <div className="movie-page__actions">
                <Button variant="secondary" onClick={handleMarkWatched}>Отметить просмотренным</Button>
                <Button variant={inWatchlist ? 'primary' : 'secondary'} onClick={handleToggleWatchlist}>
                  {inWatchlist ? 'В списке "Буду смотреть"' : 'Буду смотреть'}
                </Button>
              </div>
              <div className="movie-page__playlist-actions">
                <select value={selectedPlaylistId} onChange={e => setSelectedPlaylistId(e.target.value)}>
                  <option value="">Новый плейлист</option>
                  {playlists.map(pl => <option key={pl.id} value={pl.id}>{pl.name}</option>)}
                </select>
                {!selectedPlaylistId && (
                  <input value={newPlaylistName} onChange={e => setNewPlaylistName(e.target.value)} placeholder="Название плейлиста" />
                )}
                <Button variant="secondary" onClick={handleAddToPlaylist}>Добавить в плейлист</Button>
              </div>
            </>
          )}
        </div>
        <div className="movie-page__poster">
          {movie.posterUrl ? <img src={movie.posterUrl} alt={movie.title} /> : '🎬'}
        </div>
      </div>

      {isAdmin && (
        <div className="admin-movie-panel">
          <h2>Управление фильмом</h2>
          <div className="admin-movie-panel__grid">
            <input value={adminForm.title} onChange={e => setAdminForm(f => ({ ...f, title: e.target.value }))} placeholder="Название" />
            <input value={adminForm.titleRu} onChange={e => setAdminForm(f => ({ ...f, titleRu: e.target.value }))} placeholder="Название на русском" />
            <input value={adminForm.year} onChange={e => setAdminForm(f => ({ ...f, year: e.target.value }))} placeholder="Год" type="number" />
            <input value={adminForm.genre} onChange={e => setAdminForm(f => ({ ...f, genre: e.target.value }))} placeholder="Жанр" />
            <input value={adminForm.director} onChange={e => setAdminForm(f => ({ ...f, director: e.target.value }))} placeholder="Режиссер" />
            <input value={adminForm.posterUrl} onChange={e => setAdminForm(f => ({ ...f, posterUrl: e.target.value }))} placeholder="URL постера" />
          </div>
          <textarea value={adminForm.description} onChange={e => setAdminForm(f => ({ ...f, description: e.target.value }))} placeholder="Описание" rows={3} />
          <div className="admin-movie-panel__actions">
            <Button onClick={handleUpdateMovie}>Сохранить</Button>
            <Button variant="danger" onClick={handleDeleteMovie}>Удалить фильм</Button>
          </div>
        </div>
      )}

      {isAuth && !userAlreadyReviewed && (
        <div className="review-form">
          <h2 className="review-form__title">Написать отзыв</h2>
          <form onSubmit={handleSubmitReview} className="review-form__body">
            <div>
              <p className="review-form__label">Оценка</p>
              <StarRating value={form.rating} onChange={v => setForm(f => ({ ...f, rating: v }))} size={24} />
            </div>
            <div>
              <p className="review-form__label">Отзыв</p>
              <textarea
                className="review-form__textarea"
                value={form.text}
                onChange={e => setForm(f => ({ ...f, text: e.target.value }))}
                placeholder="Минимум 10 символов. Поделитесь впечатлениями о фильме..."
                rows={4}
              />
            </div>
            <div>
              <p className="review-form__label">Изображение к отзыву (URL, необязательно)</p>
              <input
                className="review-form__input"
                value={form.imageUrl}
                onChange={e => setForm(f => ({ ...f, imageUrl: e.target.value }))}
                placeholder="https://example.com/still.jpg"
              />
            </div>
            {reviewError && <p className="review-form__error">{reviewError}</p>}
            <Button type="submit" loading={submitting} style={{ alignSelf: 'flex-start' }}>
              Опубликовать
            </Button>
          </form>
        </div>
      )}

      <h2 className="reviews-heading">
        Отзывы {reviews.length > 0 && <span className="reviews-heading__count">({reviews.length})</span>}
      </h2>
      {reviews.length === 0 ? (
        <div className="reviews-empty">
          <p className="reviews-empty__icon">💬</p>
          <p>Отзывов пока нет. Будьте первым!</p>
        </div>
      ) : (
        <div className="reviews-list">
          {reviews.map(r => (
            <ReviewCard
              key={r.id}
              review={r}
              canDelete={isAdmin || r.userId === user?.id}
              onDelete={rid => setReviews(rs => rs.filter(x => x.id !== rid))}
            />
          ))}
        </div>
      )}
    </div>
  )
}

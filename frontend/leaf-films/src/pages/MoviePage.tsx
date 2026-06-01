import React, { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { apiFetch } from '../lib/api'
import type { Movie, Review } from '../types'
import { useAuthStore } from '../store/authStore'
import Button from '../components/ui/Button'
import Badge from '../components/ui/Badge'
import Avatar from '../components/ui/Avatar'
import StarRating from '../components/ui/StarRating'
import Spinner from '../components/ui/Spinner'
import Toast from '../components/ui/Toast'
import './MoviePage.css'

function ReviewCard({ review, currentUserId, onDelete }: {
  review: Review; currentUserId?: string; onDelete: (id: string) => void
}) {
  const [deleting, setDeleting] = useState(false)

  const handleDelete = async () => {
    setDeleting(true)
    await apiFetch('DELETE', `/reviews/${review.id}`)
    onDelete(review.id)
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
          {currentUserId === review.userId && (
            <Button variant="danger" size="sm" loading={deleting} onClick={handleDelete} style={{ marginLeft: '0.5rem' }}>
              Удалить
            </Button>
          )}
        </div>
      </div>
      <p className="review-card__text">{review.text}</p>
    </div>
  )
}

export default function MoviePage() {
  const { id } = useParams<{ id: string }>()
  const { user, isAuth } = useAuthStore()
  const [movie, setMovie] = useState<Movie | null>(null)
  const [reviews, setReviews] = useState<Review[]>([])
  const [loading, setLoading] = useState(true)
  const [form, setForm] = useState({ rating: 0, text: '' })
  const [submitting, setSubmitting] = useState(false)
  const [toast, setToast] = useState<{ msg: string; type: 'success' | 'error' } | null>(null)
  const [inWatchlist, setInWatchlist] = useState(false)

  useEffect(() => {
    if (!id) return
    Promise.all([
      apiFetch<Movie>('GET', `/movies/${id}`),
      apiFetch<Review[]>('GET', `/reviews/movie/${id}`),
    ]).then(([movieData, reviewsData]) => {
      setMovie(movieData)
      setReviews(reviewsData)
    }).finally(() => setLoading(false))
  }, [id])

  const handleAddToWatchlist = async () => {
    if (!movie) return
    try {
      await apiFetch('POST', '/watchlist', { movieId: movie.id, movieTitle: movie.title })
      setInWatchlist(true)
      setToast({ msg: 'Добавлено в список просмотра', type: 'success' })
    } catch {
      setToast({ msg: 'Уже в списке или ошибка', type: 'error' })
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

  const handleSubmitReview = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!form.rating || form.text.length < 10 || !id) return
    setSubmitting(true)
    try {
      const review = await apiFetch<Review>('POST', '/reviews', { movieId: id, rating: form.rating, text: form.text })
      setReviews(r => [review, ...r])
      setForm({ rating: 0, text: '' })
      setToast({ msg: 'Отзыв опубликован!', type: 'success' })
    } catch (err: any) {
      setToast({ msg: err?.data?.error || 'Ошибка', type: 'error' })
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
          <p className="movie-page__director">Режиссёр: <strong>{movie.director}</strong></p>
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
            <div className="movie-page__actions">
              <Button variant="secondary" onClick={handleMarkWatched}>✓ Просмотрено</Button>
              <Button variant="secondary" onClick={handleAddToWatchlist}>
                {inWatchlist ? '✓ В списке' : '+ В список'}
              </Button>
            </div>
          )}
        </div>
        <div className="movie-page__poster">
          {movie.posterUrl ? <img src={movie.posterUrl} alt="" /> : '🎬'}
        </div>
      </div>

      {isAuth && !userAlreadyReviewed && (
        <div className="review-form">
          <h2 className="review-form__title">Написать отзыв</h2>
          <form onSubmit={handleSubmitReview} className="review-form__body">
            <div>
              <p className="review-form__label">Оценка (кликни на звезду)</p>
              <StarRating value={form.rating} onChange={v => setForm(f => ({ ...f, rating: v }))} size={24} />
            </div>
            <div>
              <p className="review-form__label">Отзыв</p>
              <textarea
                className="review-form__textarea"
                value={form.text}
                onChange={e => setForm(f => ({ ...f, text: e.target.value }))}
                placeholder="Поделитесь впечатлениями о фильме..."
                rows={4}
              />
            </div>
            <Button type="submit" loading={submitting} disabled={!form.rating || form.text.length < 10} style={{ alignSelf: 'flex-start' }}>
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
          <p>Отзывов пока нет. Будь первым!</p>
        </div>
      ) : (
        <div className="reviews-list">
          {reviews.map(r => (
            <ReviewCard key={r.id} review={r} currentUserId={user?.id}
              onDelete={rid => setReviews(rs => rs.filter(x => x.id !== rid))} />
          ))}
        </div>
      )}
    </div>
  )
}

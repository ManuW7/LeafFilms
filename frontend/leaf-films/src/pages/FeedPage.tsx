import { useEffect, useState, useCallback } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { apiFetch } from '../lib/api'
import type { FeedItem, ReviewReaction, WsMessage } from '../types'
import { useAuthStore } from '../store/authStore'
import { useWebSocket } from '../hooks/useWebSocket'
import Avatar from '../components/ui/Avatar'
import Spinner from '../components/ui/Spinner'
import Button from '../components/ui/Button'
import './FeedPage.css'

function FeedCard({
  item,
  onReactionUpdated,
}: {
  item: FeedItem
  onReactionUpdated: (reviewId: string, likesCount: number, dislikesCount: number) => void
}) {
  const navigate = useNavigate()
  const isReview = item.eventType === 'review_created'
  const [myReaction, setMyReaction] = useState<-1 | 0 | 1>(0)

  const handleReaction = async (value: -1 | 1) => {
    if (!item.reviewId) return
    const nextValue = myReaction === value ? 0 : value
    const result = await apiFetch<ReviewReaction>('POST', `/reviews/${item.reviewId}/reaction`, { value: nextValue })
    onReactionUpdated(item.reviewId, result.likesCount, result.dislikesCount)
    setMyReaction(result.myReaction)
  }

  return (
    <div className="feed-card">
      <Avatar name={item.actorName || '?'} size={40} style={{ flexShrink: 0 }} />
      <div className="feed-card__body">
        <p className="feed-card__text">
          <strong>{item.actorName}</strong>
          {isReview ? ' написал(а) отзыв на ' : ' посмотрел(а) '}
          <span className="feed-card__movie-link" onClick={() => navigate(`/movies/${item.movieId}`)}>
            {item.movieTitle}
          </span>
        </p>
        {isReview && item.rating && (
          <div className="feed-card__rating">
            <span className="feed-card__rating-star">★</span>
            <span className="feed-card__rating-value">{item.rating}</span>
            <span className="feed-card__rating-max">/10</span>
          </div>
        )}
        {item.extraText && <p className="feed-card__excerpt">{item.extraText}</p>}
        {item.imageUrl && <img className="feed-card__image" src={item.imageUrl} alt="Изображение к отзыву" />}
        {isReview && item.reviewId && (
          <div className="feed-card__reactions">
            <button className={myReaction === 1 ? 'feed-card__reaction--active' : ''} onClick={() => handleReaction(1)}>
              Нравится <span>{item.likesCount || 0}</span>
            </button>
            <button className={myReaction === -1 ? 'feed-card__reaction--active' : ''} onClick={() => handleReaction(-1)}>
              Не нравится <span>{item.dislikesCount || 0}</span>
            </button>
          </div>
        )}
        <p className="feed-card__date">{new Date(item.createdAt).toLocaleString('ru-RU')}</p>
      </div>
      <div className={`feed-card__icon ${isReview ? 'feed-card__icon--review' : 'feed-card__icon--watched'}`}>
        {isReview ? '★' : '🎬'}
      </div>
    </div>
  )
}

function WsNotification({ msg, onClose }: { msg: WsMessage; onClose: () => void }) {
  useEffect(() => {
    const t = setTimeout(onClose, 4000)
    return () => clearTimeout(t)
  }, [onClose])

  return (
    <div className="ws-notification">
      <div className="ws-notification__inner">
        <span className="ws-notification__icon">{msg.type === 'review_created' ? '★' : '🎬'}</span>
        <div>
          <p className="ws-notification__title">Новое событие</p>
          <p className="ws-notification__body">
            {msg.actorName} {msg.type === 'review_created'
              ? `оценил(а) «${msg.movieTitle}» на ${msg.rating}/10`
              : `посмотрел(а) «${msg.movieTitle}»`}
          </p>
        </div>
        <button className="ws-notification__close" onClick={onClose}>×</button>
      </div>
    </div>
  )
}

export default function FeedPage() {
  const { token } = useAuthStore()
  const [items, setItems] = useState<FeedItem[]>([])
  const [loading, setLoading] = useState(true)
  const [notification, setNotification] = useState<WsMessage | null>(null)

  useEffect(() => {
    apiFetch<FeedItem[]>('GET', '/feed?page=1&pageSize=20')
      .then(data => setItems(data))
      .finally(() => setLoading(false))
  }, [])

  const handleWsMessage = useCallback((msg: WsMessage) => {
    if (msg.type === 'review_deleted') {
      setItems(prev => prev.filter(item => item.reviewId !== msg.reviewId))
      return
    }

    if (msg.type === 'review_reaction_updated') {
      setItems(prev => prev.map(item => item.reviewId === msg.reviewId
        ? {
            ...item,
            likesCount: msg.likesCount ?? item.likesCount,
            dislikesCount: msg.dislikesCount ?? item.dislikesCount,
          }
        : item))
      return
    }

    setNotification(msg)
    const newItem: FeedItem = {
      id: Math.random().toString(),
      eventType: msg.type,
      actorId: '',
      actorName: msg.actorName || 'Кто-то',
      movieId: msg.movieId || '',
      movieTitle: msg.movieTitle || '',
      imageUrl: msg.imageUrl,
      rating: msg.rating,
      likesCount: 0,
      dislikesCount: 0,
      createdAt: msg.createdAt || msg.watchedAt || new Date().toISOString(),
    }
    setItems(prev => [newItem, ...prev])
  }, [])

  const handleReactionUpdated = useCallback((reviewId: string, likesCount: number, dislikesCount: number) => {
    setItems(prev => prev.map(item => item.reviewId === reviewId
      ? { ...item, likesCount, dislikesCount }
      : item))
  }, [])

  useWebSocket(token, handleWsMessage)

  if (loading) return <div style={{ display: 'flex', justifyContent: 'center', padding: '4rem' }}><Spinner /></div>

  return (
    <div className="feed-page">
      {notification && <WsNotification msg={notification} onClose={() => setNotification(null)} />}

      <div className="feed-page__header">
        <div>
          <h1 className="feed-page__title">Лента</h1>
          <p className="feed-page__subtitle">События от людей, на которых вы подписаны</p>
        </div>
      </div>

      {items.length === 0 ? (
        <div className="feed-page__empty">
          <p className="feed-page__empty-title">В лесу пока тихо</p>
          <p>Подпишитесь на других зрителей, чтобы увидеть их отзывы и просмотры.</p>
          <Link to="/users/search" className="feed-page__empty-link">
            <Button>Найти людей</Button>
          </Link>
        </div>
      ) : (
        <div className="feed-page__list">
          {items.map(item => (
            <FeedCard
              key={item.id}
              item={item}
              onReactionUpdated={handleReactionUpdated}
            />
          ))}
        </div>
      )}
    </div>
  )
}

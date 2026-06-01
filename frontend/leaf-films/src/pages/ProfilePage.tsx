import React, { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { apiFetch } from '../lib/api'
import type { User, UserSummary, Review } from '../types'
import { useAuthStore } from '../store/authStore'
import Avatar from '../components/ui/Avatar'
import Button from '../components/ui/Button'
import Spinner from '../components/ui/Spinner'
import Toast from '../components/ui/Toast'
import './ProfilePage.css'

type Tab = 'reviews' | 'followers' | 'following'

export default function ProfilePage() {
  const { id } = useParams<{ id: string }>()
  const { user: me } = useAuthStore()
  const navigate = useNavigate()

  const [profile, setProfile] = useState<User | null>(null)
  const [reviews, setReviews] = useState<Review[]>([])
  const [followers, setFollowers] = useState<UserSummary[]>([])
  const [following, setFollowing] = useState<UserSummary[]>([])
  const [isFollowing, setIsFollowing] = useState(false)
  const [loading, setLoading] = useState(true)
  const [tab, setTab] = useState<Tab>('reviews')
  const [toast, setToast] = useState<{ msg: string; type: 'success' | 'error' } | null>(null)
  const [followLoading, setFollowLoading] = useState(false)

  const isMe = me?.id === id

  useEffect(() => {
    if (!id) return
    Promise.all([
      apiFetch<User>('GET', `/users/${id}`),
      apiFetch<Review[]>('GET', `/reviews/user/${id}`),
      apiFetch<UserSummary[]>('GET', `/users/${id}/followers`),
      apiFetch<UserSummary[]>('GET', `/users/${id}/following`),
    ]).then(([profileData, reviewsData, followersData, followingData]) => {
      setProfile(profileData)
      setReviews(reviewsData)
      setFollowers(followersData)
      setFollowing(followingData)
      if (me) setIsFollowing(followersData.some((f: UserSummary) => f.userId === me.id))
    }).finally(() => setLoading(false))
  }, [id, me])

  const handleFollow = async () => {
    if (!id) return
    setFollowLoading(true)
    try {
      if (isFollowing) {
        await apiFetch('DELETE', '/follow', { followedId: id })
        setIsFollowing(false)
        setFollowers(f => f.filter(x => x.userId !== me?.id))
        setToast({ msg: 'Вы отписались', type: 'error' })
      } else {
        await apiFetch('POST', '/follow', { followedId: id })
        setIsFollowing(true)
        if (me) setFollowers(f => [...f, { userId: me.id, username: me.username, followedAt: new Date().toISOString() }])
        setToast({ msg: 'Вы подписались!', type: 'success' })
      }
    } catch {
      setToast({ msg: 'Ошибка', type: 'error' })
    } finally {
      setFollowLoading(false)
    }
  }

  if (loading) return <div style={{ display: 'flex', justifyContent: 'center', padding: '4rem' }}><Spinner /></div>
  if (!profile) return <p style={{ color: 'var(--danger)' }}>Пользователь не найден</p>

  const tabs: { key: Tab; label: string; count: number }[] = [
    { key: 'reviews', label: 'Отзывы', count: reviews.length },
    { key: 'followers', label: 'Подписчики', count: followers.length },
    { key: 'following', label: 'Подписки', count: following.length },
  ]

  return (
    <div className="profile-page">
      {toast && <Toast message={toast.msg} type={toast.type as any} onClose={() => setToast(null)} />}

      <div className="profile-header">
        <Avatar name={profile.username} size={72} />
        <div className="profile-header__info">
          <h1 className="profile-header__name">{profile.username}</h1>
          <p className="profile-header__meta">{profile.email} · на сайте с {new Date(profile.createdAt).toLocaleDateString('ru-RU')}</p>
          <div className="profile-header__stats">
            <span className="profile-header__stat"><strong>{reviews.length}</strong> отзывов</span>
            <span className="profile-header__stat"><strong>{followers.length}</strong> подписчиков</span>
            <span className="profile-header__stat"><strong>{following.length}</strong> подписок</span>
          </div>
        </div>
        {me && !isMe && (
          <Button variant={isFollowing ? 'secondary' : 'primary'} onClick={handleFollow} loading={followLoading}>
            {isFollowing ? 'Отписаться' : 'Подписаться'}
          </Button>
        )}
      </div>

      <div className="profile-tabs">
        {tabs.map(t => (
          <button key={t.key} className={`profile-tabs__btn ${tab === t.key ? 'profile-tabs__btn--active' : ''}`} onClick={() => setTab(t.key)}>
            {t.label} <span className="profile-tabs__count">({t.count})</span>
          </button>
        ))}
      </div>

      {tab === 'reviews' && (
        <div className="profile-list">
          {reviews.length === 0 && <p className="profile-empty">Отзывов пока нет</p>}
          {reviews.map(r => (
            <div key={r.id} className="profile-review" onClick={() => navigate(`/movies/${r.movieId}`)}>
              <div className="profile-review__top">
                <strong className="profile-review__movie">{r.movieTitle}</strong>
                <span className="profile-review__score">{r.rating}/10</span>
              </div>
              <p className="profile-review__text">{r.text}</p>
              <p className="profile-review__date">{new Date(r.createdAt).toLocaleDateString('ru-RU')}</p>
            </div>
          ))}
        </div>
      )}

      {(tab === 'followers' || tab === 'following') && (
        <div className="profile-user-list">
          {(tab === 'followers' ? followers : following).map(u => (
            <div key={u.userId} className="profile-user" onClick={() => navigate(`/profile/${u.userId}`)}>
              <Avatar name={u.username} size={40} />
              <div>
                <p className="profile-user__name">{u.username}</p>
                <p className="profile-user__date">{new Date(u.followedAt).toLocaleDateString('ru-RU')}</p>
              </div>
              <span className="profile-user__arrow">→</span>
            </div>
          ))}
          {(tab === 'followers' ? followers : following).length === 0 && <p className="profile-empty">Пусто</p>}
        </div>
      )}
    </div>
  )
}

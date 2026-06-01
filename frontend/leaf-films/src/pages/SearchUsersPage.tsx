import { useState, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { apiFetch } from '../lib/api'
import type { User } from '../types'
import { useAuthStore } from '../store/authStore'
import Input from '../components/ui/Input'
import Avatar from '../components/ui/Avatar'
import Spinner from '../components/ui/Spinner'
import './SearchUsersPage.css'

export default function SearchUsersPage() {
  const navigate = useNavigate()
  const { user: me } = useAuthStore()
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<User[]>([])
  const [loading, setLoading] = useState(false)
  const [searched, setSearched] = useState(false)

  const handleSearch = useCallback(async (q: string) => {
    setQuery(q)
    if (q.trim().length < 2) { setResults([]); setSearched(false); return }
    setLoading(true)
    try {
      const data = await apiFetch<User[]>('GET', `/users/search?q=${encodeURIComponent(q)}`)
      setResults(data.filter(u => u.id !== me?.id))
      setSearched(true)
    } finally {
      setLoading(false)
    }
  }, [me?.id])

  return (
    <div className="search-users-page">
      <h1 className="search-users-page__title">Найти пользователей</h1>
      <p className="search-users-page__subtitle">Найдите друзей и подпишитесь на их активность</p>

      <Input className="search-users-page__input" placeholder="Введите имя пользователя..."
        value={query} onChange={e => handleSearch(e.target.value)} />

      {loading && <div className="search-users-page__searching"><Spinner size={24} /></div>}

      {!loading && searched && results.length === 0 && (
        <div className="search-users-page__empty">
          <p className="search-users-page__empty-icon">🔍</p>
          <p>Никого не найдено</p>
        </div>
      )}

      <div className="search-users-list">
        {results.map(u => (
          <div key={u.id} className="search-user-item" onClick={() => navigate(`/profile/${u.id}`)}>
            <Avatar name={u.username} size={44} />
            <div>
              <p className="search-user-item__name">{u.username}</p>
            </div>
            <span className="search-user-item__arrow">Перейти →</span>
          </div>
        ))}
      </div>
    </div>
  )
}

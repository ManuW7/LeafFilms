import React, { useState } from 'react'
import { Link, useNavigate, useLocation } from 'react-router-dom'
import { useAuthStore } from '../../store/authStore'
import Avatar from '../ui/Avatar'
import './Navbar.css'

const links = [
  { to: '/movies', label: 'Каталог' },
  { to: '/feed', label: 'Лента' },
  { to: '/activity', label: 'Активность' },
  { to: '/users/search', label: 'Люди' },
]

export default function Navbar() {
  const { user, isAuth, logout } = useAuthStore()
  const navigate = useNavigate()
  const location = useLocation()
  const [menuOpen, setMenuOpen] = useState(false)

  const handleLogout = () => { logout(); navigate('/login') }

  return (
    <header className="navbar">
      <nav className="navbar__inner">
        <Link to="/" className="navbar__logo">CineGram</Link>

        {isAuth && (
          <div className="navbar__links">
            {links.map(l => (
              <Link
                key={l.to}
                to={l.to}
                className={`navbar__link ${location.pathname.startsWith(l.to) ? 'navbar__link--active' : ''}`}
              >
                {l.label}
              </Link>
            ))}
          </div>
        )}

        <div className="navbar__right">
          {isAuth && user ? (
            <div style={{ position: 'relative' }}>
              <button className="navbar__user-btn" onClick={() => setMenuOpen(v => !v)}>
                <Avatar name={user.username} size={32} />
                <span className="navbar__username">{user.username}</span>
              </button>

              {menuOpen && (
                <div className="navbar__dropdown" onMouseLeave={() => setMenuOpen(false)}>
                  {[
                    { to: `/profile/${user.id}`, label: 'Мой профиль' },
                    { to: '/activity', label: 'Активность' },
                  ].map(item => (
                    <Link
                      key={item.to}
                      to={item.to}
                      className="navbar__dropdown-item"
                      onClick={() => setMenuOpen(false)}
                    >
                      {item.label}
                    </Link>
                  ))}
                  <hr className="navbar__divider" />
                  <button className="navbar__logout" onClick={handleLogout}>Выйти</button>
                </div>
              )}
            </div>
          ) : (
            <>
              <Link to="/login" className="navbar__auth-link">Войти</Link>
              <Link to="/register" className="navbar__auth-link navbar__auth-link--register">Регистрация</Link>
            </>
          )}
        </div>
      </nav>
    </header>
  )
}

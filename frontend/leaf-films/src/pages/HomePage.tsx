import React from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'
import Button from '../components/ui/Button'
import './HomePage.css'

export default function HomePage() {
  const { isAuth, user } = useAuthStore()
  const navigate = useNavigate()

  if (isAuth) {
    return (
      <div className="home-auth">
        <h1 className="home-auth__greeting">Привет, {user?.username} 👋</h1>
        <p className="home-auth__subtitle">Что будем смотреть сегодня?</p>
        <div className="home-auth__grid">
          {[
            { to: '/movies', icon: '🎬', label: 'Каталог', desc: 'Все фильмы' },
            { to: '/feed', icon: '📰', label: 'Лента', desc: 'События друзей' },
            { to: '/activity', icon: '📋', label: 'Активность', desc: 'История и списки' },
            { to: `/profile/${user?.id}`, icon: '👤', label: 'Профиль', desc: 'Мои отзывы' },
          ].map(item => (
            <div key={item.to} className="home-auth__card" onClick={() => navigate(item.to)}>
              <div className="home-auth__card-icon">{item.icon}</div>
              <p className="home-auth__card-label">{item.label}</p>
              <p className="home-auth__card-desc">{item.desc}</p>
            </div>
          ))}
        </div>
      </div>
    )
  }

  return (
    <div className="home-landing">
      <p className="home-landing__eyebrow">Социальная сеть для киноманов</p>
      <h1 className="home-landing__title">
        Открывай кино вместе<br />
        <em>с теми, кто понимает</em>
      </h1>
      <p className="home-landing__subtitle">
        Оценивай фильмы, пиши рецензии, подписывайся на друзей
        и следи за их вкусами в реальном времени.
      </p>
      <div className="home-landing__cta">
        <Link to="/register"><Button size="lg">Начать бесплатно</Button></Link>
        <Link to="/movies"><Button size="lg" variant="secondary">Смотреть каталог</Button></Link>
      </div>
      <div className="home-landing__features">
        {[
          { icon: '⭐', title: 'Оценки и рецензии', desc: 'Система оценок от 1 до 10. Пиши развёрнутые отзывы.' },
          { icon: '👥', title: 'Подписки', desc: 'Следи за вкусами друзей и находи новые фильмы.' },
          { icon: '⚡', title: 'Real-time лента', desc: 'Мгновенные уведомления о новых отзывах в ленте.' },
        ].map(f => (
          <div key={f.title} className="home-landing__feature">
            <div className="home-landing__feature-icon">{f.icon}</div>
            <h3 className="home-landing__feature-title">{f.title}</h3>
            <p className="home-landing__feature-desc">{f.desc}</p>
          </div>
        ))}
      </div>
    </div>
  )
}

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
        <h1 className="home-auth__greeting">Привет, {user?.username}</h1>
        <p className="home-auth__subtitle">Куда свернем сегодня: к новому фильму, в ленту или в свою рощу списков?</p>
        <div className="home-auth__grid">
          {[
            { to: '/movies', icon: '🎬', label: 'Каталог', desc: 'Все фильмы' },
            { to: '/feed', icon: '🌲', label: 'Лента', desc: 'События друзей' },
            { to: '/activity', icon: '🍃', label: 'Активность', desc: 'История и списки' },
            { to: `/profile/${user?.id}`, icon: '🌿', label: 'Профиль', desc: 'Мои отзывы' },
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
      <p className="home-landing__eyebrow">LeafFilms · лесная лента киновпечатлений</p>
      <h1 className="home-landing__title">
        Смотри кино<br />
        <em>как гуляешь по лесу</em>
      </h1>
      <p className="home-landing__subtitle">
        Оценивайте фильмы, оставляйте отзывы с изображениями, собирайте плейлисты
        и следите за вкусами людей, которым доверяете.
      </p>
      <div className="home-landing__cta">
        <Link to="/register"><Button size="lg">Начать</Button></Link>
        <Link to="/movies"><Button size="lg" variant="secondary">Смотреть каталог</Button></Link>
      </div>
      <div className="home-landing__features">
        {[
          { icon: '🌿', title: 'Отзывы и кадры', desc: 'Оценки от 1 до 10, текст и ссылка на изображение к отзыву.' },
          { icon: '🌱', title: 'Плейлисты', desc: 'Собирайте свои тропы: по жанрам, настроению или людям.' },
          { icon: '✨', title: 'Живая лента', desc: 'Отзывы и просмотры подписок появляются почти сразу.' },
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

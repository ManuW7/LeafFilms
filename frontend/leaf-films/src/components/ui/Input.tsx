import React, { useState } from 'react'
import './Input.css'

interface Props extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
}

export default function Input({ label, error, className = '', ...rest }: Props) {
  const [showPassword, setShowPassword] = useState(false)
  const isPassword = rest.type === 'password'

  return (
    <div className="input-field">
      {label && <label className="input-field__label">{label}</label>}
      <div className="input-field__control">
        <input
          {...rest}
          type={isPassword && showPassword ? 'text' : rest.type}
          className={`input-field__input ${isPassword ? 'input-field__input--with-action' : ''} ${error ? 'input-field__input--error' : ''} ${className}`}
        />
        {isPassword && (
          <button
            type="button"
            className="input-field__toggle"
            onClick={() => setShowPassword(value => !value)}
            aria-label={showPassword ? 'Скрыть пароль' : 'Показать пароль'}
          >
            {showPassword ? '◉' : '◎'}
          </button>
        )}
      </div>
      {error && <span className="input-field__error">{error}</span>}
    </div>
  )
}

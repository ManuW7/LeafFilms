import React from 'react'
import './Input.css'

interface Props extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
}

export default function Input({ label, error, className = '', ...rest }: Props) {
  return (
    <div className="input-field">
      {label && <label className="input-field__label">{label}</label>}
      <input
        {...rest}
        className={`input-field__input ${error ? 'input-field__input--error' : ''} ${className}`}
      />
      {error && <span className="input-field__error">{error}</span>}
    </div>
  )
}

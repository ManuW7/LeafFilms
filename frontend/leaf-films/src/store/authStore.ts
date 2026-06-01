import React, { createContext, useContext, useEffect, useReducer } from 'react'
import type { User } from '../types'

// ── Types ─────────────────────────────────────────────────────────────────────
interface AuthState {
  user: User | null
  token: string | null
  isAuth: boolean
}

type AuthAction =
  | { type: 'SET_AUTH'; user: User; token: string }
  | { type: 'LOGOUT' }

interface AuthContextValue extends AuthState {
  setAuth: (user: User, token: string) => void
  logout: () => void
}

// ── Reducer ───────────────────────────────────────────────────────────────────
function authReducer(state: AuthState, action: AuthAction): AuthState {
  switch (action.type) {
    case 'SET_AUTH':
      return { user: action.user, token: action.token, isAuth: true }
    case 'LOGOUT':
      return { user: null, token: null, isAuth: false }
    default:
      return state
  }
}

function getInitialState(): AuthState {
  try {
    const token = localStorage.getItem('token')
    const userRaw = localStorage.getItem('user')
    if (token && userRaw) {
      return { user: JSON.parse(userRaw), token, isAuth: true }
    }
  } catch {
    localStorage.removeItem('user')
    localStorage.removeItem('token')
  }
  return { user: null, token: null, isAuth: false }
}

// ── Context ───────────────────────────────────────────────────────────────────
const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [state, dispatch] = useReducer(authReducer, undefined, getInitialState)

  const setAuth = (user: User, token: string) => {
    localStorage.setItem('user', JSON.stringify(user))
    localStorage.setItem('token', token)
    dispatch({ type: 'SET_AUTH', user, token })
  }

  const logout = () => {
    localStorage.removeItem('user')
    localStorage.removeItem('token')
    dispatch({ type: 'LOGOUT' })
  }

  useEffect(() => {
    const handleUnauthorized = () => dispatch({ type: 'LOGOUT' })
    window.addEventListener('auth:unauthorized', handleUnauthorized)
    return () => window.removeEventListener('auth:unauthorized', handleUnauthorized)
  }, [])

  return React.createElement(
    AuthContext.Provider,
    { value: { ...state, setAuth, logout } },
    children
  )
}

// ── Hook ──────────────────────────────────────────────────────────────────────
export function useAuthStore(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuthStore must be used within AuthProvider')
  return ctx
}

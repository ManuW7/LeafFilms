import React from 'react'
import { Navigate } from 'react-router-dom'
import { useAuthStore } from '../../store/authStore'

export default function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuth } = useAuthStore()
  return isAuth ? <>{children}</> : <Navigate to="/login" replace />
}

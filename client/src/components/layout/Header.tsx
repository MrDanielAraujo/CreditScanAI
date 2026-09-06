import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../contexts/authContextValue'

export function Header() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  return (
    <header className="flex h-14 items-center justify-between border-b border-neutral/20 bg-surface px-4">
      <span className="text-lg font-semibold">CreditScanAI</span>
      {user && (
        <div className="flex items-center gap-3 text-sm">
          <span className="text-neutral">
            {user.name ?? user.email} · {user.role}
          </span>
          <button onClick={handleLogout} className="font-medium text-primary hover:underline">
            Sair
          </button>
        </div>
      )}
    </header>
  )
}

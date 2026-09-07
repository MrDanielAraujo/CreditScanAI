import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../contexts/authContextValue'
import { useTheme } from '../../contexts/themeContextValue'
import { MoonIcon, SunIcon } from '../common/icons'

export function Header() {
  const { user, logout } = useAuth()
  const { theme, toggleTheme } = useTheme()
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  return (
    <header className="flex h-14 items-center justify-between border-b border-neutral/20 bg-surface px-4">
      <span className="text-lg font-semibold">CreditScanAI</span>
      <div className="flex items-center gap-3 text-sm">
        <button
          onClick={toggleTheme}
          className="flex h-8 w-8 cursor-pointer items-center justify-center rounded-full text-neutral hover:bg-neutral/10 hover:text-surface-dark"
          title={theme === 'dark' ? 'Mudar para modo claro' : 'Mudar para modo escuro'}
          aria-label={theme === 'dark' ? 'Mudar para modo claro' : 'Mudar para modo escuro'}
        >
          {theme === 'dark' ? <SunIcon /> : <MoonIcon />}
        </button>
        {user && (
          <>
            <span className="text-neutral">
              {user.name ?? user.email} · {user.role}
            </span>
            <button onClick={handleLogout} className="font-medium text-primary hover:underline">
              Sair
            </button>
          </>
        )}
      </div>
    </header>
  )
}

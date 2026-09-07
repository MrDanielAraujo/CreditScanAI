import { useEffect, useState } from 'react'
import { NavLink, useLocation } from 'react-router-dom'
import {
  BookIcon,
  BookmarkIcon,
  BuildingIcon,
  CheckSquareIcon,
  ChevronDownIcon,
  FileIcon,
  FolderIcon,
  GridIcon,
  HomeIcon,
  LayersIcon,
  LightbulbIcon,
  ShieldIcon,
  TagIcon,
  UploadIcon,
  UsersIcon,
} from '../common/navIcons'
import { useAuth } from '../../contexts/authContextValue'

const CADASTROS_OPEN_KEY = 'creditscanai_sidebar_cadastros_open'

function getStoredCadastrosOpen(): boolean {
  try {
    const stored = localStorage.getItem(CADASTROS_OPEN_KEY)
    return stored === null ? true : stored === '1'
  } catch {
    return true
  }
}

function setStoredCadastrosOpen(open: boolean): void {
  try {
    localStorage.setItem(CADASTROS_OPEN_KEY, open ? '1' : '0')
  } catch {
    // Sem storage, só não lembra a preferência entre sessões - não é crítico.
  }
}

const links = [
  { to: '/', label: 'Dashboard', icon: HomeIcon, end: true },
  { to: '/documents/upload', label: 'Upload', icon: UploadIcon },
  // end: true - senão "/documents/upload" também bate no prefixo "/documents"
  // e o NavLink marca Documentos como ativo junto com Upload.
  { to: '/documents', label: 'Documentos', icon: FileIcon, end: true },
  { to: '/review', label: 'Revisão', icon: CheckSquareIcon },
  { to: '/consolidation', label: 'Consolidação', icon: LayersIcon },
  { to: '/learning', label: 'Aprendizado', icon: LightbulbIcon },
]

const registrationLinks = [
  { to: '/registrations/companies', label: 'Empresas', icon: BuildingIcon },
  { to: '/registrations/chart-of-accounts', label: 'Planos de Contas', icon: BookIcon },
  { to: '/registrations/standard-accounts', label: 'Contas', icon: TagIcon },
  { to: '/registrations/account-types', label: 'Tipos', icon: GridIcon },
  { to: '/registrations/account-subtypes', label: 'Subtipos', icon: BookmarkIcon },
]

function linkClassName({ isActive }: { isActive: boolean }) {
  return [
    'flex items-center gap-2.5 rounded-md px-3 py-2 text-sm font-medium',
    isActive ? 'bg-primary/10 text-primary' : 'text-neutral hover:bg-neutral/10',
  ].join(' ')
}

export function Sidebar() {
  const { user } = useAuth()
  const location = useLocation()
  const [cadastrosOpen, setCadastrosOpen] = useState(getStoredCadastrosOpen)

  // Chegar numa tela de Cadastros por link direto (ou recarregar a página nela)
  // com o grupo fechado deixaria o item ativo escondido - reabre sozinho.
  useEffect(() => {
    if (location.pathname.startsWith('/registrations')) setCadastrosOpen(true)
  }, [location.pathname])

  const toggleCadastros = () => {
    setCadastrosOpen((current) => {
      const next = !current
      setStoredCadastrosOpen(next)
      return next
    })
  }

  // Auditoria (UC-10) é só Admin/Compliance na matriz de permissões - o
  // backend já bloqueia qualquer outro papel, isso só evita mostrar um link
  // que sempre daria 403 para quem não pode usá-lo.
  const canSeeAudit = user?.role === 'Admin' || user?.role === 'Compliance'

  return (
    <nav className="w-56 shrink-0 overflow-y-auto border-r border-neutral/20 bg-surface p-4">
      <ul className="flex flex-col gap-1">
        {links.map((link) => (
          <li key={link.to}>
            <NavLink to={link.to} end={link.end} className={linkClassName}>
              <link.icon />
              {link.label}
            </NavLink>
          </li>
        ))}
        {canSeeAudit && (
          <li>
            <NavLink to="/audit/logins" className={linkClassName}>
              <ShieldIcon />
              Auditoria de Login
            </NavLink>
          </li>
        )}
        {user?.role === 'Admin' && (
          <li>
            <NavLink to="/admin/users" className={linkClassName}>
              <UsersIcon />
              Usuários
            </NavLink>
          </li>
        )}
      </ul>

      <button
        type="button"
        onClick={toggleCadastros}
        aria-expanded={cadastrosOpen}
        className="mt-4 flex w-full items-center justify-between rounded-md px-3 py-2 text-xs font-semibold uppercase tracking-wide text-neutral hover:bg-neutral/10"
      >
        <span className="flex items-center gap-2.5">
          <FolderIcon />
          Cadastros
        </span>
        <ChevronDownIcon className={['h-4 w-4 transition-transform', cadastrosOpen ? '' : '-rotate-90'].join(' ')} />
      </button>

      {cadastrosOpen && (
        <ul className="ml-[19px] mt-1 flex flex-col gap-1 border-l border-neutral/20 pl-3">
          {registrationLinks.map((link) => (
            <li key={link.to}>
              <NavLink to={link.to} className={linkClassName}>
                <link.icon />
                {link.label}
              </NavLink>
            </li>
          ))}
        </ul>
      )}
    </nav>
  )
}

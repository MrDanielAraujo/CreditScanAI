import { NavLink } from 'react-router-dom'

const links = [
  { to: '/', label: 'Dashboard' },
  { to: '/documents/upload', label: 'Upload' },
  { to: '/review', label: 'Revisão' },
  { to: '/consolidation', label: 'Consolidação' },
]

const registrationLinks = [
  { to: '/registrations/companies', label: 'Empresas' },
  { to: '/registrations/chart-of-accounts', label: 'Planos de Contas' },
  { to: '/registrations/standard-accounts', label: 'Contas' },
  { to: '/registrations/account-types', label: 'Tipos' },
  { to: '/registrations/account-subtypes', label: 'Subtipos' },
]

function linkClassName({ isActive }: { isActive: boolean }) {
  return [
    'block rounded-md px-3 py-2 text-sm font-medium',
    isActive ? 'bg-primary/10 text-primary' : 'text-neutral hover:bg-neutral/10',
  ].join(' ')
}

export function Sidebar() {
  return (
    <nav className="w-56 shrink-0 border-r border-neutral/20 bg-surface p-4">
      <ul className="flex flex-col gap-1">
        {links.map((link) => (
          <li key={link.to}>
            <NavLink to={link.to} end={link.to === '/'} className={linkClassName}>
              {link.label}
            </NavLink>
          </li>
        ))}
      </ul>

      <p className="mt-4 mb-1 px-3 text-xs font-semibold uppercase text-neutral">Cadastros</p>
      <ul className="flex flex-col gap-1">
        {registrationLinks.map((link) => (
          <li key={link.to}>
            <NavLink to={link.to} className={linkClassName}>
              {link.label}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  )
}

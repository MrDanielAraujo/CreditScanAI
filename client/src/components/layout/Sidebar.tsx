import { NavLink } from 'react-router-dom'

const links = [
  { to: '/', label: 'Dashboard' },
  { to: '/documents/upload', label: 'Upload' },
  { to: '/review', label: 'Revisão' },
]

export function Sidebar() {
  return (
    <nav className="w-56 shrink-0 border-r border-neutral/20 bg-surface p-4">
      <ul className="flex flex-col gap-1">
        {links.map((link) => (
          <li key={link.to}>
            <NavLink
              to={link.to}
              end={link.to === '/'}
              className={({ isActive }) =>
                [
                  'block rounded-md px-3 py-2 text-sm font-medium',
                  isActive ? 'bg-primary/10 text-primary' : 'text-neutral hover:bg-neutral/10',
                ].join(' ')
              }
            >
              {link.label}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  )
}

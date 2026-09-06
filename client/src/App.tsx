import { Route, Routes } from 'react-router-dom'
import { MainLayout } from './components/layout/MainLayout'
import { AccountSubtypesPage } from './pages/AccountSubtypesPage'
import { AccountTypesPage } from './pages/AccountTypesPage'
import { ChartOfAccountsPage } from './pages/ChartOfAccountsPage'
import { CompaniesPage } from './pages/CompaniesPage'
import { ConsolidationPage } from './pages/ConsolidationPage'
import { DashboardPage } from './pages/DashboardPage'
import { DocumentsPage } from './pages/DocumentsPage'
import { LearningPage } from './pages/LearningPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { ReviewPage } from './pages/ReviewPage'
import { StandardAccountsPage } from './pages/StandardAccountsPage'
import { UploadPage } from './pages/UploadPage'

export function App() {
  return (
    <MainLayout>
      <Routes>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/documents/upload" element={<UploadPage />} />
        <Route path="/documents" element={<DocumentsPage />} />
        <Route path="/review" element={<ReviewPage />} />
        <Route path="/consolidation" element={<ConsolidationPage />} />
        <Route path="/learning" element={<LearningPage />} />
        <Route path="/registrations/companies" element={<CompaniesPage />} />
        <Route path="/registrations/account-types" element={<AccountTypesPage />} />
        <Route path="/registrations/account-subtypes" element={<AccountSubtypesPage />} />
        <Route path="/registrations/chart-of-accounts" element={<ChartOfAccountsPage />} />
        <Route path="/registrations/standard-accounts" element={<StandardAccountsPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </MainLayout>
  )
}

export default App

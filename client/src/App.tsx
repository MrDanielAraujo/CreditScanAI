import { Route, Routes } from 'react-router-dom'
import { ProtectedRoute } from './components/auth/ProtectedRoute'
import { MainLayout } from './components/layout/MainLayout'
import { AuthProvider } from './contexts/AuthContext'
import { AccountSubtypesPage } from './pages/AccountSubtypesPage'
import { AccountTypesPage } from './pages/AccountTypesPage'
import { ChartOfAccountsPage } from './pages/ChartOfAccountsPage'
import { CompaniesPage } from './pages/CompaniesPage'
import { ConsolidationPage } from './pages/ConsolidationPage'
import { DashboardPage } from './pages/DashboardPage'
import { DocumentsPage } from './pages/DocumentsPage'
import { LearningPage } from './pages/LearningPage'
import { LoginPage } from './pages/LoginPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { RegisterPage } from './pages/RegisterPage'
import { ReviewPage } from './pages/ReviewPage'
import { StandardAccountsPage } from './pages/StandardAccountsPage'
import { UploadPage } from './pages/UploadPage'

function AppRoutes() {
  return (
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
  )
}

export function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route
          path="/*"
          element={
            <ProtectedRoute>
              <MainLayout>
                <AppRoutes />
              </MainLayout>
            </ProtectedRoute>
          }
        />
      </Routes>
    </AuthProvider>
  )
}

export default App

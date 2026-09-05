import { Route, Routes } from 'react-router-dom'
import { MainLayout } from './components/layout/MainLayout'
import { DashboardPage } from './pages/DashboardPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { ReviewPage } from './pages/ReviewPage'
import { UploadPage } from './pages/UploadPage'

export function App() {
  return (
    <MainLayout>
      <Routes>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/documents/upload" element={<UploadPage />} />
        <Route path="/review" element={<ReviewPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </MainLayout>
  )
}

export default App

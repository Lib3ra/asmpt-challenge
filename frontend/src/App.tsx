import { Navigate, NavLink, Route, Routes } from 'react-router'
import { ComponentsPage } from './features/components/ComponentsPage'

function App() {
  return (
    <>
      <header className="app-header">
        <span className="app-title">Order Manager</span>
        <nav aria-label="Main">
          <NavLink to="/components">Components</NavLink>
        </nav>
      </header>
      <main>
        <Routes>
          <Route path="/" element={<Navigate to="/components" replace />} />
          <Route path="/components" element={<ComponentsPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </main>
    </>
  )
}

function NotFoundPage() {
  return (
    <section className="page">
      <h1>Page not found</h1>
      <p className="muted">
        <NavLink to="/components">Go to components</NavLink>
      </p>
    </section>
  )
}

export default App

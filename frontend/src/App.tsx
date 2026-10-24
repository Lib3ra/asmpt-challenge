import { Navigate, NavLink, Route, Routes } from 'react-router'
import { BoardsPage } from './features/boards/BoardsPage'
import { ComponentsPage } from './features/components/ComponentsPage'
import { OrdersPage } from './features/orders/OrdersPage'

function App() {
  return (
    <>
      <header className="app-header">
        <span className="app-title">Order Manager</span>
        <nav aria-label="Main">
          <NavLink to="/components">Components</NavLink>
          <NavLink to="/boards">Boards</NavLink>
          <NavLink to="/orders">Orders</NavLink>
        </nav>
      </header>
      <main>
        <Routes>
          <Route path="/" element={<Navigate to="/components" replace />} />
          <Route path="/components" element={<ComponentsPage />} />
          <Route path="/boards" element={<BoardsPage />} />
          <Route path="/orders" element={<OrdersPage />} />
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

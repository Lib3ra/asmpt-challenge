import { ComponentsPage } from './features/components/ComponentsPage'

function App() {
  return (
    <>
      <header className="app-header">
        <span className="app-title">Order Manager</span>
        <nav aria-label="Main">
          <a href="/" aria-current="page">
            Components
          </a>
        </nav>
      </header>
      <main>
        <ComponentsPage />
      </main>
    </>
  )
}

export default App

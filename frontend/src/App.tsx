import { useEffect, useState } from 'react'
import { fetchHealth } from './api/health'
import { errorMessage } from './api/http'
import { ComponentsPage } from './features/components/ComponentsPage'

type ApiState = { kind: 'loading' } | { kind: 'ok'; status: string } | { kind: 'error'; message: string }

function App() {
  const [api, setApi] = useState<ApiState>({ kind: 'loading' })

  useEffect(() => {
    const controller = new AbortController()
    fetchHealth(controller.signal)
      .then((health) => setApi({ kind: 'ok', status: health.status }))
      .catch((error: unknown) => {
        if (!controller.signal.aborted) setApi({ kind: 'error', message: errorMessage(error) })
      })
    return () => controller.abort()
  }, [])

  return (
    <>
      <header className="app-header">
        <span className="app-title">Order Manager</span>
        <nav aria-label="Main">
          <a href="/" aria-current="page">
            Components
          </a>
        </nav>
        <span className="api-status" data-state={api.kind} title={api.kind === 'error' ? api.message : undefined}>
          API {api.kind === 'loading' ? 'checking…' : api.kind === 'ok' ? api.status.toLowerCase() : 'unreachable'}
        </span>
      </header>
      <main>
        <ComponentsPage />
      </main>
    </>
  )
}

export default App

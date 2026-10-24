import { useEffect, useState } from 'react'
import { fetchHealth } from './api/health'

type ApiState =
  | { kind: 'loading' }
  | { kind: 'ok'; status: string }
  | { kind: 'error'; message: string }

function App() {
  const [api, setApi] = useState<ApiState>({ kind: 'loading' })

  useEffect(() => {
    const controller = new AbortController()
    fetchHealth(controller.signal)
      .then((health) => setApi({ kind: 'ok', status: health.status }))
      .catch((error: unknown) => {
        if (controller.signal.aborted) return
        setApi({ kind: 'error', message: error instanceof Error ? error.message : String(error) })
      })
    return () => controller.abort()
  }, [])

  return (
    <main>
      <h1>SMT Order Manager</h1>
      <p className="status" data-state={api.kind}>
        API status:{' '}
        {api.kind === 'loading' && 'checking…'}
        {api.kind === 'ok' && api.status}
        {api.kind === 'error' && `unreachable (${api.message})`}
      </p>
    </main>
  )
}

export default App

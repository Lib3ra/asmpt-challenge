import { useState } from 'react'
import { errorMessage } from '../api/http'

type DeleteSelectionProps = {
  count: number
  noun: string
  onDelete: () => Promise<void>
  onClear: () => void
}

/** Bulk delete bar with an in-page confirmation step. */
export function DeleteSelection({ count, noun, onDelete, onClear }: DeleteSelectionProps) {
  const [confirming, setConfirming] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (count === 0) return null
  const label = `${count} ${noun}${count === 1 ? '' : 's'}`

  async function confirmDelete() {
    setDeleting(true)
    setError(null)
    try {
      await onDelete()
      setConfirming(false)
    } catch (deleteError) {
      setError(errorMessage(deleteError))
    } finally {
      setDeleting(false)
    }
  }

  return (
    <div className="selection-bar" role="region" aria-label="Selection">
      {confirming ? (
        <>
          <span>
            Delete {label}? This cannot be undone.
          </span>
          <button type="button" className="danger" onClick={confirmDelete} disabled={deleting}>
            {deleting ? 'Deleting…' : `Delete ${label}`}
          </button>
          <button type="button" className="link-button" onClick={() => setConfirming(false)} disabled={deleting}>
            Cancel
          </button>
        </>
      ) : (
        <>
          <span>{label} selected</span>
          <button type="button" className="danger" onClick={() => setConfirming(true)}>
            Delete selected
          </button>
          <button type="button" className="link-button" onClick={onClear}>
            Clear selection
          </button>
        </>
      )}
      {error && <p className="form-error">{error}</p>}
    </div>
  )
}

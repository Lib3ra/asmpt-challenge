import { useState, type FormEvent } from 'react'
import { createBoard, updateBoard, type Board, type BoardInput } from '../../api/boards'
import type { Component } from '../../api/components'
import { ApiError, errorMessage, type FieldErrors } from '../../api/http'
import { CheckboxList } from '../../ui/CheckboxList'
import { FieldError } from '../../ui/FieldError'
import { componentLabel } from './componentLabel'

type BoardFormProps = {
  /** The board being edited, or null to create a new one. */
  board: Board | null
  components: Component[]
  onSaved: (board: Board) => void
  onCancel: () => void
}

type BoardDraft = {
  name: string
  description: string
  lengthMm: string
  widthMm: string
  componentIds: number[]
}

function draftFrom(board: Board | null): BoardDraft {
  return board
    ? {
        name: board.name,
        description: board.description,
        lengthMm: String(board.lengthMm),
        widthMm: String(board.widthMm),
        componentIds: board.components.map((component) => component.id),
      }
    : { name: '', description: '', lengthMm: '', widthMm: '', componentIds: [] }
}

/** Empty dimensions are sent as 0 so the server reports them with its range message. */
function toInput(draft: BoardDraft): BoardInput {
  return {
    name: draft.name,
    description: draft.description,
    lengthMm: Number(draft.lengthMm) || 0,
    widthMm: Number(draft.widthMm) || 0,
    componentIds: draft.componentIds,
  }
}

/** Creates or edits one board. The parent remounts it (key) when switching between boards. */
export function BoardForm({ board, components, onSaved, onCancel }: BoardFormProps) {
  const [draft, setDraft] = useState<BoardDraft>(() => draftFrom(board))
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setFieldErrors({})
    setFormError(null)
    try {
      const saved = board ? await updateBoard(board.id, toInput(draft)) : await createBoard(toInput(draft))
      setDraft(draftFrom(null))
      onSaved(saved)
    } catch (error) {
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        setFieldErrors(error.fieldErrors)
      } else {
        setFormError(errorMessage(error))
      }
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="panel" onSubmit={handleSubmit} noValidate>
      <h2>{board ? `Edit ${board.name}` : 'Add board'}</h2>
      <div className="form-grid">
        <label>
          Name
          <input
            id="board-name"
            value={draft.name}
            aria-invalid={Boolean(fieldErrors.name)}
            onChange={(event) => setDraft({ ...draft, name: event.target.value })}
          />
          <FieldError messages={fieldErrors.name} />
        </label>
        <label>
          Description
          <input
            id="board-description"
            value={draft.description}
            aria-invalid={Boolean(fieldErrors.description)}
            onChange={(event) => setDraft({ ...draft, description: event.target.value })}
          />
          <FieldError messages={fieldErrors.description} />
        </label>
        <label>
          Length (mm)
          <input
            id="board-length"
            type="number"
            min="0.01"
            max="1000"
            step="0.01"
            value={draft.lengthMm}
            aria-invalid={Boolean(fieldErrors.lengthMm)}
            onChange={(event) => setDraft({ ...draft, lengthMm: event.target.value })}
          />
          <FieldError messages={fieldErrors.lengthMm} />
        </label>
        <label>
          Width (mm)
          <input
            id="board-width"
            type="number"
            min="0.01"
            max="1000"
            step="0.01"
            value={draft.widthMm}
            aria-invalid={Boolean(fieldErrors.widthMm)}
            onChange={(event) => setDraft({ ...draft, widthMm: event.target.value })}
          />
          <FieldError messages={fieldErrors.widthMm} />
        </label>
      </div>
      <CheckboxList
        id="board-component"
        legend="Components (copies per board)"
        options={components.map((component) => ({ id: component.id, label: componentLabel(component) }))}
        selectedIds={draft.componentIds}
        invalid={Boolean(fieldErrors.componentIds)}
        onChange={(componentIds) => setDraft({ ...draft, componentIds })}
      />
      <FieldError messages={fieldErrors.componentIds} />
      {formError && <p className="form-error">{formError}</p>}
      <div className="actions">
        {board && (
          <button type="button" className="secondary" onClick={onCancel} disabled={saving}>
            Cancel
          </button>
        )}
        <button type="submit" disabled={saving}>
          {saving ? 'Saving…' : board ? 'Save changes' : 'Save board'}
        </button>
      </div>
    </form>
  )
}

import { useState } from 'react'
import { componentLimits, updateComponent, type Component } from '../../api/components'
import { ApiError, errorMessage, type FieldErrors } from '../../api/http'
import { FieldError } from '../../ui/FieldError'
import { draftFrom, toInput, type ComponentDraft } from './componentDraft'

type ComponentRowProps = {
  component: Component
  selected: boolean
  onToggleSelected: (id: number) => void
  onSaved: (updated: Component) => void
}

/** One table row; switches to inline inputs while editing. */
export function ComponentRow({ component, selected, onToggleSelected, onSaved }: ComponentRowProps) {
  const [draft, setDraft] = useState<ComponentDraft | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [rowError, setRowError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  function startEditing() {
    setDraft(draftFrom(component))
    setFieldErrors({})
    setRowError(null)
  }

  async function save() {
    if (!draft) return
    setSaving(true)
    setRowError(null)
    try {
      const updated = await updateComponent(component.id, toInput(draft))
      setDraft(null)
      setFieldErrors({})
      onSaved(updated)
    } catch (error) {
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        setFieldErrors(error.fieldErrors)
      } else {
        setRowError(errorMessage(error))
      }
    } finally {
      setSaving(false)
    }
  }

  const selectCell = (
    <td className="select-cell">
      <input
        id={`select-component-${component.id}`}
        type="checkbox"
        checked={selected}
        aria-label={`Select ${component.name}`}
        onChange={() => onToggleSelected(component.id)}
      />
    </td>
  )

  if (!draft) {
    return (
      <tr>
        {selectCell}
        <td>{component.name}</td>
        <td className="muted">{component.description}</td>
        <td className="number">{component.quantity}</td>
        <td className="row-actions">
          <button type="button" className="link-button" onClick={startEditing}>
            Edit
          </button>
        </td>
      </tr>
    )
  }

  return (
    <tr className="editing">
      {selectCell}
      <td>
        <input
          id={`edit-name-${component.id}`}
          aria-label="Name"
          value={draft.name}
          maxLength={componentLimits.nameMaxLength}
          aria-invalid={Boolean(fieldErrors.name)}
          onChange={(event) => setDraft({ ...draft, name: event.target.value })}
        />
        <FieldError messages={fieldErrors.name} />
      </td>
      <td>
        <input
          id={`edit-description-${component.id}`}
          aria-label="Description"
          value={draft.description}
          maxLength={componentLimits.descriptionMaxLength}
          aria-invalid={Boolean(fieldErrors.description)}
          onChange={(event) => setDraft({ ...draft, description: event.target.value })}
        />
        <FieldError messages={fieldErrors.description} />
      </td>
      <td className="number">
        <input
          id={`edit-quantity-${component.id}`}
          aria-label="Quantity per board"
          type="number"
          min={componentLimits.minQuantity}
          max={componentLimits.maxQuantity}
          value={draft.quantity}
          aria-invalid={Boolean(fieldErrors.quantity)}
          onChange={(event) => setDraft({ ...draft, quantity: event.target.value })}
        />
        <FieldError messages={fieldErrors.quantity} />
      </td>
      <td className="row-actions">
        <button type="button" onClick={save} disabled={saving}>
          {saving ? 'Saving…' : 'Save'}
        </button>
        <button type="button" className="link-button" onClick={() => setDraft(null)} disabled={saving}>
          Cancel
        </button>
        {rowError && <p className="form-error">{rowError}</p>}
      </td>
    </tr>
  )
}

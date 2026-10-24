import { useState, type FormEvent } from 'react'
import { componentLimits, createComponents, type Component } from '../../api/components'
import { ApiError, errorMessage, type FieldErrors } from '../../api/http'
import { FieldError } from '../../ui/FieldError'
import { emptyDraft, toInput, type ComponentDraft } from './componentDraft'

type ComponentCreateFormProps = {
  onCreated: (created: Component[]) => void
}

/** Creates one or more components at once. The server saves all rows or none. */
export function ComponentCreateForm({ onCreated }: ComponentCreateFormProps) {
  const [drafts, setDrafts] = useState<ComponentDraft[]>([emptyDraft])
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  function updateDraft(index: number, change: Partial<ComponentDraft>) {
    setDrafts((current) => current.map((draft, i) => (i === index ? { ...draft, ...change } : draft)))
  }

  function removeDraft(index: number) {
    setDrafts((current) => current.filter((_, i) => i !== index))
    setFieldErrors({})
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setFieldErrors({})
    setFormError(null)
    try {
      const created = await createComponents(drafts.map(toInput))
      setDrafts([emptyDraft])
      onCreated(created)
    } catch (error) {
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        setFieldErrors(error.fieldErrors)
        setFormError('Nothing was saved. Fix the highlighted rows and try again.')
      } else {
        setFormError(errorMessage(error))
      }
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="panel create-form" onSubmit={handleSubmit} noValidate>
      <h2>Add components</h2>
      <div className="draft-rows">
        {drafts.map((draft, index) => {
          const errorsFor = (field: string) => fieldErrors[`[${index}].${field}`]
          return (
            // Rows have no stable id before they are saved; the index is the row position in the request.
            <fieldset className="draft-row" key={index} aria-label={`Component ${index + 1}`}>
              <label>
                Name
                <input
                  id={`create-name-${index}`}
                  value={draft.name}
                  maxLength={componentLimits.nameMaxLength}
                  aria-invalid={Boolean(errorsFor('name'))}
                  onChange={(event) => updateDraft(index, { name: event.target.value })}
                />
                <FieldError messages={errorsFor('name')} />
              </label>
              <label className="grow">
                Description
                <input
                  id={`create-description-${index}`}
                  value={draft.description}
                  maxLength={componentLimits.descriptionMaxLength}
                  aria-invalid={Boolean(errorsFor('description'))}
                  onChange={(event) => updateDraft(index, { description: event.target.value })}
                />
                <FieldError messages={errorsFor('description')} />
              </label>
              <label className="narrow">
                Quantity per board
                <input
                  id={`create-quantity-${index}`}
                  type="number"
                  min={componentLimits.minQuantity}
                  max={componentLimits.maxQuantity}
                  value={draft.quantity}
                  aria-invalid={Boolean(errorsFor('quantity'))}
                  onChange={(event) => updateDraft(index, { quantity: event.target.value })}
                />
                <FieldError messages={errorsFor('quantity')} />
              </label>
              {drafts.length > 1 && (
                <button type="button" className="link-button" onClick={() => removeDraft(index)}>
                  Remove row
                </button>
              )}
            </fieldset>
          )
        })}
      </div>
      <FieldError messages={fieldErrors['']} />
      {formError && <p className="form-error">{formError}</p>}
      <div className="actions">
        <button type="button" className="secondary" onClick={() => setDrafts((current) => [...current, emptyDraft])}>
          Add another row
        </button>
        <button type="submit" disabled={saving}>
          {saving ? 'Saving…' : drafts.length > 1 ? `Save ${drafts.length} components` : 'Save component'}
        </button>
      </div>
    </form>
  )
}

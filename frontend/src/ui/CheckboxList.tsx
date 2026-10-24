type CheckboxOption = {
  id: number
  label: string
}

type CheckboxListProps = {
  id: string
  legend: string
  options: CheckboxOption[]
  selectedIds: number[]
  onChange: (selectedIds: number[]) => void
  invalid?: boolean
}

/** Picks several records by id, e.g. the components of a board. */
export function CheckboxList({ id, legend, options, selectedIds, onChange, invalid }: CheckboxListProps) {
  function toggle(optionId: number) {
    onChange(selectedIds.includes(optionId) ? selectedIds.filter((x) => x !== optionId) : [...selectedIds, optionId])
  }

  return (
    <fieldset className="checkbox-list" aria-invalid={invalid}>
      <legend>{legend}</legend>
      {options.length === 0 && <p className="muted">Nothing to choose from yet.</p>}
      {options.map((option) => (
        <label key={option.id} className="checkbox-option">
          <input
            id={`${id}-${option.id}`}
            type="checkbox"
            checked={selectedIds.includes(option.id)}
            onChange={() => toggle(option.id)}
          />
          {option.label}
        </label>
      ))}
    </fieldset>
  )
}

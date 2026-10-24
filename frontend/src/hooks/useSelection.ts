import { useState } from 'react'

/** Checkbox selection over a list of records with ids; only records still in the list count as selected. */
export function useSelection(items: { id: number }[]) {
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set())

  const visibleSelectedIds = items.filter((item) => selectedIds.has(item.id)).map((item) => item.id)
  const allSelected = items.length > 0 && visibleSelectedIds.length === items.length

  function toggle(id: number) {
    setSelectedIds((current) => {
      const next = new Set(current)
      if (!next.delete(id)) next.add(id)
      return next
    })
  }

  return {
    selectedIds: visibleSelectedIds,
    isSelected: (id: number) => selectedIds.has(id),
    allSelected,
    toggle,
    toggleAll: () => setSelectedIds(allSelected ? new Set() : new Set(items.map((item) => item.id))),
    clear: () => setSelectedIds(new Set()),
  }
}
